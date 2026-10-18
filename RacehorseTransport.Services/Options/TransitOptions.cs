namespace RacehorseTransport.Services.Options
{
    // Đọc từ section "Transit" trong appsettings: tham số ước tính thời gian vận chuyển (TransitTimeEstimator).
    // Đây là số liệu giả định hợp lý cho dự án, chỉnh được mà không cần sửa code.
    public class TransitOptions
    {
        public const string SectionName = "Transit";

        // ---------- Đường bộ ----------
        // Tốc độ trung bình của xe tải chở ngựa (chậm hơn xe con, đã tính đường xấu, kẹt xe)
        public decimal TruckAverageSpeedKmh { get; set; } = 55;

        // Lái tối đa bấy nhiêu giờ thì nghỉ (tài xế + cho ngựa uống nước), theo quy định giờ lái EU 561/2006
        public decimal MaxDrivingHoursBeforeBreak { get; set; } = 4.5m;
        public int BreakMinutes { get; set; } = 45;

        // Giờ lái tối đa mỗi ngày: một tài xế / đoàn có từ hai tài xế thay ca
        public decimal MaxDrivingHoursPerDaySingleDriver { get; set; } = 9;
        public decimal MaxDrivingHoursPerDayTeam { get; set; } = 18;

        // Hết giờ lái trong ngày thì nghỉ qua đêm (ngựa xuống chuồng tạm)
        public decimal OvernightRestHours { get; set; } = 10;

        // Mỗi lần qua cửa khẩu (thủ tục hải quan, kiểm dịch biên giới)
        public decimal BorderCrossingHours { get; set; } = 6;

        // Xếp / dỡ ngựa ở mỗi điểm đón hoặc trả
        public decimal LoadingHoursPerStop { get; set; } = 1;

        // ---------- Đường bay ----------
        // Chở ngựa bằng xe từ trang trại tới sân bay (và từ sân bay tới nơi nhận), mỗi đầu
        public decimal AirGroundTransferHours { get; set; } = 4;

        // Hàng động vật sống phải có mặt sớm trước giờ bay
        public decimal AirCheckInHours { get; set; } = 6;

        public decimal AirCruiseSpeedKmh { get; set; } = 800;

        // Dùng khi không biết khoảng cách bay
        public decimal AirDefaultFlightHours { get; set; } = 8;

        // Kiểm tra thú y + hải quan ở sân bay đến
        public decimal AirArrivalInspectionHours { get; set; } = 4;

        // ---------- Chung ----------
        // Biên an toàn cộng thêm để ra mốc "chậm nhất" của khoảng thời gian dự kiến
        public decimal BufferPercent { get; set; } = 15;
    }
}
