namespace RacehorseTransport.API.ResponseModels
{
    public class HorseResponse
    {
        public int HorseId { get; set; }
        public int OwnerUserId { get; set; }
        public string? OwnerName { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Color { get; set; } = string.Empty;
        public string? SpecialCareRequirements { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsPregnant { get; set; }
        public DateOnly? ExpectedFoalingDate { get; set; }
        public DateOnly? LastFoalingDate { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class HorseDocumentResponse
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
}
