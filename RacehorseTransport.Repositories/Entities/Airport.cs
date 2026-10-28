namespace RacehorseTransport.Repositories.Entities
{
    // common.airports (R-18): sân bay nhận ngựa của từng nước. Đường bay cần cả nước đi và nước đến có sân bay đang mở.
    public class Airport
    {
        public int AirportId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string IataCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public bool IsActive { get; set; } = true;

        public Country? Country { get; set; }
    }
}
