using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // fleet.trip_horses
    public class TripHorse
    {
        public int TripHorseId { get; set; }
        public int TripId { get; set; }
        public int BookingHorseId { get; set; }
        public int StallSlotNumber { get; set; }
        public string? Notes { get; set; }

        // Escort phụ trách con ngựa (R-06): phải là Escort trong đoàn của chính chuyến, mỗi escort tối đa 3 con
        public int? EscortUserId { get; set; }

        // Trạng thái riêng của con ngựa trên chuyến (R-05); trạng thái chuyến là trạng thái của xe
        public string Status { get; set; } = TripHorseStatuses.Planned;
        public DateTimeOffset? PickedUpAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public int? HandoverId { get; set; }
        public DateTimeOffset? OffloadedAt { get; set; }
        public string? OffloadReason { get; set; }

        // Navigation properties
        public virtual Trip? Trip { get; set; }
        public virtual BookingHorse? BookingHorse { get; set; }
        public virtual HandoverAcceptance? Handover { get; set; }
        public virtual User? Escort { get; set; }
        public virtual ICollection<HorseWelfareLog> WelfareLogs { get; set; } = new List<HorseWelfareLog>();
    }
}
