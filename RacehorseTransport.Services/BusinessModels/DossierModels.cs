namespace RacehorseTransport.Services.BusinessModels
{
    public class DossierFilterOptions : BusinessQueryOptions
    {
        public string? Status { get; set; }
        public int? BookingId { get; set; }
    }

    // Nộp (hoặc nộp lại) một giấy tờ, file gửi kèm trong cùng request.
    public class UploadDocumentModel
    {
        public int DocTypeId { get; set; }
        public string? DocumentNumber { get; set; }
        public FileUploadModel File { get; set; } = null!;
        public DateOnly? ExpiryDate { get; set; }
    }

    // Dòng trong danh sách hồ sơ
    public class DossierSummaryModel
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
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public int SpecialistUserId { get; set; }
        public string? SpecialistName { get; set; }
        public string? ClearanceNumber { get; set; }
        public DateTimeOffset? ClearedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class DossierDetailModel : DossierSummaryModel
    {
        public string PassportNumber { get; set; } = string.Empty;
        public string? IssueReason { get; set; }

        // Giấy tờ bắt buộc theo quy định của nước đi (Export) và nước đến (Import), đối chiếu với giấy đã nộp
        public List<DossierChecklistItemModel> Checklist { get; set; } = new();
        public int RequiredCount { get; set; }
        public int ApprovedCount { get; set; }
        public bool IsReadyToSubmit { get; set; } // mọi giấy bắt buộc đã được duyệt

        // Toàn bộ giấy tờ đã nộp (kể cả các bản bị từ chối trước đó)
        public List<DossierDocumentModel> Documents { get; set; } = new();
    }

    public class DossierChecklistItemModel
    {
        public int DocTypeId { get; set; }
        public string DocTypeCode { get; set; } = string.Empty;
        public string DocTypeName { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
        public int? ValidityDays { get; set; } // giấy phải được cấp trong vòng N ngày trước ngày đi
        public string? RegulationNote { get; set; }
        public List<string> RequiredBy { get; set; } = new(); // ví dụ: "VN Export", "CN Import"

        // Missing | Pending | Approved | Rejected (theo bản nộp mới nhất của loại giấy này)
        public string State { get; set; } = ChecklistStates.Missing;
        public DossierDocumentModel? LatestDocument { get; set; }
    }

    public static class ChecklistStates
    {
        public const string Missing = "Missing";
    }

    public class DossierDocumentModel
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
