namespace RacehorseTransport.Services.BusinessModels
{
    // Một mốc trung gian Coordinator thêm vào tuyến (điểm đầu / cuối lấy từ đơn)
    public class RoutePlanStopInputModel
    {
        public string Name { get; set; } = string.Empty;
        public string StopType { get; set; } = string.Empty;   // RestStop | BorderGate
        public string Address { get; set; } = string.Empty;
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public string? CountryCode { get; set; }
        public int RestMinutes { get; set; }
    }

    public class SaveRoutePlanModel
    {
        public List<RoutePlanStopInputModel> Stops { get; set; } = new();
        public string? Notes { get; set; }

        // Đơn cũ chưa có tọa độ điểm đón / trả: Coordinator gửi kèm (ghim trên bản đồ)
        public decimal? OriginLat { get; set; }
        public decimal? OriginLng { get; set; }
        public decimal? DestinationLat { get; set; }
        public decimal? DestinationLng { get; set; }
    }

    public class RoutePlanStopModel
    {
        public int SequenceOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string StopType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public string? CountryCode { get; set; }
        public decimal? DistanceKmFromPrevious { get; set; }
        public decimal? DurationHoursFromPrevious { get; set; }
        public int RestMinutes { get; set; }
    }

    // Cảnh báo một chặng đổi nước mà thiếu mốc cửa khẩu
    public class RoutePlanWarningModel
    {
        public int FromIndex { get; set; }   // vị trí mốc trong Stops (0 = điểm đầu)
        public int ToIndex { get; set; }
        public List<string> CountryCodes { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class RoutePlanModel
    {
        public int RoutePlanId { get; set; }                  // 0 khi chỉ xem trước (chưa lưu)
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<RoutePlanStopModel> Stops { get; set; } = new();
        public List<string> TransitCountryCodes { get; set; } = new();
        public decimal TotalDistanceKm { get; set; }
        public decimal EstimatedHours { get; set; }
        public bool IsEstimate { get; set; }
        public List<GeoPointModel> Polyline { get; set; } = new();
        public List<RoutePlanWarningModel> Warnings { get; set; } = new();
        public string? Notes { get; set; }
        public int PlannedByUserId { get; set; }
        public string? PlannedByName { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
