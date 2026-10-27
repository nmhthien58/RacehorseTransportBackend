using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // R-10 (Chính sách mục 2, 3): báo giá chốt -> chấp nhận + cọc trong hạn.
    //   Manager duyệt tuyến -> tính báo giá chốt theo tuyến (quãng đường thật, phí từng nước quá cảnh, bảo hiểm từng con, Express)
    //   -> đơn Quoted, hạn = bây giờ + Booking.QuoteAcceptDays, email QuoteReady
    //   -> khách chấp nhận (đồng ý chính sách hiện hành) -> AwaitingDeposit, cọc tính trên báo giá chốt, hạn cọc = hạn báo giá
    //   -> quá hạn mà chưa chấp nhận hoặc chưa cọc -> QuoteExpired (không mất phí). Khách từ chối -> Cancelled.
    public class BookingQuoteService : IBookingQuoteService
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly CultureInfo VietnameseCulture = new("vi-VN");

        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<RoutePlan> _planRepo;
        private readonly IPricingService _pricingService;
        private readonly ISystemSettingService _settingService;
        private readonly IBookingService _bookingService;
        private readonly IBookingActivator _bookingActivator;
        private readonly INotificationService _notificationService;
        private readonly IContractService _contractService;
        private readonly IUnitOfWork _unitOfWork;

        public BookingQuoteService(
            IRepository<BookingRequest> bookingRepo,
            IRepository<RoutePlan> planRepo,
            IPricingService pricingService,
            ISystemSettingService settingService,
            IBookingService bookingService,
            IBookingActivator bookingActivator,
            INotificationService notificationService,
            IContractService contractService,
            IUnitOfWork unitOfWork)
        {
            _bookingRepo = bookingRepo;
            _planRepo = planRepo;
            _pricingService = pricingService;
            _settingService = settingService;
            _bookingService = bookingService;
            _bookingActivator = bookingActivator;
            _notificationService = notificationService;
            _contractService = contractService;
            _unitOfWork = unitOfWork;
        }

        // =============================================================================================
        // Báo giá chốt
        // =============================================================================================

        public async Task IssueFinalQuoteAsync(int bookingId, int routePlanId)
        {
            var booking = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Horse)
                .FirstAsync(b => b.BookingId == bookingId);
            var plan = await _planRepo.GetQueryable().AsNoTracking().FirstAsync(p => p.RoutePlanId == routePlanId);

            var policy = await _settingService.GetPolicySettingsAsync();
            var payment = await _settingService.GetPaymentSettingsAsync();
            var horses = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).ToList();

            // Lịch đã kiểm tra lúc khách gửi đơn; giữ Express như lúc đó để không bắt khách đổi ngày vì công ty lập tuyến lâu
            var quote = await _pricingService.CalculateQuoteAsync(new QuoteInputModel
            {
                TransportMode = booking.TransportMode,
                PickupCountryCode = booking.PickupCountryCode,
                DropoffCountryCode = booking.DropoffCountryCode,
                DistanceKm = (int)Math.Ceiling(plan.TotalDistanceKm),
                RequiresClimateControl = booking.RequiresClimateControl,
                DeclaredValue = null, // bảo hiểm tính theo từng con bên dưới
                PickupWindowStart = booking.PickupWindowStart,
                PickupWindowEnd = booking.PickupWindowEnd,
                DeliveryDeadline = booking.DeliveryDeadline,
                TransitCountryCodes = JsonSerializer.Deserialize<List<string>>(plan.TransitCountryCodes) ?? new List<string>(),
                QuotedAt = booking.CreatedAt,
                StallClasses = horses.Select(bh => bh.StallClass).ToList(),
                SkipScheduleCheck = true,
                ForceExpress = booking.IsExpress
            });

            // R-13: phí bảo hiểm toàn giá trị theo từng con (thay dòng INS_TRANSIT cũ)
            quote.Lines.AddRange(QuoteRules.InsuranceLines(
                horses.Select(bh => new QuoteRules.InsuredHorse(bh.Horse?.Name ?? $"#{bh.HorseId}", bh.InsurancePlan, bh.AgreedValue, bh.DeclaredValue)),
                policy.InsurancePremiumPercent));
            decimal total = quote.Lines.Sum(l => l.Amount);

            var now = DateTimeOffset.UtcNow;
            booking.EstimatedCost = total;
            booking.CurrencyCode = quote.CurrencyCode;
            booking.QuoteBreakdown = _pricingService.SerializeLines(quote.Lines);
            booking.EstimatedTransitHours = plan.EstimatedHours;
            booking.QuotedAt = now;
            booking.QuoteExpiresAt = QuoteRules.ExpiresAt(now, policy.QuoteAcceptDays);
            booking.QuoteReminderSentAt = null;
            booking.Status = BookingStatuses.Quoted;
            await _bookingRepo.SaveChangesAsync();

            // R-17: hợp đồng nháp theo báo giá này (bản nháp cũ, nếu có, bị thay)
            await _contractService.CreateDraftAsync(booking.BookingId, ContractReasons.Initial);

            await _notificationService.SendAsync(
                booking.CustomerUserId,
                $"Final quote for booking {booking.BookingCode}",
                $"The route is planned. Final price: {total:N2} {booking.CurrencyCode}. Accept the quote and pay the deposit by {VietnamTime(booking.QuoteExpiresAt.Value)} (Vietnam time); after that the booking closes without any fee.",
                NotificationReferenceTypes.Booking,
                booking.BookingId);
            await _notificationService.EmailCustomerAsync(
                booking.CustomerUserId,
                EmailTemplateNames.QuoteReady,
                CustomerPages.Booking(booking.BookingId),
                new Dictionary<string, string>
                {
                    ["BookingCode"] = booking.BookingCode,
                    ["PriceUsd"] = total.ToString("N2", CultureInfo.InvariantCulture),
                    ["PriceVnd"] = Math.Round(total * payment.UsdVndRate).ToString("N0", VietnameseCulture),
                    ["ExpiresAt"] = VietnamTime(booking.QuoteExpiresAt.Value)
                });
        }

        // =============================================================================================
        // Khách chấp nhận / từ chối
        // =============================================================================================

        public async Task<BookingDetailModel> AcceptQuoteAsync(int bookingId, ContractAcceptanceModel acceptance, CurrentUser currentUser)
        {
            var booking = await GetOwnBookingAsync(bookingId, currentUser);
            if (booking.Status != BookingStatuses.Quoted)
            {
                throw new BusinessException($"Only a quoted booking can be accepted (current status: {booking.Status}).");
            }
            var now = DateTimeOffset.UtcNow;
            if (QuoteRules.IsExpired(booking.QuoteExpiresAt, now))
            {
                throw new BusinessException($"This quote expired at {VietnamTime(booking.QuoteExpiresAt!.Value)} (Vietnam time). Please submit a new request.");
            }
            await EnsurePolicyAcceptedAsync(acceptance.AcceptPolicy, acceptance.PolicyVersion);
            string policyVersion = acceptance.PolicyVersion;

            var payment = await _settingService.GetPaymentSettingsAsync();
            decimal deposit = QuoteRules.DepositAmount(booking.EstimatedCost, payment.DepositPercent);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                booking.PolicyVersion = policyVersion.Trim();
                booking.PolicyAcceptedAt = now;
                booking.QuoteAcceptedAt = now;
                booking.DepositPercent = payment.DepositPercent;
                booking.DepositAmount = deposit;
                booking.Status = BookingStatuses.AwaitingDeposit;
                await _bookingRepo.UpdateAsync(booking);

                // R-17: giao kết hợp đồng (kiểm tra đúng bản nháp hiện hành + mã băm, lưu bằng chứng)
                await _contractService.AcceptInitialAsync(booking.BookingId, acceptance, currentUser);

                if (deposit <= 0)
                {
                    // Cấu hình cọc 0%: không cần cọc, mở hồ sơ ngay
                    await _bookingActivator.ActivateAsync(booking);
                    return;
                }

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Quote accepted - deposit for booking {booking.BookingCode}",
                    $"Please pay the {payment.DepositPercent:0.##}% deposit of {deposit:N2} {booking.CurrencyCode} by {VietnamTime(booking.QuoteExpiresAt!.Value)} (Vietnam time) to confirm the booking.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Quote accepted: {booking.BookingCode}",
                    $"The customer accepted the final quote of {booking.EstimatedCost:N2} {booking.CurrencyCode}; waiting for the deposit.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            return await _bookingService.GetBookingByIdAsync(bookingId, currentUser);
        }

        public async Task<BookingDetailModel> DeclineQuoteAsync(int bookingId, string? reason, CurrentUser currentUser)
        {
            var booking = await GetOwnBookingAsync(bookingId, currentUser);
            if (booking.Status != BookingStatuses.Quoted)
            {
                throw new BusinessException($"Only a quoted booking can be declined (current status: {booking.Status}).");
            }

            string note = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Reason: {reason.Trim()}";
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                booking.Status = BookingStatuses.Cancelled;
                CancelHorses(booking);
                await _bookingRepo.UpdateAsync(booking);

                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Quote declined: {booking.BookingCode}",
                    $"The customer declined the final quote of {booking.EstimatedCost:N2} {booking.CurrencyCode}.{note}",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            return await _bookingService.GetBookingByIdAsync(bookingId, currentUser);
        }

        // =============================================================================================
        // Job: nhắc sắp hết hạn, đóng đơn quá hạn
        // =============================================================================================

        public async Task<int> ProcessQuoteDeadlinesAsync()
        {
            var now = DateTimeOffset.UtcNow;
            int processed = 0;

            var open = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses)
                .Where(b => b.QuoteExpiresAt != null
                    && (b.Status == BookingStatuses.Quoted || b.Status == BookingStatuses.AwaitingDeposit))
                .ToListAsync();

            foreach (var booking in open)
            {
                if (QuoteRules.IsExpired(booking.QuoteExpiresAt, now))
                {
                    // Chưa chấp nhận, hoặc đã chấp nhận mà chưa cọc: đóng đơn, không mất phí
                    await _unitOfWork.ExecuteInTransactionAsync(async () =>
                    {
                        booking.Status = BookingStatuses.QuoteExpired;
                        CancelHorses(booking);
                        await _bookingRepo.UpdateAsync(booking);

                        await _notificationService.SendAsync(
                            booking.CustomerUserId,
                            $"Quote for booking {booking.BookingCode} has expired",
                            "The quote was not accepted and paid in time, so the booking is closed without any fee. You can submit a new request at any time.",
                            NotificationReferenceTypes.Booking,
                            booking.BookingId);
                        await _notificationService.EmailCustomerAsync(
                            booking.CustomerUserId,
                            EmailTemplateNames.QuoteExpired,
                            CustomerPages.Booking(booking.BookingId),
                            new Dictionary<string, string> { ["BookingCode"] = booking.BookingCode });
                    });
                    processed++;
                }
                else if (booking.Status == BookingStatuses.Quoted
                    && QuoteRules.NeedsReminder(booking.QuoteExpiresAt, booking.QuoteReminderSentAt, now))
                {
                    await _unitOfWork.ExecuteInTransactionAsync(async () =>
                    {
                        booking.QuoteReminderSentAt = now;
                        await _bookingRepo.UpdateAsync(booking);

                        await _notificationService.SendAsync(
                            booking.CustomerUserId,
                            $"Quote for booking {booking.BookingCode} expires soon",
                            $"Accept the quote and pay the deposit by {VietnamTime(booking.QuoteExpiresAt!.Value)} (Vietnam time).",
                            NotificationReferenceTypes.Booking,
                            booking.BookingId);
                        await _notificationService.EmailCustomerAsync(
                            booking.CustomerUserId,
                            EmailTemplateNames.QuoteExpiring,
                            CustomerPages.Booking(booking.BookingId),
                            new Dictionary<string, string>
                            {
                                ["BookingCode"] = booking.BookingCode,
                                ["ExpiresAt"] = VietnamTime(booking.QuoteExpiresAt!.Value)
                            });
                    });
                    processed++;
                }
            }

            return processed;
        }

        // ---------------------------------------------------------------------------------------------

        // Khách phải tích đồng ý đúng phiên bản Chính sách dịch vụ hiện hành
        private async Task EnsurePolicyAcceptedAsync(bool acceptPolicy, string? policyVersion)
        {
            if (!acceptPolicy)
            {
                throw new BusinessException("You must accept the service policy to continue.");
            }
            string current = (await _settingService.GetPolicySettingsAsync()).PolicyVersion;
            if (string.IsNullOrWhiteSpace(policyVersion) || policyVersion.Trim() != current)
            {
                throw new BusinessException($"The service policy has changed (current version {current}). Please read the latest policy and accept it again.");
            }
        }

        private async Task<BookingRequest> GetOwnBookingAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId)
                ?? throw new NotFoundException($"Booking with ID {bookingId} was not found.");
            if (booking.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only answer the quote of your own booking.");
            }
            return booking;
        }

        private static void CancelHorses(BookingRequest booking)
        {
            foreach (var horse in booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled))
            {
                horse.Status = BookingHorseStatuses.Cancelled;
            }
        }

        private static string VietnamTime(DateTimeOffset value) =>
            value.ToOffset(VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
