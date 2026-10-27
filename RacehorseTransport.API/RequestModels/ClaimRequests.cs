using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    // R-13 (BE-12): khách mở hồ sơ bồi thường trong 24 giờ sau khi nhận ngựa
    public class OpenClaimRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "BookingHorseId is required.")]
        public int BookingHorseId { get; set; }

        // Death | Injury | Loss
        [Required(ErrorMessage = "ClaimType is required.")]
        public string ClaimType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Range(0, 100000000, ErrorMessage = "RequestedAmount must be between 0 and 100,000,000.")]
        public decimal? RequestedAmount { get; set; }

        // Biên bản thú y, ảnh, hóa đơn thú y đã tải lên bằng POST /api/files
        [MaxLength(20, ErrorMessage = "At most 20 files.")]
        public List<string>? EvidencePaths { get; set; }
    }

    public class ClaimEvidenceRequest
    {
        [Required(ErrorMessage = "EvidencePaths is required.")]
        [MinLength(1, ErrorMessage = "Send at least one file path.")]
        [MaxLength(20, ErrorMessage = "At most 20 files.")]
        public List<string> EvidencePaths { get; set; } = new();
    }

    public class ApproveClaimRequest
    {
        [Range(0.01, 100000000, ErrorMessage = "ApprovedAmount must be greater than 0.")]
        public decimal ApprovedAmount { get; set; }

        // Company | Customer | ForceMajeure. Bỏ trống = lấy theo sự cố đã đóng. Gói không mua chỉ trả khi Company.
        public string? FaultParty { get; set; }

        [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters.")]
        public string? Reason { get; set; }
    }

    public class RejectClaimRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class PayClaimRequest
    {
        // Mã tham chiếu chuyển khoản cho phần vượt số khách đã trả
        [MaxLength(100, ErrorMessage = "ManualReference cannot exceed 100 characters.")]
        public string? ManualReference { get; set; }
    }
}
