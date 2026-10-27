using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Goong (bản đồ Việt Nam): tìm địa chỉ, tọa độ -> địa chỉ, chỉ đường cho xe tải. Chỉ dùng cho điểm trong Việt Nam.
    public class GoongGeoProvider : IGeocodingProvider, IRoutingProvider
    {
        public const string ProviderName = "Goong";
        private const string Vietnam = "VN";

        private readonly HttpClient _http;
        private readonly GeoOptions _options;
        private readonly ILogger<GoongGeoProvider> _logger;
        private readonly string? _apiKey;

        public GoongGeoProvider(HttpClient http, IOptions<GeoOptions> options, IConfiguration configuration, ILogger<GoongGeoProvider> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
            _apiKey = configuration["Goong:ApiKey"];
        }

        public string Name => ProviderName;

        private bool Configured => !string.IsNullOrWhiteSpace(_apiKey) && !_apiKey.StartsWith('<');

        public bool Supports(string? countryCode) => Configured && countryCode == Vietnam;

        public bool Supports(string? fromCountryCode, string? toCountryCode) =>
            Configured && fromCountryCode == Vietnam && toCountryCode == Vietnam;

        public async Task<IReadOnlyList<GeoPlaceModel>?> SearchAsync(string query, string? countryCode, int limit, CancellationToken cancellationToken)
        {
            using var doc = await GetJsonAsync($"geocode?address={Uri.EscapeDataString(query)}", cancellationToken);
            if (doc == null)
            {
                return null;
            }
            return ReadResults(doc.RootElement).Take(limit).ToList();
        }

        public async Task<GeoPlaceModel?> ReverseAsync(double lat, double lng, CancellationToken cancellationToken)
        {
            using var doc = await GetJsonAsync($"Geocode?latlng={GeoMath.Num(lat)},{GeoMath.Num(lng)}", cancellationToken);
            var place = doc == null ? null : ReadResults(doc.RootElement).FirstOrDefault();
            if (place != null)
            {
                // Kết quả ngược trả về tọa độ của địa chỉ gần nhất; giữ đúng điểm người dùng đã chọn
                place.Lat = lat;
                place.Lng = lng;
            }
            return place;
        }

        public async Task<GeoRouteLegModel?> RouteLegAsync(GeoPointModel from, GeoPointModel to, CancellationToken cancellationToken)
        {
            using var doc = await GetJsonAsync(
                $"Direction?origin={GeoMath.Num(from.Lat)},{GeoMath.Num(from.Lng)}&destination={GeoMath.Num(to.Lat)},{GeoMath.Num(to.Lng)}&vehicle=truck",
                cancellationToken);
            if (doc == null
                || !doc.RootElement.TryGetProperty("routes", out var routes)
                || routes.ValueKind != JsonValueKind.Array
                || routes.GetArrayLength() == 0)
            {
                return null;
            }

            try
            {
                var route = routes[0];
                double meters = 0, seconds = 0;
                foreach (var leg in route.GetProperty("legs").EnumerateArray())
                {
                    meters += leg.GetProperty("distance").GetProperty("value").GetDouble();
                    seconds += leg.GetProperty("duration").GetProperty("value").GetDouble();
                }
                string? encoded = route.TryGetProperty("overview_polyline", out var overview)
                    && overview.TryGetProperty("points", out var pts) ? pts.GetString() : null;

                return new GeoRouteLegModel
                {
                    DistanceKm = Math.Round((decimal)(meters / 1000), 2),
                    DurationHours = Math.Round((decimal)(seconds / 3600), 2),
                    Provider = ProviderName,
                    Polyline = GeoMath.DecodePolyline(encoded)
                };
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or FormatException)
            {
                _logger.LogWarning(ex, "Goong returned a route in an unexpected format.");
                return null;
            }
        }

        private static IEnumerable<GeoPlaceModel> ReadResults(JsonElement root)
        {
            if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            foreach (var item in results.EnumerateArray())
            {
                if (!item.TryGetProperty("geometry", out var geometry) || !geometry.TryGetProperty("location", out var location))
                {
                    continue;
                }
                string name = item.TryGetProperty("formatted_address", out var address) ? address.GetString() ?? string.Empty : string.Empty;
                yield return new GeoPlaceModel
                {
                    DisplayName = name,
                    Lat = location.GetProperty("lat").GetDouble(),
                    Lng = location.GetProperty("lng").GetDouble(),
                    CountryCode = Vietnam,
                    Provider = ProviderName
                };
            }
        }

        // Gọi Goong; lỗi mạng / HTTP / JSON thì ghi log và trả null để nơi gọi chuyển sang dự phòng
        private async Task<JsonDocument?> GetJsonAsync(string pathAndQuery, CancellationToken cancellationToken)
        {
            if (!Configured)
            {
                return null;
            }

            string url = $"{_options.GoongUrl.TrimEnd('/')}/{pathAndQuery}&api_key={Uri.EscapeDataString(_apiKey!)}";
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                using var response = await _http.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Goong returned HTTP {Status} for {Path}.", (int)response.StatusCode, pathAndQuery.Split('?')[0]);
                    return null;
                }
                return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Goong request failed for {Path}.", pathAndQuery.Split('?')[0]);
                return null;
            }
        }
    }
}
