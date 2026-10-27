namespace RacehorseTransport.API.ResponseModels
{
    public class RoutePlanResponse
    {
        public int RoutePlanId { get; set; }              // 0 với kết quả xem trước (chưa lưu)
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Status { get; set; } = string.Empty; // Draft | Submitted | Approved | Rejected | Superseded
        public List<RoutePlanStopResponse> Stops { get; set; } = new();
        public List<string> TransitCountryCodes { get; set; } = new();
        public decimal TotalDistanceKm { get; set; }
        public decimal EstimatedHours { get; set; }
        public bool IsEstimate { get; set; }
        public List<double[]> Polyline { get; set; } = new(); // [vĩ độ, kinh độ] để vẽ
        public List<RoutePlanWarningResponse> Warnings { get; set; } = new();
        public string? Notes { get; set; }
        public int PlannedByUserId { get; set; }
        public string? PlannedByName { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class RoutePlanStopResponse
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

    public class RoutePlanWarningResponse
    {
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }
        public List<string> CountryCodes { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
