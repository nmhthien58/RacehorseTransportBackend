namespace RacehorseTransport.Services.BusinessModels
{
    public class DistanceEstimateInputModel
    {
        public string PickupCountryCode { get; set; } = "VN";
        public string? PickupCity { get; set; }
        public string? PickupAddress { get; set; }

        public string DropoffCountryCode { get; set; } = string.Empty;
        public string? DropoffCity { get; set; }
        public string? DropoffAddress { get; set; }
    }

    public class DistanceEstimateResultModel
    {
        public int DistanceKm { get; set; }
        public decimal EstimatedHours { get; set; }
        public string RouteSummary { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // "GoongApi", "OsrmApi", or "RouteMatrix"
    }
}
