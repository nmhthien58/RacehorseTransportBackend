using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // R-09 / Chính sách mục 7: bồi thường giao trễ. Hàm thuần để dễ viết unit test.
    //   giờ trễ = lúc ký e-POD - hạn giao của đơn
    //   trễ <= Delay.GraceHours: không bồi thường
    //   trễ hơn: Delay.PercentPer12Hours % tiền cước cho mỗi 12 giờ (chưa đủ 12 giờ tính tròn lên), tối đa Delay.MaxPercent %
    //   chỉ khi chuyến trễ do lỗi công ty
    //   tiền cước = báo giá - bảo hiểm - phí thủ tục (giấy tờ, thông quan, kiểm dịch, quá cảnh)
    public static class DelayCompensationRules
    {
        // Phí thủ tục nộp cho cơ quan / bên thứ ba: không tính vào tiền cước
        public static readonly string[] ProceduralFeeCodes = { "FEE_DOCS", "FEE_CLEARANCE", "FEE_QUARANTINE", "FEE_TRANSIT" };
        public const string InsuranceCodePrefix = "INS_";

        // Đơn cũ không lưu chi tiết báo giá: không tách được bảo hiểm / phí thủ tục, lấy cả tổng báo giá
        public static decimal FreightAmount(IReadOnlyCollection<QuoteLineModel> quoteLines, decimal quoteTotal)
        {
            return quoteLines.Count == 0 ? Math.Max(0, quoteTotal) : FreightAmount(quoteLines);
        }

        public static decimal FreightAmount(IEnumerable<QuoteLineModel> quoteLines)
        {
            decimal freight = quoteLines
                .Where(l => !l.Code.StartsWith(InsuranceCodePrefix, StringComparison.OrdinalIgnoreCase)
                    && !ProceduralFeeCodes.Contains(l.Code, StringComparer.OrdinalIgnoreCase))
                .Sum(l => l.Amount);
            return Math.Max(0, freight);
        }

        public static decimal DelayHours(DateTimeOffset deliveredAt, DateTimeOffset deadline) =>
            Math.Max(0, Math.Round((decimal)(deliveredAt - deadline).TotalHours, 2));

        // % tiền cước được bồi thường cho số phút trễ (tính theo phút để 6h01 khác 6h)
        public static decimal Percent(double delayMinutes, PolicySettingsModel settings)
        {
            double graceMinutes = settings.DelayGraceHours * 60.0;
            if (delayMinutes <= graceMinutes)
            {
                return 0;
            }
            int steps = (int)Math.Ceiling((delayMinutes - graceMinutes) / (12 * 60.0));
            return Math.Min(steps * settings.DelayPercentPer12Hours, settings.DelayMaxPercent);
        }

        public static decimal Amount(decimal freight, decimal percent) => Math.Round(freight * percent / 100, 2);

        // Chuyến trễ do lỗi công ty: không có sự cố nào được chấp nhận (trễ vì vận hành),
        // hoặc có ít nhất một sự cố do công ty. Mọi sự cố đều do khách / bất khả kháng thì không phải lỗi công ty.
        public static bool IsCompanyFault(IReadOnlyCollection<string?> acceptedIncidentFaultParties)
        {
            return acceptedIncidentFaultParties.Count == 0
                || acceptedIncidentFaultParties.Any(f => f == null || f == FaultParties.Company);
        }
    }
}
