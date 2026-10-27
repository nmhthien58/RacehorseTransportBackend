namespace RacehorseTransport.Repositories.Entities
{
    // racehorse.horses
    public class Horse
    {
        public int HorseId { get; set; }
        public int OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public string Breed { get; set; } = "Thoroughbred";
        public string Gender { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Color { get; set; } = string.Empty;
        public string? SpecialCareRequirements { get; set; }
        public string? PhotoUrl { get; set; }

        // R-12: tình trạng sinh sản, dùng để chặn chở ngựa cái mang thai gần ngày đẻ hoặc vừa đẻ
        public bool IsPregnant { get; set; }                  // chỉ ngựa cái (Mare)
        public DateOnly? ExpectedFoalingDate { get; set; }    // bắt buộc khi IsPregnant
        public DateOnly? LastFoalingDate { get; set; }        // lần đẻ gần nhất
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual User? Owner { get; set; }
        public virtual ICollection<BookingHorse> BookingHorses { get; set; } = new List<BookingHorse>();
        public virtual ICollection<HorseDocument> Documents { get; set; } = new List<HorseDocument>();
    }
}
