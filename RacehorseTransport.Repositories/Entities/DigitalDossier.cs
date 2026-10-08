using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // clearance.digital_dossiers
    public class DigitalDossier
    {
        public int DossierId { get; set; }
        public string DossierCode { get; set; } = string.Empty;
        public int BookingHorseId { get; set; }
        public int SpecialistUserId { get; set; }
        public string Status { get; set; } = DossierStatuses.Draft;
        public string? ClearanceNumber { get; set; }
        public string? IssueReason { get; set; }
        public DateTimeOffset? ClearedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual BookingHorse? BookingHorse { get; set; }
        public virtual User? Specialist { get; set; }
        public virtual ICollection<DossierDocument> Documents { get; set; } = new List<DossierDocument>();
    }
}
