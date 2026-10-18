namespace RacehorseTransport.API.ResponseModels
{
    public class DossierSummaryResponse
    {
        public int DossierId { get; set; }
        public string DossierCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int BookingHorseId { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string PickupCountryCode { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public List<string> TransitCountryCodes { get; set; } = new();
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public int SpecialistUserId { get; set; }
        public string? SpecialistName { get; set; }
        public string? ClearanceNumber { get; set; }
        public DateTimeOffset? ClearedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class DossierDetailResponse : DossierSummaryResponse
    {
        public string PassportNumber { get; set; } = string.Empty;
        public string? IssueReason { get; set; }
        public List<DossierChecklistItemResponse> Checklist { get; set; } = new();
        public int RequiredCount { get; set; }
        public int ApprovedCount { get; set; }
        public bool IsReadyToSubmit { get; set; }
        public List<DossierDocumentResponse> Documents { get; set; } = new();
    }

    public class DossierChecklistItemResponse
    {
        public int DocTypeId { get; set; }
        public string DocTypeCode { get; set; } = string.Empty;
        public string DocTypeName { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
        public int? ValidityDays { get; set; }
        public string? RegulationNote { get; set; }
        public List<string> RequiredBy { get; set; } = new();
        public string State { get; set; } = string.Empty; // Missing | Pending | Approved | Rejected
        public DossierDocumentResponse? LatestDocument { get; set; }
    }

    public class DossierDocumentResponse
    {
        public int DocumentId { get; set; }
        public int DocTypeId { get; set; }
        public string DocTypeCode { get; set; } = string.Empty;
        public string DocTypeName { get; set; } = string.Empty;
        public string? DocumentNumber { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CorrectionNote { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public int RejectionCount { get; set; }
        public int UploadedByUserId { get; set; }
        public string? UploadedByName { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
    }
}
