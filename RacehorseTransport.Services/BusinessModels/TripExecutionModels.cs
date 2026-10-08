namespace RacehorseTransport.Services.BusinessModels
{
    // Một lần ghi nhật ký thể trạng của một con ngựa trên chuyến
    public class CreateWelfareLogModel
    {
        public int TripHorseId { get; set; }
        public int? CheckpointId { get; set; } // ghi tại mốc nào (bỏ trống nếu ghi dọc đường)
        public decimal CabinTemp { get; set; }
        public decimal WaterIntakeLiters { get; set; }
        public string FeedStatus { get; set; } = string.Empty;
        public string StressLevel { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class WelfareLogModel
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
        public bool NeedsAttention { get; set; } // bỏ ăn hoặc kích động
        public string? Notes { get; set; }
        public string? PhotoUrl { get; set; }
        public int RecordedByUserId { get; set; }
        public string? RecordedByName { get; set; }
        public DateTimeOffset LoggedAt { get; set; }
    }
}
