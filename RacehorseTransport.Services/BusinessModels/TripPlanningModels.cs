namespace RacehorseTransport.Services.BusinessModels
{
    // Một con ngựa thuộc đơn đã duyệt nhưng chưa được xếp lên chuyến nào
    public class UnplannedHorseModel
    {
        public int BookingHorseId { get; set; }
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string TransportMode { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string PickupCountryCode { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public List<string> TransitCountryCodes { get; set; } = new(); // nước quá cảnh: mỗi nước cần mốc cửa khẩu
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public bool RequiresClimateControl { get; set; }
        public int HorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string? SpecialCareRequirements { get; set; }
        public string StallClass { get; set; } = string.Empty;
        public string? DossierStatus { get; set; }
    }

    // Kế hoạch chuyến do Coordinator lập (dùng cho cả tạo mới và sửa bản nháp)
    public class TripPlanModel
    {
        public int VehicleId { get; set; }
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
        public decimal PlannedCost { get; set; }
        public List<TripCrewInputModel> Crew { get; set; } = new();
        public List<TripHorseInputModel> Horses { get; set; } = new();

        // null khi sửa = giữ nguyên các mốc hiện có; có giá trị = thay toàn bộ
        public List<CheckpointInputModel>? Checkpoints { get; set; }
    }

    public class TripCrewInputModel
    {
        public int UserId { get; set; }
        public string CrewRole { get; set; } = string.Empty;
        public bool IsLead { get; set; }
        public string? Duty { get; set; }
    }

    public class TripHorseInputModel
    {
        public int BookingHorseId { get; set; }
        public int StallSlotNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class CheckpointInputModel
    {
        public string CheckpointName { get; set; } = string.Empty;
        public string CheckpointType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTimeOffset PlannedTime { get; set; }
        public int MandatoryRestMinutes { get; set; }

        // Bắt buộc với mốc PickUp / DropOff: đơn được đón / trả ở mốc này; để trống với các loại mốc khác
        public int? BookingId { get; set; }

        // Chỉ dùng khi thêm một mốc: vị trí chèn (1 = đầu tiên). Bỏ trống = thêm vào cuối.
        public int? Position { get; set; }
    }

    public class AvailableVehicleModel
    {
        public int AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public int CapacityHorses { get; set; }
        public bool HasClimateControl { get; set; }
        public bool HasGpsTracker { get; set; }
        public DateTimeOffset? LastSanitizationDate { get; set; }
    }
}
