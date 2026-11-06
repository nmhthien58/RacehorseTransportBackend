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

        // Khoang trên xe (1 = khoang đầu) và vị trí trong khoang (1-3). Mỗi khoang chỉ chứa ngựa cùng hạng chuồng:
        // Shared tối đa 3 con, Comfort 2, Private 1.
        [Range(1, 4, ErrorMessage = "CompartmentNo must be between 1 and 4.")]
        public int? CompartmentNo { get; set; }

        [Range(1, 3, ErrorMessage = "PositionNo must be between 1 and 3.")]
        public int? PositionNo { get; set; }

        // Cách cũ, vẫn nhận: chỉ dùng khi không gửi CompartmentNo. Ô n = khoang ceil(n / 3), vị trí ((n - 1) % 3) + 1.
        [Range(1, 12, ErrorMessage = "StallSlotNumber must be between 1 and 12.")]
        public int? StallSlotNumber { get; set; }

        [MaxLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
        public string? Notes { get; set; }

        // Escort phụ trách con ngựa (R-06): UserId của một người trong Crew có CrewRole = Escort.
        // Bỏ trống = hệ thống tự phân theo ô chuồng (ô 1-3 cho escort đầu tiên, 4-6 cho escort thứ hai...).
        [Range(1, int.MaxValue, ErrorMessage = "EscortUserId must be a valid user ID.")]
        public int? EscortUserId { get; set; }
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

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal? Longitude { get; set; }

        [Range(0, 100000, ErrorMessage = "DistanceKmFromPrevious must be between 0 and 100,000.")]
        public decimal? DistanceKmFromPrevious { get; set; }

        [Required(ErrorMessage = "PlannedTime is required.")]
        public DateTimeOffset? PlannedTime { get; set; }

        [Range(0, 2880, ErrorMessage = "MandatoryRestMinutes must be between 0 and 2880.")]
        public int MandatoryRestMinutes { get; set; }

        // Bắt buộc với mốc PickUp / DropOff: đơn được đón / trả ở mốc này
        [Range(1, int.MaxValue, ErrorMessage = "BookingId must be a valid booking ID.")]
        public int? BookingId { get; set; }

        // Chỉ có tác dụng khi thêm một mốc: vị trí chèn (1 = đầu tiên). Bỏ trống = thêm vào cuối.
        [Range(1, 1000, ErrorMessage = "Position must be 1 or greater.")]
        public int? Position { get; set; }
    }

    public class SuggestRouteRequest
    {
        [Required(ErrorMessage = "BookingHorseIds is required.")]
        [MinLength(1, ErrorMessage = "Choose at least one horse.")]
        public List<int> BookingHorseIds { get; set; } = new();

        [Required(ErrorMessage = "PlannedStartDate is required.")]
        public DateTimeOffset? PlannedStartDate { get; set; }

        [Range(1, 4, ErrorMessage = "DriverCount must be between 1 and 4.")]
        public int DriverCount { get; set; } = 1;
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
