namespace RacehorseTransport.Services.BusinessModels
{
    // Đầu vào của công thức báo giá (docs/TASKS.md, phụ lục "Báo giá")
    public class QuoteInputModel
    {
        public string TransportMode { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public int? DistanceKm { get; set; }
        public bool RequiresClimateControl { get; set; }
        public decimal? DeclaredValue { get; set; } // null = không mua bảo hiểm
        public DateTimeOffset DepartureDate { get; set; }
        public DateTimeOffset DeliveryDate { get; set; }

        // Hạng chuồng của từng con ngựa trong đơn (mỗi phần tử một con)
        public List<string> StallClasses { get; set; } = new();
    }

    public class QuoteLineModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class QuoteModel
    {
        public List<QuoteLineModel> Lines { get; set; } = new();
        public decimal Total { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public int HorseCount { get; set; }
        public int GroomCount { get; set; }
        public int TransitDays { get; set; }
        public bool IsExpress { get; set; } // hệ thống tự xác định: khởi hành dưới 7 ngày kể từ lúc báo giá
    }
}
