namespace RacehorseTransport.Services.Options
{
    // Đọc từ section "Booking" trong appsettings
    public class BookingOptions
    {
        public const string SectionName = "Booking";

        // Đơn Submitted quá số giờ này mà chưa được duyệt / từ chối thì tự chuyển sang Expired
        public int ReviewSlaHours { get; set; } = 48;

        // Chu kỳ quét đơn quá hạn của BookingExpiryJob
        public int ExpiryCheckMinutes { get; set; } = 10;

        // ---------- Thời gian đặt trước tối thiểu (R-01) ----------
        // Ngày pickup sớm nhất = hôm nay + thời gian duyệt + hạn đặt cọc + thời gian làm hồ sơ + dự phòng xếp chuyến / thanh toán.

        // Hạn khách đặt cọc sau khi đơn được duyệt
        public int DepositDays { get; set; } = 2;

        // Thời gian làm hồ sơ kiểm dịch khi chưa có số liệu riêng theo nước
        public int DefaultClearanceDays { get; set; } = 14;

        // Dự phòng cho xếp chuyến và thanh toán phần còn lại trước ngày chạy
        public int PlanningBufferDays { get; set; } = 3;

        // Khoảng pickup dài tối đa (khoảng quá rộng thì không xếp chuyến được)
        public int MaxPickupWindowDays { get; set; } = 30;

        // Express: pickup trong vòng bấy nhiêu ngày sau ngày sớm nhất (phải làm hồ sơ gấp) ...
        public int ExpressWindowDays { get; set; } = 7;

        // ... hoặc hạn giao chỉ dư ít hơn bấy nhiêu giờ so với thời gian vận chuyển chậm nhất
        public int TightDeadlineHours { get; set; } = 24;

        // ---------- Khoảng giá ước tính khi tạo đơn (R-10) ----------
        // Giá cao nhất: quãng đường A -> B × hệ số này (tuyến chốt thường dài hơn đường ngắn nhất: vòng qua cửa khẩu, trạm nghỉ)
        public decimal QuoteRangeDistanceFactor { get; set; } = 1.25m;

        // ... và cộng phí thủ tục quá cảnh cho các nước đường A -> B đi qua, tối đa bấy nhiêu nước (giá thấp nhất không tính phí này)
        public int QuoteMaxTransitCountries { get; set; } = 2;

        // ---------- Kiểm tra trước khi xuất phát (BE-10) ----------
        // Giấy khám lâm sàng đủ sức khỏe phải cấp trong bấy nhiêu giờ trước giờ xuất phát (chính sách mục 5)
        public int FitExamValidHours { get; set; } = 48;

        // Nhắc khách đi khám lần hai khi còn bấy nhiêu ngày tới giờ xuất phát mà ngựa chưa có giấy khám nộp lên hồ sơ
        public int FitExamReminderDaysBeforeDeparture { get; set; } = 3;

        // Số ngày cần tính từ lúc DUYỆT đơn tới ngày pickup, với thời gian làm hồ sơ của tuyến (R-03)
        public int ApprovalLeadDaysFor(int clearanceDays) => DepositDays + clearanceDays + PlanningBufferDays;

        // Số ngày cần tính từ lúc TẠO đơn tới ngày pickup
        public int LeadTimeDaysFor(int clearanceDays) => (int)Math.Ceiling(ReviewSlaHours / 24.0) + ApprovalLeadDaysFor(clearanceDays);

        // Khi tuyến chưa có số liệu làm hồ sơ riêng
        public int ApprovalLeadDays => ApprovalLeadDaysFor(DefaultClearanceDays);
        public int LeadTimeDays => LeadTimeDaysFor(DefaultClearanceDays);
    }
}
