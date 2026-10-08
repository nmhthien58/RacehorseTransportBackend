using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // clearance.dossier_documents
    public class DossierDocument
    {
        public int DocumentId { get; set; }
        public int DossierId { get; set; }
        public int DocTypeId { get; set; }
        public string? DocumentNumber { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public int UploadedByUserId { get; set; }
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
        public string Status { get; set; } = DocumentStatuses.Pending;
        public string? CorrectionNote { get; set; }
        public int? ReviewedByUserId { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public int RejectionCount { get; set; }

        // Navigation properties
        public virtual DigitalDossier? Dossier { get; set; }
        public virtual DocumentType? DocumentType { get; set; }
        public virtual User? UploadedBy { get; set; }
        public virtual User? ReviewedBy { get; set; }
    }
}
