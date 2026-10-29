namespace RacehorseTransport.API.ResponseModels
{
    public class QuoteLineResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class QuoteResponse
    {
        public List<QuoteLineResponse> Lines { get; set; } = new();
        public decimal Total { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public int HorseCount { get; set; }
        public int GroomCount { get; set; }
        public int TransitDays { get; set; }
        public bool IsExpress { get; set; }
        public int? DistanceKm { get; set; }
        public decimal? EstimatedHours { get; set; } // tổng thời gian vận chuyển dự kiến
        public string? RouteSummary { get; set; }

        // Lịch cho form tạo đơn: khóa các ngày pickup trước EarliestPickupDate;
        // pickup trước ExpressUntilDate thì có phụ phí Express; hạn giao phải từ EarliestDeliveryDeadline trở đi.
        public int LeadTimeDays { get; set; }
        public int ClearanceDays { get; set; } // phần làm hồ sơ của tuyến trong LeadTimeDays
        public List<string> TransitCountryCodes { get; set; } = new();
        public DateOnly EarliestPickupDate { get; set; }
        public DateOnly ExpressUntilDate { get; set; }
        public DateTimeOffset EarliestDeliveryDeadline { get; set; }
        public int DeliveryBufferDays { get; set; }          // số ngày dự phòng đã cộng vào hạn giao sớm nhất
        public DateOnly EarliestDeliveryDate { get; set; }   // hạn giao sớm nhất khách được chọn (theo ngày)
        public TransitEstimateResponse Transit { get; set; } = new();

        // R-10: khoảng giá ước tính (giá chốt gửi sau khi công ty lập tuyến); Total = QuoteMin
        public decimal QuoteMin { get; set; }
        public decimal QuoteMax { get; set; }
        public bool RouteIsEstimate { get; set; } // quãng đường ước tính (bản đồ lỗi)
    }

    // Thời gian vận chuyển ước tính, tách theo thành phần (giờ)
    public class TransitEstimateResponse
    {
        public decimal DrivingHours { get; set; }
        public decimal RestHours { get; set; }
        public int OvernightStops { get; set; }
        public decimal BorderHours { get; set; }
        public decimal HandlingHours { get; set; }
        public decimal FlightHours { get; set; }
        public decimal TotalHours { get; set; }
        public decimal MaxHours { get; set; }
        public int TransitDays { get; set; }
    }

    public class DistanceEstimateResponse
    {
        public int DistanceKm { get; set; }
        public decimal EstimatedHours { get; set; }
        public string RouteSummary { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public class BookingSummaryResponse
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
        public List<string> TransitCountryCodes { get; set; } = new(); // nước quá cảnh theo thứ tự
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public string TransportMode { get; set; } = string.Empty;
        public int TotalHorses { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal? QuoteMin { get; set; }   // R-10: khoảng giá ước tính lúc tạo đơn (null với đơn cũ)
        public decimal? QuoteMax { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CustomerStage { get; set; } = string.Empty; // giai đoạn theo góc nhìn khách (R-07)
        public int? AssignedSpecialistId { get; set; }
        public string? AssignedSpecialistName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class BookingDetailResponse : BookingSummaryResponse
    {
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
        // R-10 (BE-06): hạn nộp giấy; công ty đổi tuyến thêm nước mới -> khách được hủy hoàn 100% tới routeChangeCancelUntil
        public DateTimeOffset? DocsDueAt { get; set; }
        public DateTimeOffset? RouteChangeCancelUntil { get; set; }
        // R-10: báo giá chốt (estimatedCost + quoteLines) gửi lúc quotedAt; khách chấp nhận + cọc trước quoteExpiresAt
        public DateTimeOffset? QuotedAt { get; set; }
        public DateTimeOffset? QuoteExpiresAt { get; set; }
        public DateTimeOffset? QuoteAcceptedAt { get; set; }
        public string? PolicyVersion { get; set; }
        public DateTimeOffset? PolicyAcceptedAt { get; set; }

        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? SpecialInstructions { get; set; }
        public int? DistanceKm { get; set; }
        public decimal? EstimatedTransitHours { get; set; }
        public bool IsExpress { get; set; }
        public bool RequiresClimateControl { get; set; }
        public decimal? DeclaredValue { get; set; }
        public string? RejectionReason { get; set; }
        public int? ReviewedByUserId { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public List<QuoteLineResponse> QuoteLines { get; set; } = new();
        public List<BookingHorseResponse> Horses { get; set; } = new();

        // Tình hình thanh toán (R-02)
        public BookingPaymentSummaryResponse Payment { get; set; } = new();
    }

    public class BookingHorseResponse
    {
        public int BookingHorseId { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string StallClass { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal? DeclaredValue { get; set; }   // giá trị khách khai báo (USD)
        public decimal? AgreedValue { get; set; }     // giá trị thỏa thuận Manager chốt; bảo hiểm / bồi thường tính trên giá trị này
        public string? ValueProofUrl { get; set; }    // link xem chứng từ giá trị (có hạn)
        public string InsurancePlan { get; set; } = string.Empty; // None | Full
        public int? DossierId { get; set; }
        public string? DossierCode { get; set; }
        public string? DossierStatus { get; set; }
        public List<BookingHorseTripResponse> Trips { get; set; } = new();
    }

    public class BookingHorseTripResponse
    {
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string TripStatus { get; set; } = string.Empty;
        public string HorseStatus { get; set; } = string.Empty;
        public int StallSlotNumber { get; set; }
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
        public DateTimeOffset? PickedUpAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
    }
}
