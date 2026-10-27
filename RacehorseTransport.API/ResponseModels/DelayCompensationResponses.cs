namespace RacehorseTransport.API.ResponseModels
{
    public class DelayCompensationResponse
    {
        public int DelayCompensationId { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public decimal DelayHours { get; set; }          // giờ trễ so với hạn giao
        public decimal FreightAmount { get; set; }       // tiền cước = báo giá - bảo hiểm - phí thủ tục
        public decimal Percent { get; set; }             // % tiền cước được bồi thường
        public decimal CalculatedAmount { get; set; }    // số tiền hệ thống tính
        public decimal? ApprovedAmount { get; set; }     // số tiền Manager duyệt
        public string CurrencyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // AwaitingApproval | Approved | Rejected
        public string? ReviewReason { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
