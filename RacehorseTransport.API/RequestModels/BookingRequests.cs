using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class BookingQueryRequest : QueryOptionsRequest
    {
        [FromQuery(Name = "status")]
        public string? Status { get; set; }

        // Chỉ có tác dụng với nhân sự
        [FromQuery(Name = "customerUserId")]
        public int? CustomerUserId { get; set; }

        [FromQuery(Name = "departureFrom")]
        public DateTimeOffset? DepartureFrom { get; set; }

        [FromQuery(Name = "departureTo")]
        public DateTimeOffset? DepartureTo { get; set; }
    }

    public class CreateBookingRequest
    {
        [Required(ErrorMessage = "PickupAddress is required.")]
        [MaxLength(255, ErrorMessage = "PickupAddress cannot exceed 255 characters.")]
        public string PickupAddress { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "PickupCity cannot exceed 100 characters.")]
        public string? PickupCity { get; set; }

        [Required(ErrorMessage = "PickupCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "PickupCountryCode must be a 2-letter country code.")]
        public string PickupCountryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "DropoffAddress is required.")]
        [MaxLength(255, ErrorMessage = "DropoffAddress cannot exceed 255 characters.")]
        public string DropoffAddress { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "DropoffCity cannot exceed 100 characters.")]
        public string? DropoffCity { get; set; }

        [Required(ErrorMessage = "DropoffCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DropoffCountryCode must be a 2-letter country code.")]
        public string DropoffCountryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "DepartureDate is required.")]
        public DateTimeOffset? DepartureDate { get; set; }

        [Required(ErrorMessage = "DeliveryDate is required.")]
        public DateTimeOffset? DeliveryDate { get; set; }

        [Required(ErrorMessage = "TransportMode is required.")]
        public string TransportMode { get; set; } = string.Empty;

        [Range(1, 50000, ErrorMessage = "DistanceKm must be between 1 and 50000.")]
        public int? DistanceKm { get; set; }

        public bool RequiresClimateControl { get; set; }

        [Range(0, 1000000000, ErrorMessage = "DeclaredValue must be between 0 and 1,000,000,000.")]
        public decimal? DeclaredValue { get; set; }

        [MaxLength(2000, ErrorMessage = "SpecialInstructions cannot exceed 2000 characters.")]
        public string? SpecialInstructions { get; set; }

        [Required(ErrorMessage = "Horses is required.")]
        [MinLength(1, ErrorMessage = "At least one horse is required.")]
        public List<CreateBookingHorseRequest> Horses { get; set; } = new();
    }

    public class CreateBookingHorseRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "HorseId is required.")]
        public int HorseId { get; set; }

        [Required(ErrorMessage = "StallClass is required.")]
        public string StallClass { get; set; } = string.Empty;

        [MaxLength(300, ErrorMessage = "Notes cannot exceed 300 characters.")]
        public string? Notes { get; set; }
    }

    // Duyệt đơn và đổi người phụ trách đều cần chọn một Transport Specialist
    public class AssignSpecialistRequest
    {
        [Required(ErrorMessage = "SpecialistUserId is required.")]
        public int? SpecialistUserId { get; set; }
    }

    public class RejectBookingRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    // Xem giá trước khi gửi đơn. Không ghi gì vào DB.
    public class QuotePreviewRequest
    {
        [Required(ErrorMessage = "TransportMode is required.")]
        public string TransportMode { get; set; } = string.Empty;

        public string? PickupCountryCode { get; set; }
        public string? PickupCity { get; set; }
        public string? PickupAddress { get; set; }

        [Required(ErrorMessage = "DropoffCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DropoffCountryCode must be a 2-letter country code.")]
        public string DropoffCountryCode { get; set; } = string.Empty;

        public string? DropoffCity { get; set; }
        public string? DropoffAddress { get; set; }

        [Range(1, 50000, ErrorMessage = "DistanceKm must be between 1 and 50000.")]
        public int? DistanceKm { get; set; }

        public bool RequiresClimateControl { get; set; }

        [Range(0, 1000000000, ErrorMessage = "DeclaredValue must be between 0 and 1,000,000,000.")]
        public decimal? DeclaredValue { get; set; }

        [Required(ErrorMessage = "DepartureDate is required.")]
        public DateTimeOffset? DepartureDate { get; set; }

        [Required(ErrorMessage = "DeliveryDate is required.")]
        public DateTimeOffset? DeliveryDate { get; set; }

        [Required(ErrorMessage = "Horses is required.")]
        [MinLength(1, ErrorMessage = "At least one horse is required.")]
        public List<QuotePreviewHorseRequest> Horses { get; set; } = new();
    }

    public class QuotePreviewHorseRequest
    {
        [Required(ErrorMessage = "StallClass is required.")]
        public string StallClass { get; set; } = string.Empty;
    }

    public class EstimateDistanceRequest
    {
        public string PickupCountryCode { get; set; } = "VN";
        public string? PickupCity { get; set; }
        public string? PickupAddress { get; set; }

        [Required(ErrorMessage = "DropoffCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DropoffCountryCode must be a 2-letter country code.")]
        public string DropoffCountryCode { get; set; } = string.Empty;

        public string? DropoffCity { get; set; }
        public string? DropoffAddress { get; set; }
    }
}
