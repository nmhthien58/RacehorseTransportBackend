using System.Text.Json.Serialization;

namespace RacehorseTransport.API.ResponseModels
{
    public class PaymentResponse
    {
        public int PaymentId { get; set; }
        public string PaymentCode { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string? BookingCode { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal ExchangeRate { get; set; }
        public long AmountVnd { get; set; }
        public string? ProviderTxnNo { get; set; }
        public string? BankCode { get; set; }
        public string? ResponseCode { get; set; }
        public int? RefundOfPaymentId { get; set; }
        public string? Note { get; set; }
        public string? CreatedByName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
    }

    public class BookingPaymentSummaryResponse
    {
        public bool PaymentRequired { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal UsdVndRate { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal? DepositPercent { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal DepositPaid { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal BalancePaid { get; set; }
        public DateTimeOffset? BalanceDueAt { get; set; }
        public decimal ExtraAmount { get; set; }
        public decimal ExtraPaid { get; set; }
        public DateTimeOffset? ExtraDueAt { get; set; }
        public bool ExtraOverdue { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal CompensationAmount { get; set; } // R-13: bồi thường bảo hiểm đã chi
        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
        public string? NextPaymentType { get; set; }
        public decimal? NextPaymentAmount { get; set; }
        public long? NextPaymentAmountVnd { get; set; }
        public List<PaymentResponse> Payments { get; set; } = new();
    }

    public class CustomerBillingResponse
    {
        public string CurrencyCode { get; set; } = "USD";
        public decimal UsdVndRate { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRefunded { get; set; }
        public decimal TotalOutstanding { get; set; }
        public long TotalOutstandingVnd { get; set; }
        public decimal TotalPayableNow { get; set; }
        public long TotalPayableNowVnd { get; set; }
        public int PayableNowCount { get; set; }
        public int OverdueCount { get; set; }
        public List<CustomerBillingBookingResponse> Bookings { get; set; } = new();
        public List<PaymentResponse> Payments { get; set; } = new();
    }

    public class CustomerBillingBookingResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PickupCountryCode { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public int TotalHorses { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public BookingPaymentSummaryResponse Summary { get; set; } = new();
    }

    public class CreatePaymentResponse
    {
        public PaymentResponse Payment { get; set; } = new();
        public string PaymentUrl { get; set; } = string.Empty;
    }

    public class PaymentCallbackResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PaymentResponse? Payment { get; set; }
    }

    // VNPay đọc đúng tên RspCode / Message (phân biệt hoa thường), nên không để camelCase mặc định đổi tên
    public class VnPayIpnResponse
    {
        [JsonPropertyName("RspCode")]
        public string RspCode { get; set; } = string.Empty;

        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;
    }

    public class PaymentSettingsResponse
    {
        public decimal DepositPercent { get; set; }
        public decimal CancelRefundPercent { get; set; }
        public decimal UsdVndRate { get; set; }
        public int BalanceDueDaysBeforeDeparture { get; set; }
        public int ExtraPaymentDueDays { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }
    }
}
