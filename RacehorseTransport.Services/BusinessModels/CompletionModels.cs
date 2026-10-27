namespace RacehorseTransport.Services.BusinessModels
{
    // Biên bản bàn giao e-POD của MỘT đơn trên chuyến (chuyến ghép nhiều đơn thì mỗi đơn một biên bản)
    public class CreateHandoverModel
    {
        public int BookingId { get; set; }

        // Giao tại chỗ (R-12): con ngựa rời chuyến với hướng xử lý HandOverHere. Bỏ trống = giao ở điểm trả của đơn.
        public int? TripHorseId { get; set; }

        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientIdCard { get; set; } = string.Empty;
        public string OverallCondition { get; set; } = string.Empty;
        public string? Remarks { get; set; }

        // Mã vi mạch quét được khi giao: phải khớp đúng các con ngựa của đơn trên chuyến
        public List<string> MicrochipNumbers { get; set; } = new();

        // Ảnh chữ ký vẽ tay của người nhận
        public FileUploadModel Signature { get; set; } = null!;
    }

    public class HandoverModel
    {
        public int HandoverId { get; set; }
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string DropoffAddress { get; set; } = string.Empty;
        public int? CheckpointId { get; set; } // mốc nơi ký nhận (DropOff của đơn hoặc Destination)
        public DateTimeOffset HandoverDateTime { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientIdCard { get; set; } = string.Empty;
        public string? SignatureUrl { get; set; } // link xem ảnh chữ ký
        public string OverallCondition { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public int DriverUserId { get; set; }
        public string? DriverName { get; set; }
        public List<HandoverHorseModel> Horses { get; set; } = new();
    }

    // Ngựa của đơn được giao trong biên bản
    public class HandoverHorseModel
    {
        public int TripHorseId { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
    }

    // Kết quả chốt KPI của một chuyến (xem TripKpiCalculator)
    public class TripKpiResult
    {
        public int DelayMinutes { get; set; }
        public string OnTimeStatus { get; set; } = string.Empty;
        public decimal KpiScore { get; set; }
    }

    // Báo cáo tổng hợp các chuyến đã đóng trong một khoảng thời gian
    public class ReportSummaryModel
    {
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public string CurrencyCode { get; set; } = "USD";

        public int CompletedTrips { get; set; }
        public int CompletedBookings { get; set; }
        public int HorsesDelivered { get; set; }

        public decimal Revenue { get; set; }        // tổng giá các đơn đã hoàn tất
        public decimal OperatingCost { get; set; }  // tổng chi phí thực tế của các chuyến
        public decimal GrossProfit { get; set; }

        public int OnTimeTrips { get; set; }
        public int DelayedForceMajeureTrips { get; set; }
        public int DelayedOperationalFaultTrips { get; set; }
        public decimal OnTimeRate { get; set; }     // % chuyến đúng giờ
        public decimal AverageKpiScore { get; set; }
        public decimal AverageDelayMinutes { get; set; }

        public int TotalIncidents { get; set; }
        public decimal IncidentAdditionalCost { get; set; }
        public List<IncidentTypeCountModel> IncidentsByType { get; set; } = new();
    }

    public class IncidentTypeCountModel
    {
        public string IncidentType { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
