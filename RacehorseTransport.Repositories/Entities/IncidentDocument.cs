namespace RacehorseTransport.Repositories.Entities
{
    // incident.incident_documents
    public class IncidentDocument
    {
        public int IncidentDocumentId { get; set; }
        public int IncidentId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string? Note { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual TripIncident? Incident { get; set; }
        public virtual User? UploadedBy { get; set; }
    }
}
