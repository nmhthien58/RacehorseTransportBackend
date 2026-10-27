using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    // R-16 (BE-16): đoàn xe ghi không giao được
    public class RecordDeliveryFailureRequest
    {
        // RecipientAbsent | RecipientRefused | RefusedInjured | RefusedWrongHorse
        [Required(ErrorMessage = "Reason is required.")]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Note cannot exceed 1000 characters.")]
        public string? Note { get; set; }

        // Ảnh tải lên trước bằng POST /api/files
        [MaxLength(10, ErrorMessage = "At most 10 photos.")]
        public List<string>? PhotoPaths { get; set; }
    }

    // R-16 (BE-16): khách chọn cách xử lý khi không giao được
    public class DeliveryDecisionRequest
    {
        // AlternateRecipient | HoldAtStable | ReturnToOrigin
        [Required(ErrorMessage = "Decision is required.")]
        public string Decision { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "RecipientName cannot exceed 100 characters.")]
        public string? RecipientName { get; set; }

        [MaxLength(20, ErrorMessage = "RecipientPhone cannot exceed 20 characters.")]
        public string? RecipientPhone { get; set; }

        [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string? Address { get; set; }

        [Range(-90, 90, ErrorMessage = "Lat must be between -90 and 90.")]
        public decimal? Lat { get; set; }

        [Range(-180, 180, ErrorMessage = "Lng must be between -180 and 180.")]
        public decimal? Lng { get; set; }
    }
}
