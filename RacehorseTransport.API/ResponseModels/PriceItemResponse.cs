namespace RacehorseTransport.API.ResponseModels
{
    public class PriceItemResponse
    {
        public int PriceItemId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? TransportMode { get; set; }
        public string? DestCountryCode { get; set; }
        public string CalcType { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int? MinHorses { get; set; }
        public bool IsActive { get; set; }
    }
}
