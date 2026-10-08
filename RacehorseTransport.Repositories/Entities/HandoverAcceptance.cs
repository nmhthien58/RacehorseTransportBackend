namespace RacehorseTransport.Repositories.Entities
{
    // acceptance.handover_acceptances (mỗi đơn trên chuyến có 1 biên bản e-POD riêng)
    public class HandoverAcceptance
    {
        public int HandoverId { get; set; }
        public int TripId { get; set; }
        public int BookingId { get; set; }
        public DateTimeOffset HandoverDateTime { get; set; } = DateTimeOffset.UtcNow;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientIdCard { get; set; } = string.Empty;
        public string DigitalSignatureUrl { get; set; } = string.Empty;
        public string OverallCondition { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public int DriverUserId { get; set; }

        // Navigation properties
        public virtual Trip? Trip { get; set; }
        public virtual BookingRequest? Booking { get; set; }
        public virtual User? Driver { get; set; }
    }
}
