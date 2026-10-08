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

    // Kế hoạch chuyến: dùng cho cả POST /api/trips và PUT /api/trips/{id}
    public class TripPlanRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "VehicleId is required.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "PlannedStartDate is required.")]
        public DateTimeOffset? PlannedStartDate { get; set; }

        [Required(ErrorMessage = "PlannedEndDate is required.")]
        public DateTimeOffset? PlannedEndDate { get; set; }

        [Range(0, 1000000000, ErrorMessage = "PlannedCost must be between 0 and 1,000,000,000.")]
        public decimal PlannedCost { get; set; }

        [Required(ErrorMessage = "Crew is required.")]
        [MinLength(1, ErrorMessage = "At least one crew member is required.")]
        public List<TripCrewRequest> Crew { get; set; } = new();

        [Required(ErrorMessage = "Horses is required.")]
        [MinLength(1, ErrorMessage = "At least one horse is required.")]
        public List<TripHorseRequest> Horses { get; set; } = new();

        // Tùy chọn. Khi sửa: bỏ trống = giữ nguyên các mốc hiện có, có giá trị = thay toàn bộ.
        public List<CheckpointRequest>? Checkpoints { get; set; }
    }

    public class TripCrewRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Crew UserId is required.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "CrewRole is required.")]
        public string CrewRole { get; set; } = string.Empty;

        public bool IsLead { get; set; }

        [MaxLength(200, ErrorMessage = "Duty cannot exceed 200 characters.")]
        public string? Duty { get; set; }
    }

    public class TripHorseRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "BookingHorseId is required.")]
        public int BookingHorseId { get; set; }

        [Range(1, 12, ErrorMessage = "StallSlotNumber must be between 1 and 12.")]
        public int StallSlotNumber { get; set; }

        [MaxLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
        public string? Notes { get; set; }
    }

    public class CheckpointRequest
    {
        [Required(ErrorMessage = "CheckpointName is required.")]
        [MaxLength(100, ErrorMessage = "CheckpointName cannot exceed 100 characters.")]
        public string CheckpointName { get; set; } = string.Empty;

        [Required(ErrorMessage = "CheckpointType is required.")]
        public string CheckpointType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "PlannedTime is required.")]
        public DateTimeOffset? PlannedTime { get; set; }

        [Range(0, 2880, ErrorMessage = "MandatoryRestMinutes must be between 0 and 2880.")]
        public int MandatoryRestMinutes { get; set; }

        // Chỉ có tác dụng khi thêm một mốc: vị trí chèn (1 = đầu tiên). Bỏ trống = thêm vào cuối.
        [Range(1, 1000, ErrorMessage = "Position must be 1 or greater.")]
        public int? Position { get; set; }
    }

    public class ReorderCheckpointsRequest
    {
        [Required(ErrorMessage = "CheckpointIds is required.")]
        [MinLength(1, ErrorMessage = "CheckpointIds cannot be empty.")]
        public List<int> CheckpointIds { get; set; } = new();
    }

    public class RejectTripRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
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
