namespace RacehorseTransport.Repositories.Entities
{
    // pricing.price_items
    public class PriceItem
    {
        public int PriceItemId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? TransportMode { get; set; }   // null = áp dụng cho cả Ground và Air
        public string? DestCountryCode { get; set; } // null = áp dụng cho mọi nước đến
        public string CalcType { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int? MinHorses { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
