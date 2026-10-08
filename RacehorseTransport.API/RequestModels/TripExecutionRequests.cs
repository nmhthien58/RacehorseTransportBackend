using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    // multipart/form-data
    public class CreateWelfareLogRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "TripHorseId is required.")]
        public int TripHorseId { get; set; }

        public int? CheckpointId { get; set; }

        [Required(ErrorMessage = "CabinTemp is required.")]
        [Range(-30, 60, ErrorMessage = "CabinTemp must be between -30 and 60 (°C).")]
        public decimal? CabinTemp { get; set; }

        [Required(ErrorMessage = "WaterIntakeLiters is required.")]
        [Range(0, 200, ErrorMessage = "WaterIntakeLiters must be between 0 and 200.")]
        public decimal? WaterIntakeLiters { get; set; }

        [Required(ErrorMessage = "FeedStatus is required.")]
        public string FeedStatus { get; set; } = string.Empty;

        [Required(ErrorMessage = "StressLevel is required.")]
        public string StressLevel { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
        public string? Notes { get; set; }

        // Ảnh chụp kèm (jpg, png), không bắt buộc
        public IFormFile? Photo { get; set; }
    }
}
