using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // fleet.route_checkpoints
    public class RouteCheckpoint
    {
        public int CheckpointId { get; set; }
        public int TripId { get; set; }
        public int SequenceOrder { get; set; }
        public string CheckpointName { get; set; } = string.Empty;
        public string CheckpointType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTimeOffset PlannedTime { get; set; }
        public int MandatoryRestMinutes { get; set; }
        public DateTimeOffset? ActualArrivalTime { get; set; }
        public DateTimeOffset? ActualDepartureTime { get; set; }
        public string Status { get; set; } = CheckpointStatuses.Pending;
        public bool IsActive { get; set; } = true; // false = mốc đã bị nắn tuyến

        // Navigation properties
        public virtual Trip? Trip { get; set; }
    }
}
