using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // OSRM (OpenStreetMap): chỉ đường ô tô khắp thế giới, dùng cho chặng có đầu nằm ngoài Việt Nam
    public class OsrmRoutingProvider : IRoutingProvider
    {
        public const string ProviderName = "Osrm";

        private readonly HttpClient _http;
        private readonly GeoOptions _options;
        private readonly ILogger<OsrmRoutingProvider> _logger;

        public OsrmRoutingProvider(HttpClient http, IOptions<GeoOptions> options, ILogger<OsrmRoutingProvider> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public string Name => ProviderName;

        public bool Supports(string? fromCountryCode, string? toCountryCode) => true;

        public async Task<GeoRouteLegModel?> RouteLegAsync(GeoPointModel from, GeoPointModel to, CancellationToken cancellationToken)
        {
            // OSRM nhận tọa độ theo thứ tự kinh độ,vĩ độ
            string url = $"{_options.OsrmUrl.TrimEnd('/')}/route/v1/driving/"
                + $"{GeoMath.Num(from.Lng)},{GeoMath.Num(from.Lat)};{GeoMath.Num(to.Lng)},{GeoMath.Num(to.Lat)}"
                + "?overview=full&geometries=polyline";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd(_options.UserAgent);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                using var response = await _http.SendAsync(request, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OSRM returned HTTP {Status}.", (int)response.StatusCode);
                    return null;
                }

                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
                var root = doc.RootElement;
                if (!root.TryGetProperty("code", out var code) || code.GetString() != "Ok"
                    || !root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                {
                    return null; // NoRoute: hai điểm không nối được bằng đường bộ
                }

                var route = routes[0];
                return new GeoRouteLegModel
                {
                    DistanceKm = Math.Round((decimal)(route.GetProperty("distance").GetDouble() / 1000), 2),
                    DurationHours = Math.Round((decimal)(route.GetProperty("duration").GetDouble() / 3600), 2),
                    Provider = ProviderName,
                    Polyline = GeoMath.DecodePolyline(route.TryGetProperty("geometry", out var geometry) ? geometry.GetString() : null)
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "OSRM request failed.");
                return null;
            }
        }
    }
}
