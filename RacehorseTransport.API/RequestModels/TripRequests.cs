using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    // Khoảng thời gian dự kiến của chuyến, dùng để tìm xe / nhân sự còn rảnh
    public class AvailabilityQueryRequest
    {
        [FromQuery(Name = "from")]
        [Required(ErrorMessage = "'from' is required.")]
        public DateTimeOffset? From { get; set; }

        [FromQuery(Name = "to")]
        [Required(ErrorMessage = "'to' is required.")]
        public DateTimeOffset? To { get; set; }

        [FromQuery(Name = "transportMode")]
        public string? TransportMode { get; set; }
    }

    public class TripQueryRequest : QueryOptionsRequest
    {
        [FromQuery(Name = "status")]
        public string? Status { get; set; }

        [FromQuery(Name = "startFrom")]
        public DateTimeOffset? StartFrom { get; set; }

        [FromQuery(Name = "startTo")]
        public DateTimeOffset? StartTo { get; set; }
    }
}
