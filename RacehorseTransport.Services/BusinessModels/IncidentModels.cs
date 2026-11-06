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

        // Thú y bắt buộc dỡ ngay: con ngựa AffectedTripHorseId rời xe lúc báo, phương án OffloadHorse duyệt sau (R-12)
        public bool EmergencyOffload { get; set; }
    }

    // Coordinator đề xuất phương án xử lý
    public class ProposeIncidentPlanModel
    {
        public string ProposedAction { get; set; } = string.Empty;
        public string ResponseAction { get; set; } = string.Empty;
        public int? ReplacementVehicleId { get; set; }
        public int? RemoveUserId { get; set; }
        public int? AddUserId { get; set; }
        public string? CrewRole { get; set; }

        // Thay đổi tuyến đi kèm phương án (bỏ trống = giữ nguyên lộ trình). Áp vào lộ trình khi Manager duyệt.
        public ApplyRerouteModel? RouteChange { get; set; }
        public string? RevisedRouteNotes { get; set; }
        public decimal AdditionalCost { get; set; }

        // R-14 (BE-14b): đi đường vòng tốn thêm tiền cho khách (tổng, chia theo số ngựa). > 0 (chỉ với Reroute) thì sau khi
        // Manager duyệt phải chờ mọi khách có ngựa trên xe đồng ý; một khách từ chối -> phương án chuyển ReturnToOrigin
        public decimal ExtraCostForCustomer { get; set; }

        // OffloadHorse (R-12): con ngựa rời chuyến (bỏ trống = con ngựa đã chọn lúc báo sự cố), hướng xử lý tiếp
        // và đường dẫn biên bản thú y đã tải lên qua POST /api/files (bắt buộc khi OffloadOutcome = Deceased)
        public int? AffectedTripHorseId { get; set; }
        public string? OffloadOutcome { get; set; }
        public string? VetReportPath { get; set; }

        // OffloadHorse + Deceased khi ngựa còn sống: chờ chủ ngựa đồng ý an tử sau khi Manager duyệt
        public bool RequiresEuthanasiaConsent { get; set; }
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
        public string? InitialSeverity { get; set; }
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
        public IncidentActionPayloadModel? ActionPayload { get; set; }
        public IncidentRouteChangeModel? RouteChange { get; set; }
        public string? RevisedRouteNotes { get; set; }
        public decimal AdditionalCost { get; set; }
        public string? RejectionReason { get; set; } // lý do của lần từ chối gần nhất; xóa khi phương án được duyệt
        public string? FaultParty { get; set; }      // nguyên nhân chọn khi đóng sự cố (R-09)
        public string? FaultNote { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
        public bool EmergencyOffload { get; set; }
        public string? OffloadOutcome { get; set; }
        public string? VetReportPath { get; set; }
        public string? VetReportUrl { get; set; } // link xem biên bản thú y (có hạn)

        // R-14 (BE-14b)
        public decimal ExtraCostForCustomer { get; set; }
        public List<IncidentConsentModel> Consents { get; set; } = new();
        public List<IncidentDocumentModel> Documents { get; set; } = new();

        // An tử: chờ chủ ngựa đồng ý; quá ConsentDeadline thì được ủy quyền khẩn cấp
        public bool RequiresEuthanasiaConsent { get; set; }
        public string? PendingConsentType { get; set; }
        public DateTimeOffset? ConsentRequestedAt { get; set; }
        public DateTimeOffset? ConsentDeadline { get; set; }
        public DateTimeOffset? EmergencyAuthorizedAt { get; set; }
        public string? EmergencyAuthorizedByName { get; set; }
        public string? EmergencyAuthorizationNote { get; set; }
    }

    public class IncidentDocumentModel
    {
        public int IncidentDocumentId { get; set; }
        public int IncidentId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string? Note { get; set; }
        public int UploadedByUserId { get; set; }
        public string? UploadedByName { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }

    public class UploadIncidentDocumentModel
    {
        public string DocumentType { get; set; } = string.Empty;
        public string? Note { get; set; }
        public FileUploadModel File { get; set; } = null!;
    }

    public class IncidentActionPayloadModel
    {
        public int? ReplacementVehicleId { get; set; }
        public int? RemoveUserId { get; set; }
        public int? AddUserId { get; set; }
        public string? CrewRole { get; set; }
    }

    public class IncidentConsentModel
    {
        public string ConsentType { get; set; } = string.Empty; // Detour | Euthanasia
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public bool Accepted { get; set; }
        public DateTimeOffset DecidedAt { get; set; }
    }

    // Thay đổi tuyến của một phương án, lưu dạng JSON ở trip_incidents.route_change.
    // Lưu kèm tên mốc để vẫn đọc được sau khi lộ trình đổi; AppliedAt có giá trị khi đã áp vào lộ trình.
    public class IncidentRouteChangeModel
    {
        public int? InsertAfterCheckpointId { get; set; }
        public string? InsertAfterName { get; set; }
        public List<RouteChangeRemovedCheckpointModel> RemovedCheckpoints { get; set; } = new();
        public List<RouteChangeNewCheckpointModel> NewCheckpoints { get; set; } = new();
        public DateTimeOffset? AppliedAt { get; set; }

        // R-14 (BE-14b) ReturnToOrigin: tỉ lệ quãng đường chưa đi của từng đơn lúc quay đầu (theo BookingId), dùng để hoàn tiền khi đóng sự cố
        public Dictionary<int, decimal>? ReturnUnusedRatios { get; set; }
        public int? TurnAroundCheckpointId { get; set; }
    }

    public class RouteChangeRemovedCheckpointModel
    {
        public int CheckpointId { get; set; }
        public string CheckpointName { get; set; } = string.Empty;
        public DateTimeOffset PlannedTime { get; set; }
    }

    public class RouteChangeNewCheckpointModel
    {
        public int? CheckpointId { get; set; } // có sau khi đã áp vào lộ trình
        public string CheckpointName { get; set; } = string.Empty;
        public string CheckpointType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTimeOffset PlannedTime { get; set; }
        public int MandatoryRestMinutes { get; set; }
        public decimal? Latitude { get; set; }   // để vẽ bản đồ và tính quãng đường khi áp tuyến
        public decimal? Longitude { get; set; }
    }
}
