namespace RacehorseTransport.Services.BusinessModels
{
    public class BookingFilterOptions : BusinessQueryOptions
    {
        public string? Status { get; set; }
        public string? Stage { get; set; }       // giai đoạn theo góc nhìn khách (R-07)
        public int? CustomerUserId { get; set; } // chỉ nhân sự dùng được
        // Lọc theo ngày bắt đầu khoảng pickup
        public DateTimeOffset? PickupFrom { get; set; }
        public DateTimeOffset? PickupTo { get; set; }
    }

    public class CreateBookingModel
    {
        public string PickupAddress { get; set; } = string.Empty;
        public string? PickupCity { get; set; }
        public string PickupCountryCode { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public string? DropoffCity { get; set; }
        public string DropoffCountryCode { get; set; } = string.Empty;

        // Khoảng ngày ngựa sẵn sàng được đón + hạn giao trễ nhất (ngày chạy thật do Coordinator chốt)
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public string TransportMode { get; set; } = string.Empty;
        public int? DistanceKm { get; set; }
        public bool RequiresClimateControl { get; set; }
        public decimal? DeclaredValue { get; set; }
        public string? SpecialInstructions { get; set; }

        // R-16 (BE-15): người nhận ngựa (bắt buộc tên + SĐT, hoặc RecipientIsCustomer = true)
        public RecipientInputModel Recipient { get; set; } = new();

        // R-10: khách phải đồng ý Chính sách dịch vụ đúng phiên bản hiện hành khi gửi đơn
        public bool AcceptPolicy { get; set; }
        public string? PolicyVersion { get; set; }

        // R-10 / R-11: tọa độ điểm đón / trả (đủ 4 số thì quãng đường + nước đi qua lấy từ bản đồ)
        public decimal? PickupLat { get; set; }
        public decimal? PickupLng { get; set; }
        public decimal? DropoffLat { get; set; }
        public decimal? DropoffLng { get; set; }
        public List<CreateBookingHorseModel> Horses { get; set; } = new();
    }

    public class CreateBookingHorseModel
    {
        public int HorseId { get; set; }
        public string StallClass { get; set; } = string.Empty;
        public string? Notes { get; set; }

        // R-12 / R-13: giá trị khai báo (USD), chứng từ (bắt buộc từ ngưỡng Insurance.ProofThresholdUsd), gói bảo hiểm
        public decimal? DeclaredValue { get; set; }
        public string? ValueProofPath { get; set; }
        public string? InsurancePlan { get; set; }
    }

    // Manager chốt giá trị thỏa thuận từng con trước khi gửi báo giá
    public class UpdateHorseValuesModel
    {
        public List<HorseAgreedValueModel> Horses { get; set; } = new();
        public string? Note { get; set; }
    }

    public class HorseAgreedValueModel
    {
        public int BookingHorseId { get; set; }
        public decimal AgreedValue { get; set; }
    }

