namespace RacehorseTransport.API.ResponseModels
{
    public class IncidentResponse
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
        public string? ResponseAction { get; set; }
        public IncidentRouteChangeResponse? RouteChange { get; set; } // null = phương án không đổi tuyến
        public string? RevisedRouteNotes { get; set; }
        public decimal AdditionalCost { get; set; }
        public string? RejectionReason { get; set; }
        public string? FaultParty { get; set; }   // Company | Customer | ForceMajeure, có khi sự cố đã đóng
        public string? FaultNote { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
        public bool EmergencyOffload { get; set; }        // tài xế đã dỡ con ngựa ngay lúc báo sự cố
        public string? OffloadOutcome { get; set; }       // hướng xử lý tiếp của phương án OffloadHorse
        public string? VetReportUrl { get; set; }         // link xem biên bản thú y (có hạn)
    }

    // Thay đổi tuyến của phương án. AppliedAt có giá trị khi tuyến đã được áp vào lộ trình (lúc Manager duyệt).
    public class IncidentRouteChangeResponse
    {
        public int? InsertAfterCheckpointId { get; set; }
        public string? InsertAfterName { get; set; }
        public List<RouteChangeRemovedCheckpointResponse> RemovedCheckpoints { get; set; } = new();
        public List<RouteChangeNewCheckpointResponse> NewCheckpoints { get; set; } = new();
        public DateTimeOffset? AppliedAt { get; set; }
    }

    public class RouteChangeRemovedCheckpointResponse
    {
        public int CheckpointId { get; set; }
        public string CheckpointName { get; set; } = string.Empty;
        public DateTimeOffset PlannedTime { get; set; }
    }

    public class RouteChangeNewCheckpointResponse
    {
        public int? CheckpointId { get; set; }
        public string CheckpointName { get; set; } = string.Empty;
        public string CheckpointType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTimeOffset PlannedTime { get; set; }
        public int MandatoryRestMinutes { get; set; }
    }
}
