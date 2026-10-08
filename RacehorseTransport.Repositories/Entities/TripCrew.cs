namespace RacehorseTransport.Repositories.Entities
{
    // fleet.trip_crew
    public class TripCrew
    {
        public int TripCrewId { get; set; }
        public int TripId { get; set; }
        public int UserId { get; set; }
        public string CrewRole { get; set; } = string.Empty;
        public bool IsLead { get; set; }
        public string? Duty { get; set; }

        // Navigation properties
        public virtual Trip? Trip { get; set; }
        public virtual User? User { get; set; }
    }
}
