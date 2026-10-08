namespace RacehorseTransport.API.ResponseModels
{
    public class UnplannedHorseResponse
    {
        public int BookingHorseId { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string TransportMode { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string PickupCountryCode { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public DateTimeOffset DepartureDate { get; set; }
        public DateTimeOffset DeliveryDate { get; set; }
        public bool RequiresClimateControl { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string? SpecialCareRequirements { get; set; }
        public string StallClass { get; set; } = string.Empty;
        public string? DossierStatus { get; set; }
    }

    public class AvailableVehicleResponse
    {
        public int AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public int CapacityHorses { get; set; }
        public bool HasClimateControl { get; set; }
        public bool HasGpsTracker { get; set; }
        public DateTimeOffset? LastSanitizationDate { get; set; }
    }
}
