using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // billing.payments: mỗi lần thu (cọc / phần còn lại / phát sinh) hoặc hoàn tiền là một dòng
    public class Payment
    {
        public int PaymentId { get; set; }
        public string PaymentCode { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string PaymentType { get; set; } = PaymentTypes.Deposit;
        public string Provider { get; set; } = PaymentProviders.VnPay;
        public string Status { get; set; } = PaymentStatuses.Pending;
        public decimal Amount { get; set; }                 // theo CurrencyCode (USD như báo giá)
        public string CurrencyCode { get; set; } = "USD";
        public decimal ExchangeRate { get; set; }           // VND cho 1 đơn vị CurrencyCode
        public long AmountVnd { get; set; }
        public string TxnRef { get; set; } = string.Empty;  // vnp_TxnRef
        public string? ProviderTxnNo { get; set; }          // vnp_TransactionNo
        public string? ProviderPayDate { get; set; }        // vnp_PayDate (yyyyMMddHHmmss, giờ VN)
        public string? BankCode { get; set; }
        public string? ResponseCode { get; set; }
        public int? RefundOfPaymentId { get; set; }
        public string? Note { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? PaidAt { get; set; }
        public string? RawResponse { get; set; }            // JSON trả về từ cổng thanh toán

        // Navigation properties
        public virtual BookingRequest? Booking { get; set; }
        public virtual Payment? RefundOf { get; set; }
        public virtual User? CreatedBy { get; set; }
    }
}
