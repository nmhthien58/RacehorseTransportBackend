namespace RacehorseTransport.API.ResponseModels
{
    public class HorseResponse
    {
        public int HorseId { get; set; }
        public int OwnerUserId { get; set; }
        public string? OwnerName { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Color { get; set; } = string.Empty;
        public string? SpecialCareRequirements { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
