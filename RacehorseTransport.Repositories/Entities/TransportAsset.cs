using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // fleet.transport_assets
    public class TransportAsset
    {
        public int AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetType { get; set; } = AssetTypes.HorseTruckAirSuspension;
        public int CapacityHorses { get; set; }
        public bool HasClimateControl { get; set; } = true;
        public bool HasGpsTracker { get; set; } = true;
        public string Status { get; set; } = AssetStatuses.Available;
        public DateTimeOffset? LastSanitizationDate { get; set; }
    }
}
