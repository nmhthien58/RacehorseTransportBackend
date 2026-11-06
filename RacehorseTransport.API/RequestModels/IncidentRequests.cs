using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class IncidentQueryRequest : QueryOptionsRequest
    {
        [FromQuery(Name = "status")]
        public string? Status { get; set; }

        [FromQuery(Name = "severity")]
        public string? Severity { get; set; }

        [FromQuery(Name = "tripId")]
        public int? TripId { get; set; }
    }

    public class ReportIncidentRequest
    {
        [Required(ErrorMessage = "IncidentType is required.")]
        public string IncidentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Severity is required.")]
        public string Severity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [MaxLength(255, ErrorMessage = "Location cannot exceed 255 characters.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string Description { get; set; } = string.Empty;

        // Bỏ trống nếu sự cố không gắn với một con ngựa cụ thể
        public int? AffectedTripHorseId { get; set; }

        // true khi thú y yêu cầu dỡ con ngựa AffectedTripHorseId xuống ngay (R-12); phương án OffloadHorse vẫn phải duyệt sau
        public bool EmergencyOffload { get; set; }
    }

    public class ProposeIncidentPlanRequest
    {
        [Required(ErrorMessage = "ProposedAction is required.")]
        [MaxLength(255, ErrorMessage = "ProposedAction cannot exceed 255 characters.")]
        public string ProposedAction { get; set; } = string.Empty;

        // WaitAndContinue | RepairAndContinue | Reroute | VeterinaryCare | OffloadHorse | ReplaceVehicle | ReplaceCrew | Other
        [Required(ErrorMessage = "ResponseAction is required.")]
        public string ResponseAction { get; set; } = string.Empty;

        // ReplaceVehicle
        [Range(1, int.MaxValue, ErrorMessage = "ReplacementVehicleId must be a valid asset ID.")]
        public int? ReplacementVehicleId { get; set; }

        // ReplaceCrew
        [Range(1, int.MaxValue, ErrorMessage = "RemoveUserId must be a valid user ID.")]
        public int? RemoveUserId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "AddUserId must be a valid user ID.")]
        public int? AddUserId { get; set; }

        // Driver | Escort
        public string? CrewRole { get; set; }

        // Thay đổi tuyến đi kèm (bắt buộc với Reroute). Được kiểm tra ngay và áp vào lộ trình khi Manager duyệt.
        public ApplyRerouteRequest? RouteChange { get; set; }

        [MaxLength(2000, ErrorMessage = "RevisedRouteNotes cannot exceed 2000 characters.")]
        public string? RevisedRouteNotes { get; set; }

        [Range(0, 1000000000, ErrorMessage = "AdditionalCost must be between 0 and 1,000,000,000.")]
        public decimal AdditionalCost { get; set; }

        // R-14 (BE-14b): chỉ với Reroute. Tiền đi đường vòng khách phải trả thêm (tổng, chia theo số ngựa); > 0 thì phải hỏi ý khách
        [Range(0, 1000000000, ErrorMessage = "ExtraCostForCustomer must be between 0 and 1,000,000,000.")]
        public decimal ExtraCostForCustomer { get; set; }

        // Chỉ dùng với OffloadHorse (R-12). Bỏ trống AffectedTripHorseId = con ngựa đã chọn lúc báo sự cố.
        public int? AffectedTripHorseId { get; set; }

        // WaitRecover | ReturnToOrigin | HandOverHere | Deceased (bắt buộc với OffloadHorse)
        public string? OffloadOutcome { get; set; }

        // Đường dẫn biên bản thú y đã tải lên qua POST /api/files (category documents); bắt buộc khi Deceased
        [MaxLength(500, ErrorMessage = "VetReportPath cannot exceed 500 characters.")]
        public string? VetReportPath { get; set; }

        // Chỉ với OffloadHorse + Deceased khi ngựa còn sống và bác sĩ khuyến nghị an tử: duyệt xong chờ chủ ngựa đồng ý
        public bool RequiresEuthanasiaConsent { get; set; }

        // Bắt buộc với OffloadHorse + WaitRecover: phòng khám thú y nơi con ngựa ở lại
        public VetClinicRequest? VetClinic { get; set; }
    }

    public class VetClinicRequest
    {
        [Required(ErrorMessage = "VetClinic.Name is required.")]
        [MaxLength(150, ErrorMessage = "VetClinic.Name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "VetClinic.Address is required.")]
        [MaxLength(255, ErrorMessage = "VetClinic.Address cannot exceed 255 characters.")]
        public string Address { get; set; } = string.Empty;

        [MaxLength(30, ErrorMessage = "VetClinic.Phone cannot exceed 30 characters.")]
        public string? Phone { get; set; }

        [MaxLength(100, ErrorMessage = "VetClinic.AttendingVetName cannot exceed 100 characters.")]
        public string? AttendingVetName { get; set; }

        [Range(-90, 90, ErrorMessage = "VetClinic.Latitude must be between -90 and 90.")]
        public decimal? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "VetClinic.Longitude must be between -180 and 180.")]
        public decimal? Longitude { get; set; }
    }

    public class RecordVetExpenseRequest
    {
        [Required(ErrorMessage = "Amount is required.")]
        [Range(0, 1000000, ErrorMessage = "Amount must be between 0 and 1,000,000 USD.")]
        public decimal? Amount { get; set; }

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }

    // Đơn nối tiếp từ phòng khám tới đích cũ (cùng khách, người nhận, hạng chuồng, giá trị, gói bảo hiểm)
    public class CreateContinuationBookingRequest
    {
        // Nước của phòng khám (ISO 3166-1 alpha-2), ví dụ VN, CN
        [Required(ErrorMessage = "PickupCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "PickupCountryCode must be a 2-letter country code.")]
        public string PickupCountryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "PickupWindowStart is required.")]
        public DateTimeOffset? PickupWindowStart { get; set; }

        [Required(ErrorMessage = "PickupWindowEnd is required.")]
        public DateTimeOffset? PickupWindowEnd { get; set; }

        [Required(ErrorMessage = "DeliveryDeadline is required.")]
        public DateTimeOffset? DeliveryDeadline { get; set; }

        [MaxLength(1000, ErrorMessage = "SpecialInstructions cannot exceed 1000 characters.")]
        public string? SpecialInstructions { get; set; }
    }

    // multipart/form-data: ngựa đang điều trị tại phòng khám thì chết
    public class PostOffloadDeathRequest
    {
        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }

        // Giấy chứng tử / biên bản thú y: pdf, jpg, png; tối đa 10 MB
        [Required(ErrorMessage = "File is required.")]
        public IFormFile? File { get; set; }
    }

    // R-14 (BE-14b): khách đồng ý / từ chối đi đường vòng tốn thêm tiền
    public class IncidentConsentRequest
    {
        [Required(ErrorMessage = "Accept is required.")]
        public bool? Accept { get; set; }
    }

    // Quá hạn chờ chủ ngựa trả lời khuyến nghị an tử: áp dụng điều khoản ủy quyền khẩn cấp
    public class EmergencyAuthorizeRequest
    {
        [Required(ErrorMessage = "Note is required.")]
        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string Note { get; set; } = string.Empty;
    }

    // R-09: đóng sự cố phải chọn nguyên nhân
    public class ResolveIncidentRequest
    {
        // Company | Customer | ForceMajeure
        [Required(ErrorMessage = "FaultParty is required.")]
        public string FaultParty { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "FaultNote cannot exceed 500 characters.")]
        public string? FaultNote { get; set; }
    }

    public class RejectIncidentPlanRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ApplyRerouteRequest
    {
        // Chèn mốc mới ngay sau mốc này. Bỏ trống = sau mốc gần nhất mà đoàn xe đã tới.
        public int? InsertAfterCheckpointId { get; set; }

        // Các mốc chưa tới cần bỏ khỏi lộ trình
        public List<int> DeactivateCheckpointIds { get; set; } = new();

        // Các mốc mới theo đúng thứ tự đi (Position bị bỏ qua)
        public List<CheckpointRequest> NewCheckpoints { get; set; } = new();
    }

    // multipart/form-data: tài liệu của sự cố (ảnh hiện trường, biên bản thú y, giấy chứng tử, giấy đủ sức đi tiếp)
    public class UploadIncidentDocumentRequest
    {
        // ScenePhoto | VetReport | DeathCertificate | FitToTravel
        [Required(ErrorMessage = "DocumentType is required.")]
        public string DocumentType { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }

        // pdf, jpg, png; tối đa 10 MB
        [Required(ErrorMessage = "File is required.")]
        public IFormFile? File { get; set; }
    }
}
