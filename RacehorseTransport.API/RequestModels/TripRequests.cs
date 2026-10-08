using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
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
