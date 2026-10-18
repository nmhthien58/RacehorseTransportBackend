namespace RacehorseTransport.API.ResponseModels
{
    public class EnumLookupsResponse
    {
        public string[] UserRoles { get; set; } = Array.Empty<string>();
        public string[] HorseGenders { get; set; } = Array.Empty<string>();
        public string[] BookingStatuses { get; set; } = Array.Empty<string>();
        public string[] BookingHorseStatuses { get; set; } = Array.Empty<string>();
        public string[] TransportModes { get; set; } = Array.Empty<string>();
        public string[] StallClasses { get; set; } = Array.Empty<string>();
        public string[] DossierStatuses { get; set; } = Array.Empty<string>();
        public string[] DocumentStatuses { get; set; } = Array.Empty<string>();
        public string[] RequirementDirections { get; set; } = Array.Empty<string>();
        public string[] AssetTypes { get; set; } = Array.Empty<string>();
        public string[] AssetStatuses { get; set; } = Array.Empty<string>();
        public string[] TripStatuses { get; set; } = Array.Empty<string>();
        public string[] OnTimeStatuses { get; set; } = Array.Empty<string>();
        public string[] CrewRoles { get; set; } = Array.Empty<string>();
        public string[] CheckpointTypes { get; set; } = Array.Empty<string>();
        public string[] CheckpointStatuses { get; set; } = Array.Empty<string>();
        public string[] TripHorseStatuses { get; set; } = Array.Empty<string>();
        public string[] FeedStatuses { get; set; } = Array.Empty<string>();
        public string[] StressLevels { get; set; } = Array.Empty<string>();
        public string[] IncidentTypes { get; set; } = Array.Empty<string>();
        public string[] IncidentSeverities { get; set; } = Array.Empty<string>();
        public string[] IncidentStatuses { get; set; } = Array.Empty<string>();
        public string[] HandoverConditions { get; set; } = Array.Empty<string>();
        public string[] PriceCategories { get; set; } = Array.Empty<string>();
        public string[] PriceCalcTypes { get; set; } = Array.Empty<string>();
        public string[] PaymentTypes { get; set; } = Array.Empty<string>();
        public string[] PaymentStatuses { get; set; } = Array.Empty<string>();
        public string[] PaymentProviders { get; set; } = Array.Empty<string>();
    }
}
