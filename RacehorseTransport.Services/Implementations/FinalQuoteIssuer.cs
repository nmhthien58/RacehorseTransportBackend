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
    // R-10 (Chính sách mục 2, 3): báo giá chốt.
    //   Đường bộ: Manager duyệt tuyến -> giá theo tuyến (quãng đường thật, phí từng nước quá cảnh).
    //   Đường bay: Manager duyệt sơ bộ -> giá theo con theo nước đến (không cần tuyến trên bản đồ).
    //   Cả hai: bảo hiểm từng con, Express như lúc gửi đơn -> đơn Quoted, hạn = bây giờ + Booking.QuoteAcceptDays, email QuoteReady.
    public class FinalQuoteIssuer : IFinalQuoteIssuer
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly CultureInfo VietnameseCulture = new("vi-VN");

        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<RoutePlan> _planRepo;
        private readonly IPricingService _pricingService;
        private readonly ISystemSettingService _settingService;
        private readonly INotificationService _notificationService;
        private readonly IContractService _contractService;

        public FinalQuoteIssuer(
            IRepository<BookingRequest> bookingRepo,
            IRepository<RoutePlan> planRepo,
            IPricingService pricingService,
            ISystemSettingService settingService,
            INotificationService notificationService,
            IContractService contractService)
        {
            _bookingRepo = bookingRepo;
            _planRepo = planRepo;
            _pricingService = pricingService;
            _settingService = settingService;
            _notificationService = notificationService;
            _contractService = contractService;
        }

        public async Task IssueAsync(int bookingId, int? routePlanId)
        {
            var booking = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Horse)
                .Include(b => b.TransitCountries)
                .FirstAsync(b => b.BookingId == bookingId);
            var plan = routePlanId == null
                ? null
                : await _planRepo.GetQueryable().AsNoTracking().FirstAsync(p => p.RoutePlanId == routePlanId.Value);

            var policy = await _settingService.GetPolicySettingsAsync();
            var payment = await _settingService.GetPaymentSettingsAsync();
            var horses = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).ToList();

            var quote = await ComputeAsync(booking, horses, plan, policy.InsurancePremiumPercent);
            decimal total = quote.Total;

            var now = DateTimeOffset.UtcNow;
            booking.EstimatedCost = total;
            booking.CurrencyCode = quote.CurrencyCode;
            booking.QuoteBreakdown = _pricingService.SerializeLines(quote.Lines);
            booking.EstimatedTransitHours = plan != null ? plan.EstimatedHours : quote.EstimatedHours ?? booking.EstimatedTransitHours;
            booking.QuotedAt = now;
            booking.QuoteExpiresAt = QuoteRules.ExpiresAt(now, policy.QuoteAcceptDays);
            booking.QuoteReminderSentAt = null;
            booking.Status = BookingStatuses.Quoted;
            await _bookingRepo.SaveChangesAsync();

            // R-17: hợp đồng nháp theo báo giá này (bản nháp cũ, nếu có, bị thay)
            await _contractService.CreateDraftAsync(booking.BookingId, ContractReasons.Initial);

            string intro = plan == null ? "Your booking passed review." : "The route is planned.";
            await _notificationService.SendAsync(
                booking.CustomerUserId,
                $"Final quote for booking {booking.BookingCode}",
                $"{intro} Final price: {total:N2} {booking.CurrencyCode}. Accept the quote and pay the deposit by {VietnamTime(booking.QuoteExpiresAt.Value)} (Vietnam time); after that the booking closes without any fee.",
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

        // Hàm thuần (dễ test): đầu vào tính giá chốt. Đường bộ lấy quãng đường + nước quá cảnh từ tuyến đã duyệt;
        // đường bay không có tuyến, giá tính theo con nên không cần km.
        // Lịch đã kiểm tra lúc khách gửi đơn; giữ Express như lúc đó để không bắt khách đổi ngày vì công ty duyệt lâu.
        // Manager xem trước báo giá chốt của một tuyến đang chờ duyệt: tính y như lúc phát hành, không lưu gì
        public async Task<QuoteModel> PreviewAsync(int routePlanId)
        {
            var plan = await _planRepo.GetQueryable().AsNoTracking().FirstOrDefaultAsync(p => p.RoutePlanId == routePlanId)
                ?? throw new NotFoundException($"Route plan with ID {routePlanId} was not found.");
            var booking = await _bookingRepo.GetQueryable().AsNoTracking()
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Horse)
                .Include(b => b.TransitCountries)
                .FirstAsync(b => b.BookingId == plan.BookingId);
            var policy = await _settingService.GetPolicySettingsAsync();
            var horses = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).ToList();
            return await ComputeAsync(booking, horses, plan, policy.InsurancePremiumPercent);
        }

        private async Task<QuoteModel> ComputeAsync(BookingRequest booking, List<BookingHorse> horses, RoutePlan? plan, decimal insurancePremiumPercent)
        {
            var quote = await _pricingService.CalculateQuoteAsync(BuildQuoteInput(booking, horses, plan));

            // R-13: phí bảo hiểm toàn giá trị theo từng con (thay dòng INS_TRANSIT cũ)
            quote.Lines.AddRange(QuoteRules.InsuranceLines(
                horses.Select(bh => new QuoteRules.InsuredHorse(bh.Horse?.Name ?? $"#{bh.HorseId}", bh.InsurancePlan, bh.AgreedValue, bh.DeclaredValue)),
                insurancePremiumPercent));
            // Đơn nối tiếp do lỗi công ty (Chính sách 9.6): miễn toàn bộ, cọc 0 nên chấp nhận là đơn chạy ngay
            if (booking.IsFreeOfCharge)
            {
                decimal subtotal = quote.Lines.Sum(l => l.Amount);
                quote.Lines.Add(new QuoteLineModel
                {
                    Code = "CONTINUATION_WAIVER",
                    Name = "Continuation booking waived (incident was the company's fault)",
                    Quantity = 1,
                    UnitPrice = -subtotal,
                    Amount = -subtotal
                });
            }
            quote.Total = quote.Lines.Sum(l => l.Amount);
            return quote;
        }

        public static QuoteInputModel BuildQuoteInput(BookingRequest booking, IReadOnlyList<BookingHorse> horses, RoutePlan? plan)
        {
            if (plan == null && booking.TransportMode != TransportModes.Air)
            {
                throw new BusinessException($"Booking {booking.BookingCode} is ground transport: approve its route plan to issue the final quote.");
            }

            return new QuoteInputModel
            {
                TransportMode = booking.TransportMode,
                PickupCountryCode = booking.PickupCountryCode,
                DropoffCountryCode = booking.DropoffCountryCode,
                DistanceKm = plan != null ? (int)Math.Ceiling(plan.TotalDistanceKm) : booking.DistanceKm,
                RequiresClimateControl = booking.RequiresClimateControl,
                DeclaredValue = null, // bảo hiểm tính theo từng con
                PickupWindowStart = booking.PickupWindowStart,
                PickupWindowEnd = booking.PickupWindowEnd,
                DeliveryDeadline = booking.DeliveryDeadline,
                TransitCountryCodes = plan != null
                    ? JsonSerializer.Deserialize<List<string>>(plan.TransitCountryCodes) ?? new List<string>()
                    : booking.TransitCountries.OrderBy(t => t.SequenceOrder).Select(t => t.CountryCode).ToList(),
                QuotedAt = booking.CreatedAt,
                StallClasses = horses.Select(bh => bh.StallClass).ToList(),
                SkipScheduleCheck = true,
                ForceExpress = booking.IsExpress
            };
        }

        private static string VietnamTime(DateTimeOffset value) =>
            value.ToOffset(VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
