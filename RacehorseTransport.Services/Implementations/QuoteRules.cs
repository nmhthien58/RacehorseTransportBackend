using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // R-10 / R-13: báo giá chốt, hạn chấp nhận, cọc. Hàm thuần để dễ viết unit test.
    public static class QuoteRules
    {
        public const string InsuranceFullCode = "INS_FULL";

        public sealed record InsuredHorse(string HorseName, string InsurancePlan, decimal? AgreedValue, decimal? DeclaredValue);

        // Phí bảo hiểm toàn giá trị: Insurance.PremiumPercent % giá trị thỏa thuận của từng con chọn gói Full (Chính sách mục 8.2)
        public static List<QuoteLineModel> InsuranceLines(IEnumerable<InsuredHorse> horses, decimal premiumPercent)
        {
            var lines = new List<QuoteLineModel>();
            foreach (var horse in horses.Where(h => h.InsurancePlan == InsurancePlans.Full))
            {
                decimal value = HorseValueRules.EffectiveValue(horse.AgreedValue, horse.DeclaredValue) ?? 0;
                decimal premium = Math.Round(value * premiumPercent / 100, 2, MidpointRounding.AwayFromZero);
                if (premium <= 0)
                {
                    continue;
                }
                lines.Add(new QuoteLineModel
                {
                    Code = InsuranceFullCode,
                    Name = $"Bảo hiểm toàn giá trị ({premiumPercent:0.##}%) - {horse.HorseName}",
                    Quantity = 1,
                    UnitPrice = premium,
                    Amount = premium
                });
            }
            return lines;
        }

        // Đường bay không lập tuyến trên bản đồ: báo giá chốt phát hành ngay khi Manager duyệt sơ bộ
        public static bool QuotesOnPreApproval(string transportMode) => transportMode == TransportModes.Air;

        // Duyệt sơ bộ từ Submitted. Đơn bay đã kẹt ở PreApproved (duyệt trước khi có báo giá đường bay) duyệt lại được để ra báo giá.
        public static bool CanPreApprove(string transportMode, string status) =>
            status == BookingStatuses.Submitted
            || (QuotesOnPreApproval(transportMode) && status == BookingStatuses.PreApproved);

        public static DateTimeOffset ExpiresAt(DateTimeOffset quotedAt, int acceptDays) => quotedAt.AddDays(acceptDays);

        // Hết hạn đúng tại thời điểm hạn (không còn chấp nhận / cọc được từ giây đó)
        public static bool IsExpired(DateTimeOffset? expiresAt, DateTimeOffset now) => expiresAt.HasValue && now >= expiresAt.Value;

        // Nhắc một lần khi còn từ 1 ngày trở xuống
        public static bool NeedsReminder(DateTimeOffset? expiresAt, DateTimeOffset? remindedAt, DateTimeOffset now) =>
            expiresAt.HasValue && remindedAt == null && now < expiresAt.Value && expiresAt.Value - now <= TimeSpan.FromDays(1);

        public static decimal DepositAmount(decimal finalQuote, decimal depositPercent) =>
            Math.Round(finalQuote * depositPercent / 100, 2);
    }
}
