using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class CountryDocRequirementQueryRequest
    {
        [FromQuery(Name = "countryCode")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "CountryCode must contain exactly two letters.")]
        public string? CountryCode { get; set; }

        [FromQuery(Name = "direction")]
        [MaxLength(10, ErrorMessage = "Direction cannot exceed 10 characters.")]
        public string? Direction { get; set; }

        [FromQuery(Name = "page")]
        [Range(1, int.MaxValue, ErrorMessage = "Page must be a positive integer.")]
        public int Page { get; set; } = 1;

        [FromQuery(Name = "size")]
        [Range(1, 100, ErrorMessage = "Size must be between 1 and 100.")]
        public int Size { get; set; } = 10;
    }

    public class CreateCountryDocRequirementRequest
    {
        [Required(ErrorMessage = "CountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "CountryCode must contain exactly two letters.")]
        public string CountryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Direction is required.")]
        [MaxLength(10, ErrorMessage = "Direction cannot exceed 10 characters.")]
        public string Direction { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "DocTypeId must be a positive integer.")]
        public int DocTypeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ValidityDays must be greater than 0 when provided.")]
        public int? ValidityDays { get; set; }

        // Số ngày cần để có giấy (xin phép, xét nghiệm, cách ly); dùng tính ngày pickup sớm nhất của tuyến
        [Range(0, 365, ErrorMessage = "ProcessingDays must be between 0 and 365.")]
        public int? ProcessingDays { get; set; }

        public bool IsMandatory { get; set; } = true;

        [MaxLength(255, ErrorMessage = "RegulationNote cannot exceed 255 characters.")]
        public string? RegulationNote { get; set; }
    }

    public class UpdateCountryDocRequirementRequest
    {
        [Required(ErrorMessage = "CountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "CountryCode must contain exactly two letters.")]
        public string CountryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Direction is required.")]
        [MaxLength(10, ErrorMessage = "Direction cannot exceed 10 characters.")]
        public string Direction { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "DocTypeId must be a positive integer.")]
        public int DocTypeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ValidityDays must be greater than 0 when provided.")]
        public int? ValidityDays { get; set; }

        // Số ngày cần để có giấy (xin phép, xét nghiệm, cách ly); dùng tính ngày pickup sớm nhất của tuyến
        [Range(0, 365, ErrorMessage = "ProcessingDays must be between 0 and 365.")]
        public int? ProcessingDays { get; set; }

        public bool IsMandatory { get; set; } = true;

        [MaxLength(255, ErrorMessage = "RegulationNote cannot exceed 255 characters.")]
        public string? RegulationNote { get; set; }
    }
}
