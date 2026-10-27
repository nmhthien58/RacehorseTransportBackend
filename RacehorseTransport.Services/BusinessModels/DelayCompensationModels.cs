namespace RacehorseTransport.Services.BusinessModels
{
    public class DelayCompensationModel
    {
        public int DelayCompensationId { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public decimal DelayHours { get; set; }
        public decimal FreightAmount { get; set; }
        public decimal Percent { get; set; }
        public decimal CalculatedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ReviewReason { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
