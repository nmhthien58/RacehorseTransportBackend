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
    }

    public class BookingSummaryResponse
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

    public class BookingDetailResponse : BookingSummaryResponse
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
        public List<QuoteLineResponse> QuoteLines { get; set; } = new();
        public List<BookingHorseResponse> Horses { get; set; } = new();
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
        public int StallSlotNumber { get; set; }
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
    }
}
