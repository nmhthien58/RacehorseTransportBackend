using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // BE-01: dịch vụ bản đồ. Nơi gọi chỉ biết interface này; đổi / thêm nhà cung cấp (vd Mapbox) không phải sửa nơi gọi.
    public interface IGeoService
    {
        // Gợi ý địa chỉ khi gõ. countryCode (ISO 2 chữ) để trống = tìm khắp nơi
        Task<IReadOnlyList<GeoPlaceModel>> SearchAsync(string query, string? countryCode, CancellationToken cancellationToken = default);

        // Tọa độ -> địa chỉ + mã nước; null nếu không có địa chỉ (giữa biển...)
        Task<GeoPlaceModel?> ReverseAsync(double lat, double lng, CancellationToken cancellationToken = default);

        // Chỉ đường qua các điểm theo thứ tự: từng chặng + tổng + đường vẽ. Nhà cung cấp lỗi thì chặng đó là ước tính.
        Task<GeoRouteModel> RouteAsync(IReadOnlyList<GeoPointModel> points, bool includeCountries = false, CancellationToken cancellationToken = default);

        // Mã các nước tuyến đi qua theo thứ tự (gộp trùng liên tiếp), dựa trên đường vẽ của tuyến
        Task<IReadOnlyList<string>> CountriesAlongAsync(IReadOnlyList<GeoPointModel> polyline, CancellationToken cancellationToken = default);
    }

    // Nhà cung cấp tìm địa chỉ / tọa độ -> địa chỉ (Goong, Nominatim...)
    public interface IGeocodingProvider
    {
        string Name { get; }

        // Nhà cung cấp phục vụ được vùng này không (countryCode null = không biết trước nước)
        bool Supports(string? countryCode);

        // null = gọi lỗi (để chuyển sang nhà cung cấp khác); danh sách rỗng = không có kết quả
        Task<IReadOnlyList<GeoPlaceModel>?> SearchAsync(string query, string? countryCode, int limit, CancellationToken cancellationToken);

        // null = gọi lỗi hoặc không có địa chỉ
        Task<GeoPlaceModel?> ReverseAsync(double lat, double lng, CancellationToken cancellationToken);

        // Chỉ lấy mã nước (dò nước dọc tuyến). Mặc định dùng ReverseAsync; nhà cung cấp có thể gọi bản rẻ hơn.
        async Task<string?> CountryCodeAsync(double lat, double lng, CancellationToken cancellationToken) =>
            (await ReverseAsync(lat, lng, cancellationToken))?.CountryCode;
    }

    // Nhà cung cấp chỉ đường (Goong, OSRM...)
    public interface IRoutingProvider
    {
        string Name { get; }

        // Phục vụ được chặng giữa hai nước này không
        bool Supports(string? fromCountryCode, string? toCountryCode);

        // null = gọi lỗi hoặc không có đường
        Task<GeoRouteLegModel?> RouteLegAsync(GeoPointModel from, GeoPointModel to, CancellationToken cancellationToken);
    }
}
