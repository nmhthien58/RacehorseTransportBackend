using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    // R-10: Coordinator gửi các mốc trung gian theo thứ tự đi; điểm đón / trả lấy từ đơn
    public class SaveRoutePlanRequest
    {
        [MaxLength(23, ErrorMessage = "A route can have at most 23 stops between the pickup and the drop-off.")]
        public List<RoutePlanStopRequest> Stops { get; set; } = new();

        [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
        public string? Notes { get; set; }

        // Chỉ cần với đơn cũ chưa có tọa độ điểm đón / trả
        [Range(-90, 90, ErrorMessage = "OriginLat must be between -90 and 90.")]
        public decimal? OriginLat { get; set; }

        [Range(-180, 180, ErrorMessage = "OriginLng must be between -180 and 180.")]
        public decimal? OriginLng { get; set; }

        [Range(-90, 90, ErrorMessage = "DestinationLat must be between -90 and 90.")]
        public decimal? DestinationLat { get; set; }

        [Range(-180, 180, ErrorMessage = "DestinationLng must be between -180 and 180.")]
        public decimal? DestinationLng { get; set; }
    }

    public class RoutePlanStopRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        // RestStop | BorderGate
        [Required(ErrorMessage = "StopType is required.")]
        public string StopType { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string? Address { get; set; }

        [Range(-90, 90, ErrorMessage = "Lat must be between -90 and 90.")]
        public decimal Lat { get; set; }

        [Range(-180, 180, ErrorMessage = "Lng must be between -180 and 180.")]
        public decimal Lng { get; set; }

        // Biết nước của mốc (từ kết quả tìm địa chỉ) thì gửi kèm để khỏi tra lại
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "CountryCode must be a 2-letter ISO code such as VN.")]
        public string? CountryCode { get; set; }

        [Range(0, 1440, ErrorMessage = "RestMinutes must be between 0 and 1440.")]
        public int RestMinutes { get; set; }
    }

    public class RejectRoutePlanRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}
