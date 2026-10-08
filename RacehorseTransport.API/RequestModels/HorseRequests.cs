using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class HorseQueryRequest : QueryOptionsRequest
    {
        // Chỉ có tác dụng với nhân sự; khách hàng luôn chỉ thấy ngựa của mình
        [FromQuery(Name = "ownerUserId")]
        public int? OwnerUserId { get; set; }

        [FromQuery(Name = "includeInactive")]
        public bool IncludeInactive { get; set; }
    }

    public class CreateHorseRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "MicrochipNumber is required.")]
        [MaxLength(30, ErrorMessage = "MicrochipNumber cannot exceed 30 characters.")]
        public string MicrochipNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "PassportNumber is required.")]
        [MaxLength(50, ErrorMessage = "PassportNumber cannot exceed 50 characters.")]
        public string PassportNumber { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Breed cannot exceed 50 characters.")]
        public string? Breed { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "DateOfBirth is required.")]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Color is required.")]
        [MaxLength(30, ErrorMessage = "Color cannot exceed 30 characters.")]
        public string Color { get; set; } = string.Empty;

        public string? SpecialCareRequirements { get; set; }

        [MaxLength(500, ErrorMessage = "PhotoUrl cannot exceed 500 characters.")]
        public string? PhotoUrl { get; set; }
    }

    public class UpdateHorseRequest : CreateHorseRequest
    {
    }
}
