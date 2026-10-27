namespace RacehorseTransport.Services.BusinessModels
{
    // R-16 (BE-16): đoàn xe ghi không giao được
    public class RecordDeliveryFailureModel
    {
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }
        public List<string>? PhotoPaths { get; set; }
    }

    // R-16 (BE-16): khách chọn cách xử lý
    public class DeliveryDecisionModel
    {
        public string Decision { get; set; } = string.Empty;
        public string? RecipientName { get; set; }
        public string? RecipientPhone { get; set; }
        public string? Address { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
    }

    public class DeliveryFailureModel
    {
        public int FailureId { get; set; }
        public int TripId { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int? CheckpointId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }
        public List<string> PhotoPaths { get; set; } = new();
        public DateTimeOffset RecordedAt { get; set; }
        public int? IncidentId { get; set; }
        public int WaitingHours { get; set; }
        public decimal WaitingFee { get; set; }
        public bool CustomerPays { get; set; }
        public DateTimeOffset? DecisionDueAt { get; set; }
        public string? Decision { get; set; }
        public DateTimeOffset? DecidedAt { get; set; }
        public bool DecidedBySystem { get; set; }
        public decimal DecisionCost { get; set; }
        public string? AlternateRecipientName { get; set; }
        public string? AlternateRecipientPhone { get; set; }
        public string? AlternateAddress { get; set; }
        public decimal? ExtraDistanceKm { get; set; }
    }
}
