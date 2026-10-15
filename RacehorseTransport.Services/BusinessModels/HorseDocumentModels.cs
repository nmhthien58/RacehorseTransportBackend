namespace RacehorseTransport.Services.BusinessModels
{
    // Hồ sơ thú y lưu theo ngựa
    public class HorseDocumentModel
    {
        public int DocumentId { get; set; }
        public int HorseId { get; set; }
        public int DocTypeId { get; set; }
        public string DocTypeCode { get; set; } = string.Empty;
        public string DocTypeName { get; set; } = string.Empty;
        public string? DocumentNumber { get; set; }
        public string? IssuedBy { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string Validity { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int UploadedByUserId { get; set; }
        public string? UploadedByName { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }

    public class CreateHorseDocumentModel
    {
        public int DocTypeId { get; set; }
        public string? DocumentNumber { get; set; }
        public string? IssuedBy { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public FileUploadModel File { get; set; } = null!;
    }
}
