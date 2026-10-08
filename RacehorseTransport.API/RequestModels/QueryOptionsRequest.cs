using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class QueryOptionsRequest
    {
        [FromQuery(Name = "search")]
        public string? Search { get; set; }

        // Ví dụ: sort=name,-createdAt (dấu "-" là giảm dần)
        [FromQuery(Name = "sort")]
        public string? Sort { get; set; }

        [FromQuery(Name = "page")]
        [Range(1, int.MaxValue, ErrorMessage = "Page must be a positive integer.")]
        public int Page { get; set; } = 1;

        [FromQuery(Name = "size")]
        [Range(1, 100, ErrorMessage = "Size must be between 1 and 100.")]
        public int Size { get; set; } = 10;
    }
}
