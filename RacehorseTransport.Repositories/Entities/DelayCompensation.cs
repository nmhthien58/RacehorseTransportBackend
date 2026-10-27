using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // billing.delay_compensations: bồi thường giao trễ của một đơn trên một chuyến (R-09).
    // Tính khi đóng chuyến; Manager duyệt thì tiền được hoàn qua các dòng Refund của billing.payments.
    public class DelayCompensation
    {
        public int DelayCompensationId { get; set; }
        public int BookingId { get; set; }
        public int TripId { get; set; }
        public decimal DelayHours { get; set; }          // giờ ký e-POD - hạn giao
        public decimal FreightAmount { get; set; }       // tiền cước = báo giá - bảo hiểm - phí thủ tục
        public decimal Percent { get; set; }             // % tiền cước được bồi thường
        public decimal CalculatedAmount { get; set; }    // số tiền hệ thống tính
        public decimal? ApprovedAmount { get; set; }     // số tiền Manager duyệt (có thể khác số tính)
        public string CurrencyCode { get; set; } = "USD";
        public string Status { get; set; } = DelayCompensationStatuses.AwaitingApproval;
        public string? ReviewReason { get; set; }        // bắt buộc khi sửa số tiền hoặc từ chối
        public int? ReviewedByUserId { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual BookingRequest? Booking { get; set; }
        public virtual Trip? Trip { get; set; }
        public virtual User? ReviewedBy { get; set; }
    }
}
