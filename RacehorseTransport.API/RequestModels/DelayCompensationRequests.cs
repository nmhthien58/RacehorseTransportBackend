using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    public class ApproveDelayCompensationRequest
    {
        // Số tiền hoàn (mặc định gửi lại calculatedAmount); khác số tính thì bắt buộc Reason
        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal? Amount { get; set; }

        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }
    }

    public class RejectDelayCompensationRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
