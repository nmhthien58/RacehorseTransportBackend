namespace RacehorseTransport.Services.BusinessModels
{
    // Cấu hình thanh toán (common.system_settings)
    public class PaymentSettingsModel
    {
        public decimal DepositPercent { get; set; } = 30;
        public decimal CancelRefundPercent { get; set; } = 50;
        public decimal UsdVndRate { get; set; } = 26000;
        public int BalanceDueDaysBeforeDeparture { get; set; } = 2;
        public int ExtraPaymentDueDays { get; set; } = 7;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }
    }

    public class PaymentModel
    {
        public int PaymentId { get; set; }
        public string PaymentCode { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal ExchangeRate { get; set; }
        public long AmountVnd { get; set; }
        public string TxnRef { get; set; } = string.Empty;
        public string? ProviderTxnNo { get; set; }
        public string? BankCode { get; set; }
        public string? ResponseCode { get; set; }
        public int? RefundOfPaymentId { get; set; }
        public string? Note { get; set; }
        public string? CreatedByName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
    }

    // Tình hình tiền của một đơn: phải trả bao nhiêu, đã trả bao nhiêu, khoản kế tiếp
    public class BookingPaymentSummaryModel
    {
        public bool PaymentRequired { get; set; }          // false: đơn cũ (trước khi có thanh toán)
        public string CurrencyCode { get; set; } = "USD";
        public decimal UsdVndRate { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal? DepositPercent { get; set; }

        public decimal DepositAmount { get; set; }
        public decimal DepositPaid { get; set; }
        public decimal BalanceAmount { get; set; }         // giá báo - cọc
        public decimal BalancePaid { get; set; }
        public DateTimeOffset? BalanceDueAt { get; set; }  // trước ngày xuất phát của chuyến sớm nhất
        public decimal ExtraAmount { get; set; }           // chi phí phát sinh
        public decimal ExtraPaid { get; set; }
        public DateTimeOffset? ExtraDueAt { get; set; }
        public bool ExtraOverdue { get; set; }
        public decimal RefundedAmount { get; set; }

        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }

        // Khoản khách có thể trả ngay (null = không có khoản nào đang mở)
        public string? NextPaymentType { get; set; }
        public decimal? NextPaymentAmount { get; set; }
        public long? NextPaymentAmountVnd { get; set; }

        public List<PaymentModel> Payments { get; set; } = new();
    }

    public class CreatePaymentResultModel
    {
        public PaymentModel Payment { get; set; } = new();
        public string PaymentUrl { get; set; } = string.Empty; // mở link này để khách thanh toán
    }

    // Kết quả xử lý lời gọi lại của cổng thanh toán (ReturnUrl / IPN)
    public class PaymentCallbackResultModel
    {
        public string RspCode { get; set; } = "99";   // mã trả lời VNPay IPN: 00 ok, 01 không thấy, 02 đã xử lý, 04 sai số tiền, 97 sai chữ ký
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public PaymentModel? Payment { get; set; }
    }

    public class ManualPaymentModel
    {
        public string PaymentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public string? Note { get; set; }
    }

    // ---------- Cổng thanh toán ----------

    public class GatewayPaymentRequest
    {
        public string TxnRef { get; set; } = string.Empty;
        public long AmountVnd { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string ClientIp { get; set; } = "127.0.0.1";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public class GatewayCallbackData
    {
        public bool IsSignatureValid { get; set; }
        public string TxnRef { get; set; } = string.Empty;
        public long AmountVnd { get; set; }
        public string ResponseCode { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public string? TransactionNo { get; set; }
        public string? PayDate { get; set; }
        public string? BankCode { get; set; }
        public bool IsSuccess => ResponseCode == "00" && (TransactionStatus == "00" || string.IsNullOrEmpty(TransactionStatus));
    }

    public class GatewayRefundRequest
    {
        public string RefundTxnRef { get; set; } = string.Empty;   // mã của dòng Refund (dùng làm vnp_RequestId)
        public string OriginalTxnRef { get; set; } = string.Empty;
        public string? OriginalTransactionNo { get; set; }
        public string? OriginalPayDate { get; set; }
        public long AmountVnd { get; set; }
        public bool IsFullRefund { get; set; }
        public string CreatedBy { get; set; } = "system";
        public string ClientIp { get; set; } = "127.0.0.1";
        public string OrderInfo { get; set; } = string.Empty;
    }

    public class GatewayRefundResult
    {
        public bool Success { get; set; }
        public string ResponseCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? TransactionNo { get; set; }
        public string? RawResponse { get; set; }
    }
}
