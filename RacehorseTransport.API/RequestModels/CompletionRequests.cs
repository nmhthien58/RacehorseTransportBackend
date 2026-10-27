using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    // multipart/form-data: ảnh chữ ký gửi kèm trong trường "signature"
    public class CreateHandoverRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "BookingId is required.")]
        public int BookingId { get; set; }

        // Giao tại chỗ (R-12): con ngựa rời chuyến với hướng xử lý HandOverHere. Bỏ trống = giao ở điểm trả của đơn.
        public int? TripHorseId { get; set; }

        [Required(ErrorMessage = "RecipientName is required.")]
        [MaxLength(100, ErrorMessage = "RecipientName cannot exceed 100 characters.")]
        public string RecipientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "RecipientPhone is required.")]
        [RegularExpression(@"^\+?[0-9 ().\-]{6,30}$", ErrorMessage = "RecipientPhone format is invalid.")]
        public string RecipientPhone { get; set; } = string.Empty;

        // Số CMND / CCCD / hộ chiếu của người nhận
        [Required(ErrorMessage = "RecipientIdCard is required.")]
        [MaxLength(50, ErrorMessage = "RecipientIdCard cannot exceed 50 characters.")]
        public string RecipientIdCard { get; set; } = string.Empty;

        [Required(ErrorMessage = "OverallCondition is required.")]
        public string OverallCondition { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Remarks cannot exceed 2000 characters.")]
        public string? Remarks { get; set; }

        // Mã vi mạch quét được khi giao; lặp lại trường này cho từng con ngựa
        [Required(ErrorMessage = "MicrochipNumbers is required.")]
        [MinLength(1, ErrorMessage = "At least one microchip number is required.")]
        public List<string> MicrochipNumbers { get; set; } = new();

        // Ảnh chữ ký vẽ tay của người nhận (jpg, png)
        [Required(ErrorMessage = "Signature is required.")]
        public IFormFile? Signature { get; set; }
    }

    public class CloseTripRequest
    {
        [MaxLength(2000, ErrorMessage = "ExecutiveRemarks cannot exceed 2000 characters.")]
        public string? ExecutiveRemarks { get; set; }
    }

    public class ReportQueryRequest
    {
        [FromQuery(Name = "from")]
        public DateTimeOffset? From { get; set; }

        [FromQuery(Name = "to")]
        public DateTimeOffset? To { get; set; }
    }
}
