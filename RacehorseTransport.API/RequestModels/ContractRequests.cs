using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    // R-17: khách chấp nhận một bản hợp đồng sửa đổi (bản đầu chấp nhận cùng báo giá: POST /api/bookings/{id}/accept-quote)
    public class AcceptContractRequest
    {
        [Required(ErrorMessage = "ContractSha256 is required.")]
        [MaxLength(64, ErrorMessage = "ContractSha256 cannot exceed 64 characters.")]
        public string ContractSha256 { get; set; } = string.Empty;

        public bool AcceptPolicy { get; set; }

        [Required(ErrorMessage = "PolicyVersion is required.")]
        [MaxLength(20, ErrorMessage = "PolicyVersion cannot exceed 20 characters.")]
        public string PolicyVersion { get; set; } = string.Empty;
    }

    // R-17: vận đơn hàng không của chuyến
    public class UpdateAirWaybillRequest
    {
        [Required(ErrorMessage = "AirWaybillNumber is required.")]
        [MaxLength(50, ErrorMessage = "AirWaybillNumber cannot exceed 50 characters.")]
        public string AirWaybillNumber { get; set; } = string.Empty;

        // Đường dẫn file do POST /api/files (category documents) trả về; bỏ trống thì giữ file cũ
        [MaxLength(255, ErrorMessage = "AirWaybillFilePath cannot exceed 255 characters.")]
        public string? AirWaybillFilePath { get; set; }
    }
}
