using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // booking.booking_requests
    public class BookingRequest
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int CustomerUserId { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string PickupCountryCode { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        // Khoảng ngày ngựa sẵn sàng được đón và hạn giao trễ nhất; ngày chạy thật nằm ở chuyến
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public int TotalHorses { get; set; }
        public string? SpecialInstructions { get; set; }
        public decimal EstimatedCost { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public string Status { get; set; } = BookingStatuses.Submitted;
        public string? RejectionReason { get; set; }
        public int? ReviewedByUserId { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public int? AssignedSpecialistId { get; set; }
        public int? AssignedCoordinatorId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Đầu vào của công thức tính giá (xem docs/TASKS.md, T-05)
        public string TransportMode { get; set; } = TransportModes.Ground;
        public int? DistanceKm { get; set; }
        public bool IsExpress { get; set; }
        public bool RequiresClimateControl { get; set; }
        public decimal? DeclaredValue { get; set; }
        public string? QuoteBreakdown { get; set; } // JSON các dòng báo giá
        public decimal? EstimatedTransitHours { get; set; } // ước tính lúc báo giá

        // Thanh toán (R-02). DepositPercent = null: đơn tạo trước khi có thanh toán, không bắt trả tiền.
        public decimal? DepositPercent { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal ExtraCharges { get; set; }      // chi phí phát sinh chốt khi đóng chuyến
        public DateTimeOffset? ExtraDueAt { get; set; }
        public int PaymentReminderCount { get; set; }

        // Navigation properties
        public virtual User? Customer { get; set; }
        public virtual User? ReviewedBy { get; set; }
        public virtual User? AssignedSpecialist { get; set; }
        public virtual User? AssignedCoordinator { get; set; }
        public virtual ICollection<BookingHorse> BookingHorses { get; set; } = new List<BookingHorse>();
        public virtual ICollection<BookingTransitCountry> TransitCountries { get; set; } = new List<BookingTransitCountry>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
