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

        // R-12 / R-13: giá trị từng con. Khách khai báo (kèm chứng từ khi từ ngưỡng Insurance.ProofThresholdUsd),
        // Manager chốt giá trị thỏa thuận (mặc định = giá trị khai báo); bảo hiểm và bồi thường tính trên giá trị thỏa thuận.
        public decimal? DeclaredValue { get; set; }
        public decimal? AgreedValue { get; set; }
        public string? ValueProofPath { get; set; }
        public string InsurancePlan { get; set; } = InsurancePlans.None;

        // Navigation properties
        public virtual BookingRequest? Booking { get; set; }
        public virtual Horse? Horse { get; set; }
        public virtual DigitalDossier? Dossier { get; set; }
        public virtual ICollection<TripHorse> TripHorses { get; set; } = new List<TripHorse>();
    }
}
