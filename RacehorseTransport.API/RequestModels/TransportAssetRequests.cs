using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class TransportAssetQueryRequest : QueryOptionsRequest
    {
        [FromQuery(Name = "status")]
        [MaxLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        public string? Status { get; set; }

        [FromQuery(Name = "assetType")]
        [MaxLength(30, ErrorMessage = "AssetType cannot exceed 30 characters.")]
        public string? AssetType { get; set; }
    }

    public class CreateTransportAssetRequest
    {
        [Required(ErrorMessage = "AssetCode is required.")]
        [MaxLength(30, ErrorMessage = "AssetCode cannot exceed 30 characters.")]
        public string AssetCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "AssetType is required.")]
        [MaxLength(30, ErrorMessage = "AssetType cannot exceed 30 characters.")]
        public string AssetType { get; set; } = string.Empty;

        [Range(1, 12, ErrorMessage = "CapacityHorses must be between 1 and 12.")]
        public int CapacityHorses { get; set; }

        public bool HasClimateControl { get; set; } = true;
        public bool HasGpsTracker { get; set; } = true;

        [Required(ErrorMessage = "Status is required.")]
        [MaxLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        public string Status { get; set; } = string.Empty;

        public DateTimeOffset? LastSanitizationDate { get; set; }
    }

    public class UpdateTransportAssetRequest
    {
        [Required(ErrorMessage = "AssetCode is required.")]
        [MaxLength(30, ErrorMessage = "AssetCode cannot exceed 30 characters.")]
        public string AssetCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "AssetType is required.")]
        [MaxLength(30, ErrorMessage = "AssetType cannot exceed 30 characters.")]
        public string AssetType { get; set; } = string.Empty;

        [Range(1, 12, ErrorMessage = "CapacityHorses must be between 1 and 12.")]
        public int CapacityHorses { get; set; }

        public bool HasClimateControl { get; set; }
        public bool HasGpsTracker { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [MaxLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        public string Status { get; set; } = string.Empty;

        public DateTimeOffset? LastSanitizationDate { get; set; }
    }
}
