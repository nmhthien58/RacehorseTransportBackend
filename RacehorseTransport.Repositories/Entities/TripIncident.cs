using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // incident.trip_incidents
    public class TripIncident
    {
        public int IncidentId { get; set; }
        public string IncidentCode { get; set; } = string.Empty;
        public int TripId { get; set; }
        public int ReportedByUserId { get; set; }
        public string IncidentType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? ProposedAction { get; set; }
        public string? ResponseAction { get; set; } // IncidentResponseActions
        public string? RouteChange { get; set; }    // JSON thay đổi tuyến đề xuất kèm phương án (IncidentRouteChangeModel)
        public string? RevisedRouteNotes { get; set; }
        public decimal AdditionalCost { get; set; }
        public string? RejectionReason { get; set; } // lý do của lần từ chối phương án gần nhất
        public string Status { get; set; } = IncidentStatuses.Reported;
        public int? ApprovedByUserId { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public DateTimeOffset ReportedAt { get; set; } = DateTimeOffset.UtcNow;
        public string Severity { get; set; } = IncidentSeverities.Moderate;
        public int? AffectedTripHorseId { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }

        // Phương án OffloadHorse (R-12): hướng xử lý tiếp của con ngựa rời chuyến và biên bản thú y (bắt buộc khi Deceased)
        public string? OffloadOutcome { get; set; }
        public string? VetReportPath { get; set; }

        // Tài xế đã dỡ con ngựa ngay lúc báo sự cố (thú y yêu cầu); phương án OffloadHorse vẫn phải được duyệt sau
        public bool EmergencyOffload { get; set; }

        // R-09: nguyên nhân, bắt buộc khi đóng sự cố (FaultParties) + giải thích ngắn
        public string? FaultParty { get; set; }

        // R-14 (BE-14b): đi đường vòng tốn thêm tiền cho khách (tổng, chia cho các đơn theo số ngựa); > 0 thì phải hỏi ý khách
        public decimal ExtraCostForCustomer { get; set; }
        public virtual ICollection<IncidentConsent> Consents { get; set; } = new List<IncidentConsent>();
        public string? FaultNote { get; set; }

        // Navigation properties
        public virtual Trip? Trip { get; set; }
        public virtual User? ReportedBy { get; set; }
        public virtual User? ApprovedBy { get; set; }
        public virtual TripHorse? AffectedTripHorse { get; set; }
    }
}
