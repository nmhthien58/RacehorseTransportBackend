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

        // Navigation properties
        public virtual Trip? Trip { get; set; }
        public virtual BookingHorse? BookingHorse { get; set; }
        public virtual ICollection<HorseWelfareLog> WelfareLogs { get; set; } = new List<HorseWelfareLog>();
    }
}
