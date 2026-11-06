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
        public int CompartmentCount { get; set; } = 1; // số khoang, mỗi khoang VehicleCompartments.PositionsPerCompartment vị trí
        public bool HasClimateControl { get; set; } = true;
        public bool HasGpsTracker { get; set; } = true;
        public string Status { get; set; } = AssetStatuses.Available;
        public DateTimeOffset? LastSanitizationDate { get; set; }
    }
}
