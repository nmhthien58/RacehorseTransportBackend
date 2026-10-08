namespace RacehorseTransport.Services.BusinessModels
{
    public class IncidentFilterOptions : BusinessQueryOptions
    {
        public string? Status { get; set; }
        public string? Severity { get; set; }
        public int? TripId { get; set; }
    }

    // Đoàn xe báo sự cố trên đường
    public class ReportIncidentModel
    {
        public string IncidentType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? AffectedTripHorseId { get; set; } // bỏ trống nếu sự cố không gắn với một con ngựa cụ thể
    }

    // Coordinator đề xuất phương án xử lý
    public class ProposeIncidentPlanModel
    {
        public string ProposedAction { get; set; } = string.Empty;
        public string? RevisedRouteNotes { get; set; }
        public decimal AdditionalCost { get; set; }
    }

    // Nắn tuyến: bỏ các mốc chưa tới và chèn các mốc mới vào lộ trình
    public class ApplyRerouteModel
    {
        // Chèn mốc mới ngay sau mốc này. Bỏ trống = sau mốc gần nhất mà đoàn xe đã tới.
        public int? InsertAfterCheckpointId { get; set; }
        public List<int> DeactivateCheckpointIds { get; set; } = new();
        public List<CheckpointInputModel> NewCheckpoints { get; set; } = new();
    }

    public class IncidentModel
    {
        public int IncidentId { get; set; }
        public string IncidentCode { get; set; } = string.Empty;
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string TripStatus { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? AffectedTripHorseId { get; set; }
        public string? AffectedHorseName { get; set; }
        public int ReportedByUserId { get; set; }
        public string? ReportedByName { get; set; }
        public DateTimeOffset ReportedAt { get; set; }
        public string? ProposedAction { get; set; }
        public string? RevisedRouteNotes { get; set; }
        public decimal AdditionalCost { get; set; }
        public string? RejectionReason { get; set; } // lý do của lần từ chối gần nhất; xóa khi phương án được duyệt
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
    }
}
