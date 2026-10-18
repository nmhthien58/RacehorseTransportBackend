using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    // Khách tạo link thanh toán cho khoản đang mở của đơn
    public class CreatePaymentRequest
    {
        [Required(ErrorMessage = "PaymentType is required.")]
        public string PaymentType { get; set; } = string.Empty; // Deposit | Balance | Extra
    }

    // Manager ghi nhận tiền khách đã trả ngoài hệ thống (chuyển khoản...)
    public class ManualPaymentRequest
    {
        [Required(ErrorMessage = "PaymentType is required.")]
        public string PaymentType { get; set; } = string.Empty;

        [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Reference is required.")]
        [MaxLength(100, ErrorMessage = "Reference cannot exceed 100 characters.")]
        public string Reference { get; set; } = string.Empty; // mã chuyển khoản / số biên lai

        [MaxLength(300, ErrorMessage = "Note cannot exceed 300 characters.")]
        public string? Note { get; set; }
    }

    public class MockPaymentCompleteRequest
    {
        public bool Success { get; set; } = true;
    }

    public class MarkRefundedRequest
    {
        [Required(ErrorMessage = "Reference is required.")]
        [MaxLength(50, ErrorMessage = "Reference cannot exceed 50 characters.")]
        public string Reference { get; set; } = string.Empty;

        [MaxLength(300, ErrorMessage = "Note cannot exceed 300 characters.")]
        public string? Note { get; set; }
    }

    public class UpdatePaymentSettingsRequest
    {
        [Range(0, 100, ErrorMessage = "DepositPercent must be between 0 and 100.")]
        public decimal DepositPercent { get; set; }

        [Range(0, 100, ErrorMessage = "CancelRefundPercent must be between 0 and 100.")]
        public decimal CancelRefundPercent { get; set; }

        [Range(1, 1000000, ErrorMessage = "UsdVndRate must be between 1 and 1,000,000.")]
        public decimal UsdVndRate { get; set; }

        [Range(0, 30, ErrorMessage = "BalanceDueDaysBeforeDeparture must be between 0 and 30.")]
        public int BalanceDueDaysBeforeDeparture { get; set; }

        [Range(1, 90, ErrorMessage = "ExtraPaymentDueDays must be between 1 and 90.")]
        public int ExtraPaymentDueDays { get; set; }
    }
}
