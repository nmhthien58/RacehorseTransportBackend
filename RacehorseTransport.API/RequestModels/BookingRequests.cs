using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class BookingQueryRequest : QueryOptionsRequest
    {
        [FromQuery(Name = "status")]
        public string? Status { get; set; }

        // Giai đoạn theo góc nhìn khách (R-07): PendingReview, AwaitingDeposit, PreparingDossier, AwaitingTrip,
        // Scheduled, AwaitingPayment, InTransit, AwaitingHandover, Completed, Closed
        [FromQuery(Name = "stage")]
        public string? Stage { get; set; }

        // Chỉ có tác dụng với nhân sự
        [FromQuery(Name = "customerUserId")]
        public int? CustomerUserId { get; set; }

        // Lọc theo ngày bắt đầu khoảng pickup
        [FromQuery(Name = "pickupFrom")]
        public DateTimeOffset? PickupFrom { get; set; }

        [FromQuery(Name = "pickupTo")]
        public DateTimeOffset? PickupTo { get; set; }
    }

    public class CreateBookingRequest
    {
        [Required(ErrorMessage = "PickupAddress is required.")]
        [MaxLength(255, ErrorMessage = "PickupAddress cannot exceed 255 characters.")]
        public string PickupAddress { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "PickupCity cannot exceed 100 characters.")]
        public string? PickupCity { get; set; }

        [Required(ErrorMessage = "PickupCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "PickupCountryCode must be a 2-letter country code.")]
        public string PickupCountryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "DropoffAddress is required.")]
        [MaxLength(255, ErrorMessage = "DropoffAddress cannot exceed 255 characters.")]
        public string DropoffAddress { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "DropoffCity cannot exceed 100 characters.")]
        public string? DropoffCity { get; set; }

        [Required(ErrorMessage = "DropoffCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DropoffCountryCode must be a 2-letter country code.")]
        public string DropoffCountryCode { get; set; } = string.Empty;

        // Khoảng ngày ngựa sẵn sàng được đón (không phải ngày khởi hành) và hạn giao trễ nhất
        [Required(ErrorMessage = "PickupWindowStart is required.")]
        public DateTimeOffset? PickupWindowStart { get; set; }

        [Required(ErrorMessage = "PickupWindowEnd is required.")]
        public DateTimeOffset? PickupWindowEnd { get; set; }

        [Required(ErrorMessage = "DeliveryDeadline is required.")]
        public DateTimeOffset? DeliveryDeadline { get; set; }

        [Required(ErrorMessage = "TransportMode is required.")]
        public string TransportMode { get; set; } = string.Empty;

        [Range(1, 50000, ErrorMessage = "DistanceKm must be between 1 and 50000.")]
        public int? DistanceKm { get; set; }

        public bool RequiresClimateControl { get; set; }

        [Range(0, 1000000000, ErrorMessage = "DeclaredValue must be between 0 and 1,000,000,000.")]
        public decimal? DeclaredValue { get; set; }

        [MaxLength(2000, ErrorMessage = "SpecialInstructions cannot exceed 2000 characters.")]
        public string? SpecialInstructions { get; set; }

        // R-16 (BE-15): người nhận ngựa: tên + SĐT bắt buộc, hoặc RecipientIsCustomer = true (khách tự nhận)
        public bool RecipientIsCustomer { get; set; }

        [MaxLength(100, ErrorMessage = "RecipientName cannot exceed 100 characters.")]
        public string? RecipientName { get; set; }

        [MaxLength(20, ErrorMessage = "RecipientPhone cannot exceed 20 characters.")]
        [RegularExpression(@"^\+?[0-9 .()-]{6,20}$", ErrorMessage = "RecipientPhone is not a valid phone number.")]
        public string? RecipientPhone { get; set; }

        [EmailAddress(ErrorMessage = "RecipientEmail is not a valid email address.")]
        [MaxLength(255, ErrorMessage = "RecipientEmail cannot exceed 255 characters.")]
        public string? RecipientEmail { get; set; }

        [MaxLength(50, ErrorMessage = "RecipientIdNumber cannot exceed 50 characters.")]
        public string? RecipientIdNumber { get; set; }

        // R-10: bắt buộc tích "Tôi đồng ý với Chính sách dịch vụ" và gửi phiên bản đang hiển thị (GET /api/policy)
        public bool AcceptPolicy { get; set; }

        [MaxLength(20, ErrorMessage = "PolicyVersion cannot exceed 20 characters.")]
        public string? PolicyVersion { get; set; }

        // R-10 / R-11: tọa độ điểm đón / trả chọn trên bản đồ (GET /api/geo/search, /reverse).
        // Gửi đủ 4 số thì quãng đường và các nước đi qua lấy từ bản đồ; bỏ trống thì tính như trước.
        [Range(-90, 90, ErrorMessage = "PickupLat must be between -90 and 90.")]
        public decimal? PickupLat { get; set; }

        [Range(-180, 180, ErrorMessage = "PickupLng must be between -180 and 180.")]
        public decimal? PickupLng { get; set; }

        [Range(-90, 90, ErrorMessage = "DropoffLat must be between -90 and 90.")]
        public decimal? DropoffLat { get; set; }

        [Range(-180, 180, ErrorMessage = "DropoffLng must be between -180 and 180.")]
        public decimal? DropoffLng { get; set; }

        [Required(ErrorMessage = "Horses is required.")]
        [MinLength(1, ErrorMessage = "At least one horse is required.")]
        public List<CreateBookingHorseRequest> Horses { get; set; } = new();
    }

    public class CreateBookingHorseRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "HorseId is required.")]
        public int HorseId { get; set; }

        [Required(ErrorMessage = "StallClass is required.")]
        public string StallClass { get; set; } = string.Empty;

        [MaxLength(300, ErrorMessage = "Notes cannot exceed 300 characters.")]
        public string? Notes { get; set; }

        // R-12 / R-13: giá trị khai báo của con ngựa (USD). Khai cho một con thì phải khai cho mọi con.
        [Range(0, 1000000000, ErrorMessage = "DeclaredValue must be between 0 and 1,000,000,000.")]
        public decimal? DeclaredValue { get; set; }

        // Chứng từ giá trị (bắt buộc từ ngưỡng Insurance.ProofThresholdUsd): đường dẫn do POST /api/files (category documents) trả về
        [MaxLength(500, ErrorMessage = "ValueProofPath cannot exceed 500 characters.")]
        public string? ValueProofPath { get; set; }

        // None (mặc định) | Full
        public string? InsurancePlan { get; set; }
    }

    // R-16 (BE-15): khách sửa người nhận
    public class UpdateRecipientRequest
    {
        public bool RecipientIsCustomer { get; set; }

        [MaxLength(100, ErrorMessage = "RecipientName cannot exceed 100 characters.")]
        public string? RecipientName { get; set; }

        [MaxLength(20, ErrorMessage = "RecipientPhone cannot exceed 20 characters.")]
        [RegularExpression(@"^\+?[0-9 .()-]{6,20}$", ErrorMessage = "RecipientPhone is not a valid phone number.")]
        public string? RecipientPhone { get; set; }

        [EmailAddress(ErrorMessage = "RecipientEmail is not a valid email address.")]
        [MaxLength(255, ErrorMessage = "RecipientEmail cannot exceed 255 characters.")]
        public string? RecipientEmail { get; set; }

        [MaxLength(50, ErrorMessage = "RecipientIdNumber cannot exceed 50 characters.")]
        public string? RecipientIdNumber { get; set; }
    }

    // R-16 (BE-15): khách xác nhận người khác nhận thay (tên phải khớp người ký e-POD)
    public class ApproveAlternateRecipientRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "IdNumber cannot exceed 50 characters.")]
        public string? IdNumber { get; set; }
    }

    // R-10 (BE-06): khách đổi điểm đón / trả (chưa trả tiền) -> lập tuyến + báo giá lại
    public class ChangeLocationsRequest
    {
        [Required(ErrorMessage = "PickupAddress is required.")]
        [MaxLength(255, ErrorMessage = "PickupAddress cannot exceed 255 characters.")]
        public string PickupAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "PickupCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "PickupCountryCode must be a 2-letter country code.")]
        public string PickupCountryCode { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "PickupLat must be between -90 and 90.")]
        public decimal? PickupLat { get; set; }

        [Range(-180, 180, ErrorMessage = "PickupLng must be between -180 and 180.")]
        public decimal? PickupLng { get; set; }

        [Required(ErrorMessage = "DropoffAddress is required.")]
        [MaxLength(255, ErrorMessage = "DropoffAddress cannot exceed 255 characters.")]
        public string DropoffAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "DropoffCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DropoffCountryCode must be a 2-letter country code.")]
        public string DropoffCountryCode { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "DropoffLat must be between -90 and 90.")]
        public decimal? DropoffLat { get; set; }

        [Range(-180, 180, ErrorMessage = "DropoffLng must be between -180 and 180.")]
        public decimal? DropoffLng { get; set; }
    }

    // R-10: khách chấp nhận báo giá chốt (đồng ý Chính sách dịch vụ đúng phiên bản hiện hành)
    public class AcceptQuoteRequest
    {
        // R-17: bản hợp đồng khách đang xem (GET /api/bookings/{id}/contract) và mã băm của nó
        [Range(1, int.MaxValue, ErrorMessage = "ContractId is required.")]
        public int ContractId { get; set; }

        [Required(ErrorMessage = "ContractSha256 is required.")]
        [MaxLength(64, ErrorMessage = "ContractSha256 cannot exceed 64 characters.")]
        public string ContractSha256 { get; set; } = string.Empty;

        public bool AcceptPolicy { get; set; }

        [Required(ErrorMessage = "PolicyVersion is required.")]
        [MaxLength(20, ErrorMessage = "PolicyVersion cannot exceed 20 characters.")]
        public string PolicyVersion { get; set; } = string.Empty;
    }

    public class DeclineQuoteRequest
    {
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }
    }

    // BE-09: Manager chốt giá trị thỏa thuận từng con trước khi gửi báo giá
    public class UpdateHorseValuesRequest
    {
        [Required(ErrorMessage = "Horses is required.")]
        [MinLength(1, ErrorMessage = "Horses must have at least one horse.")]
        public List<HorseAgreedValueRequest> Horses { get; set; } = new();

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }

    public class HorseAgreedValueRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "BookingHorseId is required.")]
        public int BookingHorseId { get; set; }

        [Range(0, 1000000000, ErrorMessage = "AgreedValue must be between 0 and 1,000,000,000.")]
        public decimal AgreedValue { get; set; }
    }

    // Duyệt đơn và đổi người phụ trách đều cần chọn một Transport Specialist
    public class AssignSpecialistRequest
    {
        [Required(ErrorMessage = "SpecialistUserId is required.")]
        public int? SpecialistUserId { get; set; }
    }

    // Danh sách nước quá cảnh mới (thay toàn bộ, theo thứ tự đi qua); mảng rỗng = đi thẳng
    public class UpdateTransitCountriesRequest
    {
        [Required(ErrorMessage = "TransitCountryCodes is required.")]
        public List<string> TransitCountryCodes { get; set; } = new();
    }

    public class RejectBookingRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    // Xem giá trước khi gửi đơn. Không ghi gì vào DB.
    public class QuotePreviewRequest
    {
        [Required(ErrorMessage = "TransportMode is required.")]
        public string TransportMode { get; set; } = string.Empty;

        public string? PickupCountryCode { get; set; }
        public string? PickupCity { get; set; }
        public string? PickupAddress { get; set; }

        [Required(ErrorMessage = "DropoffCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DropoffCountryCode must be a 2-letter country code.")]
        public string DropoffCountryCode { get; set; } = string.Empty;

        public string? DropoffCity { get; set; }
        public string? DropoffAddress { get; set; }

        [Range(1, 50000, ErrorMessage = "DistanceKm must be between 1 and 50000.")]
        public int? DistanceKm { get; set; }

        public bool RequiresClimateControl { get; set; }

        [Range(0, 1000000000, ErrorMessage = "DeclaredValue must be between 0 and 1,000,000,000.")]
        public decimal? DeclaredValue { get; set; }

        // Có thể để trống khi khách chưa chọn ngày: kết quả vẫn có ngày pickup sớm nhất và thời gian vận chuyển
        public DateTimeOffset? PickupWindowStart { get; set; }
        public DateTimeOffset? PickupWindowEnd { get; set; }
        public DateTimeOffset? DeliveryDeadline { get; set; }

        // R-10 / R-11: tọa độ điểm đón / trả chọn trên bản đồ (GET /api/geo/search, /reverse).
        // Gửi đủ 4 số thì quãng đường và các nước đi qua lấy từ bản đồ; bỏ trống thì tính như trước.
        [Range(-90, 90, ErrorMessage = "PickupLat must be between -90 and 90.")]
        public decimal? PickupLat { get; set; }

        [Range(-180, 180, ErrorMessage = "PickupLng must be between -180 and 180.")]
        public decimal? PickupLng { get; set; }

        [Range(-90, 90, ErrorMessage = "DropoffLat must be between -90 and 90.")]
        public decimal? DropoffLat { get; set; }

        [Range(-180, 180, ErrorMessage = "DropoffLng must be between -180 and 180.")]
        public decimal? DropoffLng { get; set; }

        [Required(ErrorMessage = "Horses is required.")]
        [MinLength(1, ErrorMessage = "At least one horse is required.")]
        public List<QuotePreviewHorseRequest> Horses { get; set; } = new();
    }

    public class QuotePreviewHorseRequest
    {
        [Required(ErrorMessage = "StallClass is required.")]
        public string StallClass { get; set; } = string.Empty;
    }

    public class EstimateDistanceRequest
    {
        public string PickupCountryCode { get; set; } = "VN";
        public string? PickupCity { get; set; }
        public string? PickupAddress { get; set; }

        [Required(ErrorMessage = "DropoffCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DropoffCountryCode must be a 2-letter country code.")]
        public string DropoffCountryCode { get; set; } = string.Empty;

        public string? DropoffCity { get; set; }
        public string? DropoffAddress { get; set; }
    }
}
