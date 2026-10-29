using System.Globalization;
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
    //   Báo giá chốt do IFinalQuoteIssuer phát hành (đường bộ: khi duyệt tuyến; đường bay: khi duyệt sơ bộ)
    //   -> khách chấp nhận (đồng ý chính sách hiện hành) -> AwaitingDeposit, cọc tính trên báo giá chốt, hạn cọc = hạn báo giá
    //   -> quá hạn mà chưa chấp nhận hoặc chưa cọc -> QuoteExpired (không mất phí). Khách từ chối -> Cancelled.
    public class BookingQuoteService : IBookingQuoteService
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly ISystemSettingService _settingService;
        private readonly IBookingService _bookingService;
        private readonly IBookingActivator _bookingActivator;
        private readonly INotificationService _notificationService;
        private readonly IContractService _contractService;
        private readonly IUnitOfWork _unitOfWork;

        public BookingQuoteService(
            IRepository<BookingRequest> bookingRepo,
            ISystemSettingService settingService,
            IBookingService bookingService,
            IBookingActivator bookingActivator,
            INotificationService notificationService,
            IContractService contractService,
            IUnitOfWork unitOfWork)
        {
            _bookingRepo = bookingRepo;
            _settingService = settingService;
            _bookingService = bookingService;
            _bookingActivator = bookingActivator;
            _notificationService = notificationService;
            _contractService = contractService;
            _unitOfWork = unitOfWork;
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
