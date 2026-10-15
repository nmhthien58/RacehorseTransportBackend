namespace RacehorseTransport.Services.BusinessModels
{
    public class BookingFilterOptions : BusinessQueryOptions
    {
        public string? Status { get; set; }
        public int? CustomerUserId { get; set; } // chỉ nhân sự dùng được
        public DateTimeOffset? DepartureFrom { get; set; }
        public DateTimeOffset? DepartureTo { get; set; }
    }

    public class CreateBookingModel
    {
        public string PickupAddress { get; set; } = string.Empty;
        public string? PickupCity { get; set; }
        public string PickupCountryCode { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public string? DropoffCity { get; set; }
        public string DropoffCountryCode { get; set; } = string.Empty;
        public DateTimeOffset DepartureDate { get; set; }
        public DateTimeOffset DeliveryDate { get; set; }
        public string TransportMode { get; set; } = string.Empty;
        public int? DistanceKm { get; set; }
        public bool RequiresClimateControl { get; set; }
        public decimal? DeclaredValue { get; set; }
        public string? SpecialInstructions { get; set; }
        public List<CreateBookingHorseModel> Horses { get; set; } = new();
    }

    public class CreateBookingHorseModel
    {
        public int HorseId { get; set; }
        public string StallClass { get; set; } = string.Empty;
        public string? Notes { get; set; }
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
        public string DropoffAddress { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public DateTimeOffset DepartureDate { get; set; }
        public DateTimeOffset DeliveryDate { get; set; }
        public string TransportMode { get; set; } = string.Empty;
        public int TotalHorses { get; set; }
        public decimal EstimatedCost { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
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
        public bool IsExpress { get; set; }
        public bool RequiresClimateControl { get; set; }
        public decimal? DeclaredValue { get; set; }
        public string? RejectionReason { get; set; }
        public int? ReviewedByUserId { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public List<QuoteLineModel> QuoteLines { get; set; } = new();
        public List<BookingHorseModel> Horses { get; set; } = new();
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
        public int StallSlotNumber { get; set; }
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
    }
}
