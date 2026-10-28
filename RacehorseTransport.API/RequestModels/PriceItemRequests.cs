using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class PriceItemQueryRequest : QueryOptionsRequest
    {
        [FromQuery(Name = "category")]
        public string? Category { get; set; }

        [FromQuery(Name = "transportMode")]
        public string? TransportMode { get; set; }

        [FromQuery(Name = "isActive")]
        public bool? IsActive { get; set; }

        /// <summary>Chỉ lấy dòng giá riêng của nước đến này (mã 2 chữ, ví dụ JP).</summary>
        [FromQuery(Name = "destCountryCode")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DestCountryCode must contain exactly two letters.")]
        public string? DestCountryCode { get; set; }

        /// <summary>true: chỉ lấy dòng giá chung, áp cho mọi nước đến.</summary>
        [FromQuery(Name = "globalOnly")]
        public bool? GlobalOnly { get; set; }
    }

    public class CreatePriceItemRequest
    {
        [Required(ErrorMessage = "Category is required.")]
        [MaxLength(20, ErrorMessage = "Category cannot exceed 20 characters.")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "ItemCode is required.")]
        [MaxLength(30, ErrorMessage = "ItemCode cannot exceed 30 characters.")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "ItemCode may only contain letters, digits and underscores.")]
        public string ItemCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "ItemName is required.")]
        [MaxLength(100, ErrorMessage = "ItemName cannot exceed 100 characters.")]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(10, ErrorMessage = "TransportMode cannot exceed 10 characters.")]
        public string? TransportMode { get; set; }

        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "DestCountryCode must contain exactly two letters.")]
        public string? DestCountryCode { get; set; }

        [Required(ErrorMessage = "CalcType is required.")]
        [MaxLength(20, ErrorMessage = "CalcType cannot exceed 20 characters.")]
        public string CalcType { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "99999999999999.9999", ErrorMessage = "Value must fit PostgreSQL numeric(18,4) and cannot be negative.")]
        public decimal Value { get; set; }

        public int? MinHorses { get; set; }
    }

    public class UpdatePriceItemRequest : CreatePriceItemRequest
    {
    }

    public class SetPriceItemActiveRequest
    {
        [Required(ErrorMessage = "IsActive is required.")]
        public bool? IsActive { get; set; }
    }
}
