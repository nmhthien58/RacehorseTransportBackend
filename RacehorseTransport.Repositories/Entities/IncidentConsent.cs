using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // incident.incident_consents: ý kiến từng đơn về phương án đi đường vòng tốn thêm tiền (R-14, BE-14b)
    // hoặc ý kiến của chủ ngựa về khuyến nghị an tử
    public class IncidentConsent
    {
        public int ConsentId { get; set; }
        public int IncidentId { get; set; }
        public string ConsentType { get; set; } = ConsentTypes.Detour;
        public int BookingId { get; set; }
        public int CustomerUserId { get; set; }
        public bool Accepted { get; set; }
        public DateTimeOffset DecidedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual TripIncident? Incident { get; set; }
        public virtual BookingRequest? Booking { get; set; }
    }
}
