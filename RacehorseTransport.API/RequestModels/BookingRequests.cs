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

    // Xem giá trước khi gửi đơn. Không ghi gì vào DB.
    public class QuotePreviewRequest
    {
        [Required(ErrorMessage = "TransportMode is required.")]
        public string TransportMode { get; set; } = string.Empty;

        [Required(ErrorMessage = "DropoffCountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DropoffCountryCode must be a 2-letter country code.")]
        public string DropoffCountryCode { get; set; } = string.Empty;

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
}
