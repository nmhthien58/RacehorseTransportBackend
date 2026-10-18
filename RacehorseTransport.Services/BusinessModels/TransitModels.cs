namespace RacehorseTransport.Services.BusinessModels
{
    // Đầu vào ước tính thời gian vận chuyển của một hành trình
    public class TransitEstimateInputModel
    {
        public string TransportMode { get; set; } = string.Empty;

        // Đường bộ: quãng đường lái; đường bay: khoảng cách bay (để trống thì dùng giờ bay mặc định)
        public int? DistanceKm { get; set; }

        // Số cửa khẩu phải qua (đường bộ)
        public int BorderCrossings { get; set; }

        // Số tài xế thay ca: từ 2 trở lên được lái nhiều giờ hơn mỗi ngày
        public int DriverCount { get; set; } = 1;

        // Số điểm đón + trả (mặc định 1 điểm đón, 1 điểm trả)
        public int Stops { get; set; } = 2;
    }

    // Kết quả ước tính, tách theo từng thành phần để hiển thị cho người dùng hiểu vì sao ra con số này
    public class TransitEstimateModel
    {
        public decimal DrivingHours { get; set; }
        public decimal RestHours { get; set; }   // nghỉ giữa chặng + nghỉ qua đêm
        public int OvernightStops { get; set; }
        public decimal BorderHours { get; set; }
        public decimal HandlingHours { get; set; } // xếp / dỡ ngựa
        public decimal FlightHours { get; set; }   // chỉ đường bay: bay + có mặt trước + kiểm tra ở nơi đến

        public decimal TotalHours { get; set; }    // thời gian dự kiến
        public decimal MaxHours { get; set; }      // đã cộng biên an toàn: mốc "chậm nhất"
        public int TransitDays { get; set; }       // số ngày lịch (làm tròn lên), dùng tính phí áp tải theo ngày
    }
}
