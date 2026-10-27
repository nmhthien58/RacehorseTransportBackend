namespace RacehorseTransport.Services.Options
{
    // BE-01 / R-11: dịch vụ bản đồ. Không dùng Google Maps (Việt Nam, Trung Quốc thuộc vùng cấm của Google Maps Platform).
    // Trong Việt Nam dùng Goong (khóa ở Goong:ApiKey); ngoài Việt Nam dùng Nominatim (tìm địa chỉ) + OSRM (chỉ đường).
    public class GeoOptions
    {
        public const string SectionName = "Geo";

        public string GoongUrl { get; set; } = "https://rsapi.goong.io";
        public string NominatimUrl { get; set; } = "https://nominatim.openstreetmap.org";
        public string OsrmUrl { get; set; } = "https://router.project-osrm.org";

        // Nominatim bắt buộc User-Agent riêng và tối đa 1 request / giây (chính sách sử dụng của OpenStreetMap)
        public string UserAgent { get; set; } = "RacehorseTransport-SWP391/1.0 (+https://racehorsetransportbackend-production.up.railway.app)";
        public int NominatimMinIntervalMs { get; set; } = 1100;

        public int TimeoutSeconds { get; set; } = 10;
        public int CacheDays { get; set; } = 7;
        public int MaxSearchResults { get; set; } = 5;

        // Dự phòng khi nhà cung cấp lỗi: quãng đường đường bộ ≈ đường chim bay × hệ số này
        public decimal FallbackRoadFactor { get; set; } = 1.3m;

        // Tìm các nước đi qua: hai đầu một đoạn cùng nước và đoạn ngắn hơn TrustSegmentKm thì coi như không qua nước nào khác;
        // không chia đoạn nhỏ hơn MinSegmentKm; tối đa MaxCountryLookups lần tra quốc gia cho một tuyến
        public decimal CountryTrustSegmentKm { get; set; } = 300;
        public decimal CountryMinSegmentKm { get; set; } = 20;
        public int MaxCountryLookups { get; set; } = 60;

        // Số điểm tối đa của đường vẽ trả về cho frontend (đường đầy đủ của OSRM có thể rất dài)
        public int MaxPolylinePoints { get; set; } = 2000;
    }
}
