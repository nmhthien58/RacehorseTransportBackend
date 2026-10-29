using System.Globalization;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // Giấy khám lâm sàng FIT_EXAM (chính sách mục 5): khách cho ngựa khám trong N giờ trước giờ xuất phát,
    // nộp lên hồ sơ kiểm dịch của đơn (kể cả khi hồ sơ đã thông quan) và chuyên viên phụ trách duyệt.
    // Chỉ giấy đã duyệt mới được tính lúc xuất phát.
    public static class FitExamRules
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        // Khoảng thời gian khách nên cho ngựa khám: [xuất phát - N giờ, xuất phát]
        public static (DateTimeOffset Start, DateTimeOffset End) ExamWindow(DateTimeOffset departureAt, int validHours) =>
            (departureAt.AddHours(-validHours), departureAt);

        // Giấy khám chỉ ghi ngày cấp nên so theo ngày (UTC): cấp từ ngày của mốc "xuất phát - N giờ" tới ngày xuất phát
        public static bool IsIssuedInWindow(DateOnly issueDate, DateTimeOffset departureAt, int validHours)
        {
            var earliest = DateOnly.FromDateTime(departureAt.AddHours(-validHours).UtcDateTime);
            var latest = DateOnly.FromDateTime(departureAt.UtcDateTime);
            return issueDate >= earliest && issueDate <= latest;
        }

        // FIT_EXAM không có hạn dùng riêng nên cột expiry_date lưu NGÀY CẤP (tránh đổi schema);
        // không ghi ngày cấp thì lấy ngày tải lên.
        public static DateOnly IssueDateOf(DateOnly? storedIssueDate, DateTimeOffset uploadedAt) =>
            storedIssueDate ?? DateOnly.FromDateTime(uploadedAt.UtcDateTime);

        // Chỉ giấy đã được chuyên viên duyệt mới cho phép xuất phát
        public static bool CountsAtDeparture(string status) => status == DocumentStatuses.Approved;

        // Dòng checklist cần cho bước nộp cơ quan chức năng / hạn nộp giấy: bắt buộc và không phải giấy nộp sau thông quan
        public static IEnumerable<DossierChecklistItemModel> RequiredForClearance(IEnumerable<DossierChecklistItemModel> checklist) =>
            checklist.Where(c => c.IsMandatory && !c.PostClearance);

        public static string VietnamTime(DateTimeOffset value) =>
            value.ToOffset(VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
