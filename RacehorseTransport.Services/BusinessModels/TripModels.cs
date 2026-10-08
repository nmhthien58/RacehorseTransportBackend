namespace RacehorseTransport.Services.BusinessModels
{
    public class TripFilterOptions : BusinessQueryOptions
    {
        public string? Status { get; set; }
        public DateTimeOffset? StartFrom { get; set; }
        public DateTimeOffset? StartTo { get; set; }
    }

    // Dòng trong danh sách chuyến
    public class TripSummaryModel
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
        public List<TripCrewModel> Crew { get; set; } = new();
    }

    public class TripDetailModel : TripSummaryModel
    {
        public int? VehicleCapacity { get; set; }
        public decimal PlannedCost { get; set; }
        public decimal ActualCost { get; set; }

        // Kế hoạch (Flow 3)
        public int? PlannedByUserId { get; set; }
        public string? PlannedByName { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public int PlanRejectionCount { get; set; }
        public string? PlanRejectionReason { get; set; }

        // Quyết toán khi đóng chuyến (Flow 6)
        public int DelayMinutes { get; set; }
        public string OnTimeStatus { get; set; } = string.Empty;
        public decimal KpiScore { get; set; }
        public string? ClosedByName { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }
        public string? ExecutiveRemarks { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public List<TripHorseModel> Horses { get; set; } = new();
        public List<TripCheckpointModel> Checkpoints { get; set; } = new();
        public List<TripBookingModel> Bookings { get; set; } = new();
    }

    public class TripCrewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string CrewRole { get; set; } = string.Empty; // Driver / Escort
        public bool IsLead { get; set; }
        public string? Duty { get; set; }
    }

    public class TripHorseModel
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
        public string? DossierStatus { get; set; } // chuyến chỉ xuất phát được khi mọi con đều Cleared
    }

    public class TripCheckpointModel
    {
        public int CheckpointId { get; set; }
        public int SequenceOrder { get; set; }
        public string CheckpointName { get; set; } = string.Empty;
        public string CheckpointType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTimeOffset PlannedTime { get; set; }
        public int MandatoryRestMinutes { get; set; }
        public DateTimeOffset? ActualArrivalTime { get; set; }
        public DateTimeOffset? ActualDepartureTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; } // false = mốc đã bị thay thế khi nắn tuyến
    }

    // Một đơn có ngựa trên chuyến này (chuyến có thể ghép nhiều đơn, mỗi đơn bàn giao riêng)
    public class TripBookingModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public int HorseCountOnTrip { get; set; }
        public bool HandoverCompleted { get; set; }
    }
}
