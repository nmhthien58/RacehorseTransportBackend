namespace RacehorseTransport.Repositories.Entities
{
    // tracking.horse_welfare_logs
    public class HorseWelfareLog
    {
        public int LogId { get; set; }
        public int TripHorseId { get; set; }
        public int? CheckpointId { get; set; }
        public int RecordedByUserId { get; set; }
        public decimal CabinTemp { get; set; }
        public decimal WaterIntakeLiters { get; set; }
        public string FeedStatus { get; set; } = string.Empty;
        public string StressLevel { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTimeOffset LoggedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual TripHorse? TripHorse { get; set; }
        public virtual RouteCheckpoint? Checkpoint { get; set; }
        public virtual User? RecordedBy { get; set; }
    }
}
