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

        // Khoảng ngày ngựa sẵn sàng được đón và hạn giao trễ nhất.
        // Xem giá trước có thể để trống: khi đó chỉ trả về ngày pickup sớm nhất và thời gian vận chuyển.
        public DateTimeOffset? PickupWindowStart { get; set; }
        public DateTimeOffset? PickupWindowEnd { get; set; }
        public DateTimeOffset? DeliveryDeadline { get; set; }

        // Thông tin địa chỉ phục vụ tự động tính khoảng cách đường bộ
        public string? PickupCountryCode { get; set; }
        public string? PickupCity { get; set; }
        public string? PickupAddress { get; set; }
        public string? DropoffCity { get; set; }
        public string? DropoffAddress { get; set; }

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

        // Express: pickup sát ngày sớm nhất (phải làm hồ sơ gấp) hoặc hạn giao sát thời gian vận chuyển
        public bool IsExpress { get; set; }
        public int? DistanceKm { get; set; }
        public decimal? EstimatedHours { get; set; } // tổng thời gian vận chuyển dự kiến (giờ)
        public string? RouteSummary { get; set; }

        // Lịch: ngày pickup sớm nhất được chọn, pickup trước ngày ExpressUntilDate thì tính phụ phí Express,
        // hạn giao sớm nhất khả thi tính từ ngày pickup đầu khoảng (hoặc từ ngày sớm nhất khi chưa chọn ngày)
        public int LeadTimeDays { get; set; }
        public DateOnly EarliestPickupDate { get; set; }
        public DateOnly ExpressUntilDate { get; set; }
        public DateTimeOffset EarliestDeliveryDeadline { get; set; }

        public TransitEstimateModel Transit { get; set; } = new();
    }
}
