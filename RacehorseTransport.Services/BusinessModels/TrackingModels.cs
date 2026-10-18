namespace RacehorseTransport.Services.BusinessModels
{
    // ---------- Bảng điều khiển của Coordinator: mỗi chuyến đang chạy một dòng ----------
    public class TripProgressModel
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

        // Tiến độ theo mốc (chỉ tính các mốc còn hiệu lực)
        public int CheckpointsTotal { get; set; }
        public int CheckpointsReached { get; set; }
        public string? LastCheckpointName { get; set; }
        public string? LastCheckpointStatus { get; set; }
        public string? NextCheckpointName { get; set; }
        public DateTimeOffset? NextCheckpointPlannedTime { get; set; }
        public DateTimeOffset? NextCheckpointEstimatedTime { get; set; } // cập nhật theo check-in thực tế (R-08)
        public DateTimeOffset? EstimatedEndAt { get; set; }             // giờ tới mốc cuối dự kiến theo check-in thực tế

        // Trễ so với kế hoạch tại mốc gần nhất đã tới (phút)
        public int DelayMinutes { get; set; }
        public bool IsBehindSchedule { get; set; }

        public int OpenIncidentCount { get; set; }
        public int WelfareAlertCount { get; set; }
    }

    // ---------- Màn hình theo dõi của khách: một đơn, các chuyến đang chở ngựa của đơn ----------
    public class BookingTrackingModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public List<TrackingTripModel> Trips { get; set; } = new();

        // Ngựa của đơn chưa được xếp lên chuyến nào
        public List<string> HorsesAwaitingPlanning { get; set; } = new();
        public DateTimeOffset GeneratedAt { get; set; }
    }

    public class TrackingTripModel
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

        // Mốc đón / trả của đơn này trên chuyến (R-04) và giờ dự kiến tới mốc trả
        public int? PickupCheckpointId { get; set; }
        public int? DropoffCheckpointId { get; set; }
        public DateTimeOffset? EstimatedDeliveryAt { get; set; }      // giờ tới mốc trả theo kế hoạch

        // Cập nhật theo check-in thực tế (R-08): trễ ở mốc trước thì các mốc sau lùi theo
        public DateTimeOffset? LiveEstimatedDeliveryAt { get; set; }
        public bool DeliveryAtRisk { get; set; }                       // dự kiến tới mốc trả sau hạn giao của đơn
        public List<TrackingCheckpointModel> Checkpoints { get; set; } = new();
        public List<TrackingHorseModel> Horses { get; set; } = new();
    }

    public class TrackingCheckpointModel
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
        public int? DelayMinutes { get; set; } // null = chưa tới mốc này
        public DateTimeOffset EstimatedArrivalTime { get; set; } // giờ tới thực tế, hoặc giờ dự kiến đã cộng độ trễ hiện tại

        // Điểm đón / trả của khách khác đi chung chuyến: ẩn tên và địa chỉ (CheckpointName / Address để trống)
        public bool IsOtherBookingStop { get; set; }
        public bool IsMyPickup { get; set; }
        public bool IsMyDropoff { get; set; }
    }

    public class TrackingHorseModel
    {
        public int TripHorseId { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public int StallSlotNumber { get; set; }
        public string Status { get; set; } = string.Empty;   // trạng thái của con ngựa trên chuyến (R-05)
        public DateTimeOffset? PickedUpAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public DateTimeOffset? OffloadedAt { get; set; }
        public string? OffloadReason { get; set; }
        public WelfareLogModel? LatestLog { get; set; }
        public List<WelfareLogModel> Logs { get; set; } = new();
    }
}
