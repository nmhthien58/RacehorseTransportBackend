using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // booking.booking_horses
    public class BookingHorse
    {
        public int BookingHorseId { get; set; }
        public int BookingId { get; set; }
        public int HorseId { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = BookingHorseStatuses.Pending;
        public string StallClass { get; set; } = StallClasses.Shared;

        // Navigation properties
        public virtual BookingRequest? Booking { get; set; }
        public virtual Horse? Horse { get; set; }
        public virtual DigitalDossier? Dossier { get; set; }
        public virtual ICollection<TripHorse> TripHorses { get; set; } = new List<TripHorse>();
    }
}
