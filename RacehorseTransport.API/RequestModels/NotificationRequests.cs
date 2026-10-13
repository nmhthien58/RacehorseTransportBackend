using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class NotificationQueryRequest
    {
        [FromQuery(Name = "isRead")]
        public bool? IsRead { get; set; }

        [FromQuery(Name = "page")]
        [Range(1, int.MaxValue, ErrorMessage = "Page must be a positive integer.")]
        public int Page { get; set; } = 1;

        [FromQuery(Name = "size")]
        [Range(1, 100, ErrorMessage = "Size must be between 1 and 100.")]
        public int Size { get; set; } = 10;
    }
}
