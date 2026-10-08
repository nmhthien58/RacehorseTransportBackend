using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // fleet.trips
    // Lưu ý: trips KHÔNG có BookingId / DriverId / EscortId.
    // Đơn - Chuyến là nhiều-nhiều qua trip_horses; nhân sự của chuyến nằm ở trip_crew.
    public class Trip
    {
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
        public DateTimeOffset? ActualStartDate { get; set; }
        public DateTimeOffset? ActualEndDate { get; set; }
        public decimal PlannedCost { get; set; }
        public decimal ActualCost { get; set; }
        public string OverallStatus { get; set; } = TripStatuses.Draft;

        // Quyết toán KPI khi đóng chuyến (Flow 6, xem docs/TASKS.md, T-26)
        public int DelayMinutes { get; set; }
        public string OnTimeStatus { get; set; } = OnTimeStatuses.OnTime;
        public decimal KpiScore { get; set; } = 100m;
        public int? ClosedByUserId { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
        public string? ExecutiveRemarks { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Duyệt kế hoạch (Flow 3)
        public int? PlannedByUserId { get; set; }
        public int? ApprovedByUserId { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public int PlanRejectionCount { get; set; }
        public string? PlanRejectionReason { get; set; }

        // Navigation properties
        public virtual TransportAsset? Vehicle { get; set; }
        public virtual User? PlannedBy { get; set; }
        public virtual User? ApprovedBy { get; set; }
        public virtual User? ClosedBy { get; set; }
        public virtual ICollection<HandoverAcceptance> Handovers { get; set; } = new List<HandoverAcceptance>();
        public virtual ICollection<TripCrew> Crew { get; set; } = new List<TripCrew>();
        public virtual ICollection<TripHorse> TripHorses { get; set; } = new List<TripHorse>();
        public virtual ICollection<RouteCheckpoint> Checkpoints { get; set; } = new List<RouteCheckpoint>();
        public virtual ICollection<TripIncident> Incidents { get; set; } = new List<TripIncident>();
    }
}
