using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Nominatim (OpenStreetMap): tìm địa chỉ và tọa độ -> địa chỉ + mã nước, khắp thế giới.
    // Chính sách sử dụng: User-Agent riêng, tối đa 1 request / giây -> mọi request đi qua một hàng đợi chung.
    public class NominatimGeocodingProvider : IGeocodingProvider
    {
        public const string ProviderName = "Nominatim";

        // Dùng chung cho mọi instance (HttpClient typed được tạo mới theo từng scope)
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static DateTime _lastRequestUtc = DateTime.MinValue;

        private readonly HttpClient _http;
        private readonly GeoOptions _options;
        private readonly ILogger<NominatimGeocodingProvider> _logger;

        public NominatimGeocodingProvider(HttpClient http, IOptions<GeoOptions> options, ILogger<NominatimGeocodingProvider> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public string Name => ProviderName;

        public bool Supports(string? countryCode) => true;

        public async Task<IReadOnlyList<GeoPlaceModel>?> SearchAsync(string query, string? countryCode, int limit, CancellationToken cancellationToken)
        {
            string path = $"search?q={Uri.EscapeDataString(query)}&format=jsonv2&addressdetails=1&limit={limit}";
            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                path += $"&countrycodes={countryCode.ToLowerInvariant()}";
            }

            using var doc = await GetJsonAsync(path, cancellationToken);
            if (doc == null || doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return doc == null ? null : Array.Empty<GeoPlaceModel>();
            }

            return doc.RootElement.EnumerateArray()
                .Select(ReadPlace)
                .Where(p => p != null)
                .Select(p => p!)
                .ToList();
        }

        public async Task<GeoPlaceModel?> ReverseAsync(double lat, double lng, CancellationToken cancellationToken)
        {
            using var doc = await GetJsonAsync($"reverse?lat={GeoMath.Num(lat)}&lon={GeoMath.Num(lng)}&format=jsonv2&zoom=14", cancellationToken);
            if (doc == null || doc.RootElement.TryGetProperty("error", out _))
            {
                return null; // lỗi gọi, hoặc điểm không có địa chỉ (giữa biển)
            }

            var place = ReadPlace(doc.RootElement);
            if (place != null)
            {
                place.Lat = lat;
                place.Lng = lng;
            }
            return place;
        }

        private static GeoPlaceModel? ReadPlace(JsonElement item)
        {
            if (!item.TryGetProperty("lat", out var latEl) || !item.TryGetProperty("lon", out var lonEl))
            {
                return null;
            }

            string? country = item.TryGetProperty("address", out var address) && address.TryGetProperty("country_code", out var cc)
                ? cc.GetString()?.ToUpperInvariant()
                : null;

            return new GeoPlaceModel
            {
                DisplayName = item.TryGetProperty("display_name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                Lat = ReadNumber(latEl),
                Lng = ReadNumber(lonEl),
                CountryCode = country,
                Provider = ProviderName
            };
        }

        // Nominatim trả tọa độ dạng chuỗi ("10.77")
        private static double ReadNumber(JsonElement value) =>
            value.ValueKind == JsonValueKind.Number
                ? value.GetDouble()
                : double.Parse(value.GetString() ?? "0", NumberStyles.Float, CultureInfo.InvariantCulture);

        private async Task<JsonDocument?> GetJsonAsync(string pathAndQuery, CancellationToken cancellationToken)
        {
            string url = $"{_options.NominatimUrl.TrimEnd('/')}/{pathAndQuery}";
            await Gate.WaitAsync(cancellationToken);
            try
            {
                // Giãn cách tối thiểu giữa hai request
                var wait = _lastRequestUtc.AddMilliseconds(_options.NominatimMinIntervalMs) - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, cancellationToken);
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd(_options.UserAgent);
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("vi"));
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.8));

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
                using var response = await _http.SendAsync(request, cts.Token);
                _lastRequestUtc = DateTime.UtcNow;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Nominatim returned HTTP {Status} for {Path}.", (int)response.StatusCode, pathAndQuery.Split('?')[0]);
                    return null;
                }
                return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or FormatException)
            {
                _lastRequestUtc = DateTime.UtcNow;
                _logger.LogWarning(ex, "Nominatim request failed for {Path}.", pathAndQuery.Split('?')[0]);
                return null;
            }
            finally
            {
                Gate.Release();
            }
        }
    }
}
