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
        // Báo giá chỉ cần biết đi qua nước nào nên giữ ít lượt tra (Nominatim 1 request / giây)
        public decimal CountryMinSegmentKm { get; set; } = 50;
        public int MaxCountryLookups { get; set; } = 12;

        // Hết thời gian này thì dừng dò nước, trả các nước đã biết (ít nhất nước đi, nước đến)
        public int CountryLookupBudgetSeconds { get; set; } = 8;

        // Mã nước được cache theo ô lưới CountryGridDegrees độ, giữ CountryCacheDays ngày (biên giới hầu như không đổi)
        public double CountryGridDegrees { get; set; } = 0.5;
        public int CountryCacheDays { get; set; } = 90;

        // Số điểm tối đa của đường vẽ trả về cho frontend (đường đầy đủ của OSRM có thể rất dài)
        public int MaxPolylinePoints { get; set; } = 2000;
    }
}
