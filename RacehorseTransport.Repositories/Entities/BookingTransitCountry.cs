namespace RacehorseTransport.Repositories.Entities
{
    // booking.booking_transit_countries: các nước ngựa đi qua (quá cảnh) theo thứ tự, giữa nước đi và nước đến.
    // Đường bay thẳng thì không có dòng nào.
    public class BookingTransitCountry
    {
        public int BookingId { get; set; }
        public int SequenceOrder { get; set; }
        public string CountryCode { get; set; } = string.Empty;

        // Navigation properties
        public virtual BookingRequest? Booking { get; set; }
    }
}
