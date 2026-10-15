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
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual User? Owner { get; set; }
        public virtual ICollection<BookingHorse> BookingHorses { get; set; } = new List<BookingHorse>();
        public virtual ICollection<HorseDocument> Documents { get; set; } = new List<HorseDocument>();
    }
}
