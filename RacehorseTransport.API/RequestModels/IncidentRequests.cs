using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class IncidentQueryRequest : QueryOptionsRequest
    {
        [FromQuery(Name = "status")]
        public string? Status { get; set; }

        [FromQuery(Name = "severity")]
        public string? Severity { get; set; }

        [FromQuery(Name = "tripId")]
        public int? TripId { get; set; }
    }

    public class ReportIncidentRequest
    {
        [Required(ErrorMessage = "IncidentType is required.")]
        public string IncidentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Severity is required.")]
        public string Severity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [MaxLength(255, ErrorMessage = "Location cannot exceed 255 characters.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string Description { get; set; } = string.Empty;

        // Bỏ trống nếu sự cố không gắn với một con ngựa cụ thể
        public int? AffectedTripHorseId { get; set; }
    }

    public class ProposeIncidentPlanRequest
    {
        [Required(ErrorMessage = "ProposedAction is required.")]
        [MaxLength(255, ErrorMessage = "ProposedAction cannot exceed 255 characters.")]
        public string ProposedAction { get; set; } = string.Empty;

        // WaitAndContinue | RepairAndContinue | Reroute | VeterinaryCare | OffloadHorse | Other
        [Required(ErrorMessage = "ResponseAction is required.")]
        public string ResponseAction { get; set; } = string.Empty;

        // Thay đổi tuyến đi kèm (bắt buộc với Reroute). Được kiểm tra ngay và áp vào lộ trình khi Manager duyệt.
        public ApplyRerouteRequest? RouteChange { get; set; }

        [MaxLength(2000, ErrorMessage = "RevisedRouteNotes cannot exceed 2000 characters.")]
        public string? RevisedRouteNotes { get; set; }

        [Range(0, 1000000000, ErrorMessage = "AdditionalCost must be between 0 and 1,000,000,000.")]
        public decimal AdditionalCost { get; set; }
    }

    public class RejectIncidentPlanRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ApplyRerouteRequest
    {
        // Chèn mốc mới ngay sau mốc này. Bỏ trống = sau mốc gần nhất mà đoàn xe đã tới.
        public int? InsertAfterCheckpointId { get; set; }

        // Các mốc chưa tới cần bỏ khỏi lộ trình
        public List<int> DeactivateCheckpointIds { get; set; } = new();

        // Các mốc mới theo đúng thứ tự đi (Position bị bỏ qua)
        public List<CheckpointRequest> NewCheckpoints { get; set; } = new();
    }
}
