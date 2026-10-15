namespace RacehorseTransport.Repositories.Entities
{
    // racehorse.horse_documents
    public class HorseDocument
    {
        public int DocumentId { get; set; }
        public int HorseId { get; set; }
        public int DocTypeId { get; set; }
        public string? DocumentNumber { get; set; }
        public string? IssuedBy { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual Horse? Horse { get; set; }
        public virtual DocumentType? DocumentType { get; set; }
        public virtual User? UploadedBy { get; set; }
    }
}
