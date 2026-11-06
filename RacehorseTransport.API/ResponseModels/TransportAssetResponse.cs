namespace RacehorseTransport.API.ResponseModels
{
    public class TransportAssetResponse
    {
        public int AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public int CapacityHorses { get; set; }
        public int CompartmentCount { get; set; }
        public int PositionsPerCompartment { get; set; } // luôn 3
        public bool HasClimateControl { get; set; }
        public bool HasGpsTracker { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset? LastSanitizationDate { get; set; }
    }
}
