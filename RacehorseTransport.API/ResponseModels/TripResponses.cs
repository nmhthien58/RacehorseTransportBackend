namespace RacehorseTransport.API.ResponseModels
{
    public class TripSummaryResponse
    {
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string OverallStatus { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string? VehicleCode { get; set; }
        public string? VehicleType { get; set; }
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
        public DateTimeOffset? ActualStartDate { get; set; }
        public DateTimeOffset? ActualEndDate { get; set; }
        public int HorseCount { get; set; }
        public List<TripCrewResponse> Crew { get; set; } = new();
    }

    public class TripDetailResponse : TripSummaryResponse
    {
        public int? VehicleCapacity { get; set; }
        public int LiveDelayMinutes { get; set; }
        public decimal PlannedCost { get; set; }
        public decimal ActualCost { get; set; }
        public int? PlannedByUserId { get; set; }
        public string? PlannedByName { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public int PlanRejectionCount { get; set; }
        public string? PlanRejectionReason { get; set; }
        public int DelayMinutes { get; set; }
        public string OnTimeStatus { get; set; } = string.Empty;
        public decimal KpiScore { get; set; }
        public string? ClosedByName { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
        public string? ExecutiveRemarks { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<TripHorseResponse> Horses { get; set; } = new();
        public List<TripCheckpointResponse> Checkpoints { get; set; } = new();
        public List<TripBookingResponse> Bookings { get; set; } = new();
    }

    public class TripCrewResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string CrewRole { get; set; } = string.Empty;
        public bool IsLead { get; set; }
        public string? Duty { get; set; }
        public int? AssignedHorseCount { get; set; }
    }

    public class TripHorseResponse
    {
        public int TripHorseId { get; set; }
        public int StallSlotNumber { get; set; }
        public string? Notes { get; set; }
        public int BookingHorseId { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? SpecialCareRequirements { get; set; }
        public string? PhotoUrl { get; set; }
        public string StallClass { get; set; } = string.Empty;
        public string? DossierStatus { get; set; }
        public int? EscortUserId { get; set; }
        public string? EscortName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset? PickedUpAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public DateTimeOffset? OffloadedAt { get; set; }
        public string? OffloadReason { get; set; }
        public string? OffloadOutcome { get; set; } // WaitRecover | ReturnToOrigin | HandOverHere | Deceased (R-12)
        public RacehorseTransport.Services.BusinessModels.VetClinicModel? VetClinic { get; set; } // WaitRecover: phòng khám thú y
        public decimal? VetExpenseAmount { get; set; }
        public string? VetExpenseBearer { get; set; }  // Company | Customer
        public string? VetExpenseNote { get; set; }
    }

    public class TripCheckpointResponse
    {
        public int CheckpointId { get; set; }
        public int SequenceOrder { get; set; }
        public string CheckpointName { get; set; } = string.Empty;
        public string CheckpointType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? DistanceKmFromPrevious { get; set; }
        public DateTimeOffset PlannedTime { get; set; }
        public int MandatoryRestMinutes { get; set; }
        public DateTimeOffset? ActualArrivalTime { get; set; }
        public DateTimeOffset? ActualDepartureTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? BookingId { get; set; }
        public string? BookingCode { get; set; }
        public DateTimeOffset? EstimatedArrivalTime { get; set; }
    }

    public class TripBookingResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public int HorseCountOnTrip { get; set; }
        public bool HandoverCompleted { get; set; }
        public int? PickupCheckpointId { get; set; }
        public int? DropoffCheckpointId { get; set; }
        public DateTimeOffset? EstimatedDeliveryAt { get; set; }
        public DateTimeOffset? LiveEstimatedDeliveryAt { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public int DeliveredHorseCount { get; set; }
        public int OffloadedHorseCount { get; set; }
    }

    // ---------- Gợi ý lộ trình (R-04) ----------

    public class RouteSuggestionResponse
    {
        public string TransportMode { get; set; } = string.Empty;
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset SuggestedEndDate { get; set; }
        public int TotalDistanceKm { get; set; }
        public decimal TotalHours { get; set; }
        public List<SuggestedCheckpointResponse> Checkpoints { get; set; } = new();
        public List<RouteBookingEtaResponse> Bookings { get; set; } = new();
        public List<PlanWarningResponse> Warnings { get; set; } = new();
    }

    public class SuggestedCheckpointResponse
    {
        public string CheckpointName { get; set; } = string.Empty;
        public string CheckpointType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateTimeOffset PlannedTime { get; set; }
        public int MandatoryRestMinutes { get; set; }
        public int? BookingId { get; set; }
        public string? BookingCode { get; set; }
        public int LegDistanceKm { get; set; }
    }

    public class RouteBookingEtaResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTimeOffset PickupAt { get; set; }
        public DateTimeOffset EstimatedDeliveryAt { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public bool OnTime { get; set; }
        public int RouteDistanceKm { get; set; }
        public int? DirectDistanceKm { get; set; }
        public decimal? DetourRatio { get; set; }
    }

    public class PlanWarningResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? BookingCode { get; set; }
        public Dictionary<string, string> Params { get; set; } = new();
    }
}