    // Dòng trong danh sách đơn
    public class BookingSummaryModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int CustomerUserId { get; set; }
        public string? CustomerName { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string PickupCountryCode { get; set; } = string.Empty;
        // Tọa độ điểm đón / giao để vẽ bản đồ (null với đơn cũ)
        public decimal? PickupLat { get; set; }
        public decimal? PickupLng { get; set; }
        public decimal? DropoffLat { get; set; }
        public decimal? DropoffLng { get; set; }
        public string DropoffAddress { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public List<string> TransitCountryCodes { get; set; } = new(); // theo thứ tự đi qua
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public string TransportMode { get; set; } = string.Empty;
        public int TotalHorses { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal? QuoteMin { get; set; }
        public decimal? QuoteMax { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        // Đơn nối tiếp từ phòng khám (ngựa ở lại điều trị ở chuyến trước); IsFreeOfCharge = miễn cước vì lỗi công ty
        public int? ParentBookingId { get; set; }
        public string? ParentBookingCode { get; set; }
        public bool IsFreeOfCharge { get; set; }
        public string CustomerStage { get; set; } = string.Empty; // xem CustomerStages (R-07)
        public int? AssignedSpecialistId { get; set; }
        public string? AssignedSpecialistName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    // Chi tiết một đơn: thêm thông tin báo giá, xét duyệt và từng con ngựa
    public class BookingDetailModel : BookingSummaryModel
    {
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? SpecialInstructions { get; set; }
        public int? DistanceKm { get; set; }
        public decimal? EstimatedTransitHours { get; set; }
        public bool IsExpress { get; set; }
        public bool RequiresClimateControl { get; set; }
        public decimal? DeclaredValue { get; set; }
        public string? RejectionReason { get; set; }
        // R-16 (BE-15): người nhận ngựa
        public string? RecipientName { get; set; }
        public string? RecipientPhone { get; set; }
        public string? RecipientEmail { get; set; }
        public string? RecipientIdNumber { get; set; }
        public bool RecipientIsCustomer { get; set; }
        public DateTimeOffset? RecipientConfirmedAt { get; set; }
        public DateTimeOffset? RecipientNotifiedWindowStart { get; set; }
        public DateTimeOffset? RecipientNotifiedWindowEnd { get; set; }
        public string? AlternateRecipientName { get; set; }
        public DateTimeOffset? AlternateRecipientApprovedAt { get; set; }
        // R-10 (BE-06): hạn nộp giấy; công ty đổi tuyến thêm nước mới thì khách được hủy hoàn 100% tới RouteChangeCancelUntil
        public DateTimeOffset? DocsDueAt { get; set; }
        public DateTimeOffset? RouteChangeCancelUntil { get; set; }
        // R-10: báo giá chốt, hạn chấp nhận, chính sách khách đã đồng ý
        public DateTimeOffset? QuotedAt { get; set; }
        public DateTimeOffset? QuoteExpiresAt { get; set; }
        public DateTimeOffset? QuoteAcceptedAt { get; set; }
        public string? PolicyVersion { get; set; }
        public DateTimeOffset? PolicyAcceptedAt { get; set; }
        public int? ReviewedByUserId { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public List<QuoteLineModel> QuoteLines { get; set; } = new();
        public List<BookingHorseModel> Horses { get; set; } = new();

        // Tình hình thanh toán (R-02)
        public BookingPaymentSummaryModel Payment { get; set; } = new();
    }

    public class BookingHorseModel
    {
        public int BookingHorseId { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string StallClass { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }

        // R-12 / R-13: giá trị khai báo, giá trị thỏa thuận (bảo hiểm / bồi thường tính trên giá trị này), chứng từ, gói bảo hiểm
        public decimal? DeclaredValue { get; set; }
        public decimal? AgreedValue { get; set; }
        public string? ValueProofUrl { get; set; }       // link xem chứng từ giá trị (có hạn)
        public string InsurancePlan { get; set; } = string.Empty;

        // Hồ sơ kiểm dịch của con ngựa này (null khi đơn chưa được duyệt)
        public int? DossierId { get; set; }
        public string? DossierCode { get; set; }
        public string? DossierStatus { get; set; }

        // Các chuyến con ngựa này được xếp lên (một đơn có thể đi nhiều chuyến)
        public List<BookingHorseTripModel> Trips { get; set; } = new();
    }

    public class BookingHorseTripModel
    {
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string TripStatus { get; set; } = string.Empty;
        public string HorseStatus { get; set; } = string.Empty; // trạng thái của con ngựa trên chuyến (R-05)
        public int StallSlotNumber { get; set; }
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
        public DateTimeOffset? PickedUpAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
    }

    // R-10 (BE-06): khách đổi điểm đón / trả
    public class ChangeLocationsModel
    {
        public string PickupAddress { get; set; } = string.Empty;
        public string PickupCountryCode { get; set; } = string.Empty;
        public decimal? PickupLat { get; set; }
        public decimal? PickupLng { get; set; }
        public string DropoffAddress { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public decimal? DropoffLat { get; set; }
        public decimal? DropoffLng { get; set; }
    }

    // R-16 (BE-15): người nhận ngựa khách nhập
    public class RecipientInputModel
    {
        public bool RecipientIsCustomer { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientPhone { get; set; }
        public string? RecipientEmail { get; set; }
        public string? RecipientIdNumber { get; set; }
    }

    // R-16 (BE-15): người nhận của từng đơn trên chuyến (đoàn xe: e-POD điền sẵn, gọi điện)
    public class TripRecipientModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int? DropoffCheckpointId { get; set; }
        public string? DropoffCheckpointName { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientPhone { get; set; }
        public string? RecipientEmail { get; set; }
        public string? RecipientIdNumber { get; set; }
        public bool RecipientIsCustomer { get; set; }
        public DateTimeOffset? RecipientConfirmedAt { get; set; }
        public string? AlternateRecipientName { get; set; }
        public string? AlternateRecipientIdNumber { get; set; }
        public DateTimeOffset? AlternateRecipientApprovedAt { get; set; }
        public DateTimeOffset? NotifiedWindowStart { get; set; }
        public DateTimeOffset? NotifiedWindowEnd { get; set; }
    }

    // Tạo đơn nối tiếp cho con ngựa ở lại phòng khám
    public class CreateContinuationBookingModel
    {
        public string PickupCountryCode { get; set; } = string.Empty;
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public string? SpecialInstructions { get; set; }
    }
}
