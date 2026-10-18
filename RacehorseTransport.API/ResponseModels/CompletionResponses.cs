namespace RacehorseTransport.API.ResponseModels
{
    public class HandoverResponse
    {
        public int HandoverId { get; set; }
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string DropoffAddress { get; set; } = string.Empty;
        public int? CheckpointId { get; set; }
        public DateTimeOffset HandoverDateTime { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientIdCard { get; set; } = string.Empty;
        public string? SignatureUrl { get; set; }
        public string OverallCondition { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public int DriverUserId { get; set; }
        public string? DriverName { get; set; }
        public List<HandoverHorseResponse> Horses { get; set; } = new();
    }

    public class HandoverHorseResponse
    {
        public int TripHorseId { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
    }

    public class ReportSummaryResponse
    {
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public int CompletedTrips { get; set; }
        public int CompletedBookings { get; set; }
        public int HorsesDelivered { get; set; }
        public decimal Revenue { get; set; }
        public decimal OperatingCost { get; set; }
        public decimal GrossProfit { get; set; }
        public int OnTimeTrips { get; set; }
        public int DelayedForceMajeureTrips { get; set; }
        public int DelayedOperationalFaultTrips { get; set; }
        public decimal OnTimeRate { get; set; }
        public decimal AverageKpiScore { get; set; }
        public decimal AverageDelayMinutes { get; set; }
        public int TotalIncidents { get; set; }
        public decimal IncidentAdditionalCost { get; set; }
        public List<IncidentTypeCountResponse> IncidentsByType { get; set; } = new();
    }

    public class IncidentTypeCountResponse
    {
        public string IncidentType { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
