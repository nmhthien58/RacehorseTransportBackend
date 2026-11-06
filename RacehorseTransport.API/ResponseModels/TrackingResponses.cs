namespace RacehorseTransport.API.ResponseModels
{
    public class WelfareLogResponse
    {
        public int LogId { get; set; }
        public int TripHorseId { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public int? CheckpointId { get; set; }
        public string? CheckpointName { get; set; }
        public decimal CabinTemp { get; set; }
        public decimal WaterIntakeLiters { get; set; }
        public string FeedStatus { get; set; } = string.Empty;
        public string StressLevel { get; set; } = string.Empty;
        public bool NeedsAttention { get; set; }
        public string? Notes { get; set; }
        public string? PhotoUrl { get; set; }
        public int RecordedByUserId { get; set; }
        public string? RecordedByName { get; set; }
        public DateTimeOffset LoggedAt { get; set; }
    }

    public class TripProgressResponse
    {
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string OverallStatus { get; set; } = string.Empty;
        public string? VehicleCode { get; set; }
        public string? LeadDriverName { get; set; }
        public string? LeadDriverPhone { get; set; }
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
        public DateTimeOffset? ActualStartDate { get; set; }
        public DateTimeOffset? ActualEndDate { get; set; }
        public int HorseCount { get; set; }
        public int CheckpointsTotal { get; set; }
        public int CheckpointsReached { get; set; }
        public string? LastCheckpointName { get; set; }
        public string? LastCheckpointStatus { get; set; }
        public string? NextCheckpointName { get; set; }
        public DateTimeOffset? NextCheckpointPlannedTime { get; set; }
        public DateTimeOffset? NextCheckpointEstimatedTime { get; set; }
        public DateTimeOffset? EstimatedEndAt { get; set; }
        public int DelayMinutes { get; set; }
        public bool IsBehindSchedule { get; set; }
        public int OpenIncidentCount { get; set; }
        public int WelfareAlertCount { get; set; }
    }

    public class BookingTrackingResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public List<TrackingTripResponse> Trips { get; set; } = new();
        public List<string> HorsesAwaitingPlanning { get; set; } = new();
        public DateTimeOffset GeneratedAt { get; set; }
    }

    public class TrackingTripResponse
    {
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string OverallStatus { get; set; } = string.Empty;
        public string? VehicleCode { get; set; }
        public string? LeadDriverName { get; set; }
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
        public DateTimeOffset? ActualStartDate { get; set; }
        public DateTimeOffset? ActualEndDate { get; set; }
        public int DelayMinutes { get; set; }
        public bool HandoverCompleted { get; set; }
        public int? PickupCheckpointId { get; set; }
        public int? DropoffCheckpointId { get; set; }
        public DateTimeOffset? EstimatedDeliveryAt { get; set; }
        public DateTimeOffset? LiveEstimatedDeliveryAt { get; set; }
        public bool DeliveryAtRisk { get; set; }
        public List<TrackingCheckpointResponse> Checkpoints { get; set; } = new();
        public List<TrackingHorseResponse> Horses { get; set; } = new();
    }

    public class TrackingCheckpointResponse
    {
        public int CheckpointId { get; set; }
        public int SequenceOrder { get; set; }
        public string CheckpointName { get; set; } = string.Empty;
        public string CheckpointType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTimeOffset PlannedTime { get; set; }
        public DateTimeOffset? ActualArrivalTime { get; set; }
        public DateTimeOffset? ActualDepartureTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? DelayMinutes { get; set; }
        public DateTimeOffset EstimatedArrivalTime { get; set; }
        public bool IsOtherBookingStop { get; set; }
        public bool IsMyPickup { get; set; }
        public bool IsMyDropoff { get; set; }
    }

    public class TrackingHorseResponse
    {
        public int TripHorseId { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public int StallSlotNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset? PickedUpAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public DateTimeOffset? OffloadedAt { get; set; }
        public string? OffloadReason { get; set; }
        public string? OffloadOutcome { get; set; }
        public RacehorseTransport.Services.BusinessModels.VetClinicModel? VetClinic { get; set; }
        public decimal? VetExpenseAmount { get; set; }
        public string? VetExpenseBearer { get; set; }
        public WelfareLogResponse? LatestLog { get; set; }
        public List<WelfareLogResponse> Logs { get; set; } = new();
    }

    // BE-10: GET /api/trips/{id}/departure-check
    public class DepartureCheckResponse
    {
        public bool IsReady { get; set; }
        public DateTimeOffset DepartureAt { get; set; }
        public List<DepartureIssueResponse> Issues { get; set; } = new();
    }

    public class DepartureIssueResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? HorseName { get; set; }
        public string? BookingCode { get; set; }
    }
}
