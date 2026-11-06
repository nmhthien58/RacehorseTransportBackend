namespace RacehorseTransport.Services.BusinessModels
{
    public class TransportAssetModel
    {
        public int AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public int CapacityHorses { get; set; }
        public int CompartmentCount { get; set; } // 0 khi tạo / sửa = tự tính ceil(CapacityHorses / 3)
        public bool HasClimateControl { get; set; }
        public bool HasGpsTracker { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset? LastSanitizationDate { get; set; }
    }

    public class TransportAssetQueryOptions : BusinessQueryOptions
    {
        public string? Status { get; set; }
        public string? AssetType { get; set; }
    }
}
