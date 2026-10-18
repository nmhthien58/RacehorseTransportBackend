using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    public class DistanceService : IDistanceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DistanceService> _logger;

        // Tọa độ các trung tâm logistics / thành phố chính để tính toán cự ly thực tế
        private static readonly Dictionary<string, (double Lat, double Lng)> KnownCityCoordinates = new(StringComparer.OrdinalIgnoreCase)
        {
            // Việt Nam - Miền Nam
            ["TP. Hồ Chí Minh"] = (10.7769, 106.7009),
            ["Hồ Chí Minh"] = (10.7769, 106.7009),
            ["TPHCM"] = (10.7769, 106.7009),
            ["Bình Dương"] = (10.9804, 106.6519),
            ["Đồng Nai"] = (10.9574, 106.8427),
            ["Long An"] = (10.5333, 106.4167),
            ["Tây Ninh"] = (11.3100, 106.0983),
            ["Cần Thơ"] = (10.0452, 105.7469),
            ["Vũng Tàu"] = (10.3460, 107.0843),
            ["Đà Lạt"] = (11.9404, 108.4583),
            ["Lâm Đồng"] = (11.9404, 108.4583),

            // Việt Nam - Miền Trung
            ["Đà Nẵng"] = (16.0544, 108.2022),
            ["Huế"] = (16.4637, 107.5909),
            ["Quảng Trị"] = (16.7500, 107.2000),
            ["Hà Tĩnh"] = (18.3559, 105.9059),
            ["Nghệ An"] = (18.6734, 105.6813),
            ["Khánh Hòa"] = (12.2388, 109.1967),
            ["Nha Trang"] = (12.2388, 109.1967),

            // Việt Nam - Miền Bắc
            ["Hà Nội"] = (21.0285, 105.8542),
            ["Hải Phòng"] = (20.8449, 106.6881),
            ["Quảng Ninh"] = (20.9505, 107.0734),
            ["Lạng Sơn"] = (21.8537, 106.7615),
            ["Lào Cai"] = (22.4856, 103.9707),
            ["Bắc Ninh"] = (21.1861, 106.0763),

            // Campuchia (KH)
            ["Phnom Penh"] = (11.5564, 104.9282),
            ["Bavet"] = (11.0800, 106.1400),
            ["Siem Reap"] = (13.3671, 103.8448),
            ["Sihanoukville"] = (10.6275, 103.5221),
            ["Kandal"] = (11.4500, 105.0000),

            // Thái Lan (TH)
            ["Bangkok"] = (13.7563, 100.5018),
            ["Băng Cốc"] = (13.7563, 100.5018),
            ["Pattaya"] = (12.9276, 100.8771),
            ["Nakhon Ratchasima"] = (14.9799, 102.0978),
            ["Chiang Mai"] = (18.7883, 98.9853),

            // Lào (LA)
            ["Vientiane"] = (17.9757, 102.6331),
            ["Viêng Chăn"] = (17.9757, 102.6331),
            ["Savannakhet"] = (16.5566, 104.7523),
            ["Pakse"] = (15.1214, 105.7988),

            // Trung Quốc (CN)
            ["Nanning"] = (22.8170, 108.3665),
            ["Nam Ninh"] = (22.8170, 108.3665),
            ["Pingxiang"] = (22.0967, 106.7550),
            ["Bằng Tường"] = (22.0967, 106.7550),
            ["Guangzhou"] = (23.1291, 113.2644),
            ["Quảng Châu"] = (23.1291, 113.2644),
            ["Kunming"] = (25.0406, 102.7129),
            ["Côn Minh"] = (25.0406, 102.7129),

            // Nga (RU)
            ["Moscow"] = (55.7558, 37.6173),
            ["Saint Petersburg"] = (59.9311, 30.3609),
            ["Novosibirsk"] = (55.0084, 82.9357)
        };

        public DistanceService(HttpClient httpClient, IConfiguration configuration, ILogger<DistanceService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DistanceEstimateResultModel> EstimateGroundDistanceAsync(DistanceEstimateInputModel input, CancellationToken cancellationToken = default)
        {
            // 1. Thử gọi Goong API nếu có cấu hình API Key
            string? goongApiKey = _configuration["Goong:ApiKey"]
                ?? _configuration["Goong__ApiKey"]
                ?? _configuration["GOONG_API_KEY"]
                ?? Environment.GetEnvironmentVariable("GOONG_API_KEY")
                ?? Environment.GetEnvironmentVariable("Goong__ApiKey");

            if (!string.IsNullOrWhiteSpace(goongApiKey))
            {
                var goongResult = await TryEstimateWithGoongAsync(input, goongApiKey, cancellationToken);
                if (goongResult != null && goongResult.DistanceKm > 0)
                {
                    return goongResult;
                }
            }

            // 2. Thử gọi OSRM Route API nếu xác định được tọa độ trung tâm
            var osrmResult = await TryEstimateWithOsrmAsync(input, cancellationToken);
            if (osrmResult != null && osrmResult.DistanceKm > 0)
            {
                return osrmResult;
            }

            // 3. Fallback sang Bảng cự ly đường bộ chuẩn (Reliable Deterministic Route Matrix)
            return EstimateWithMatrix(input);
        }

        private async Task<DistanceEstimateResultModel?> TryEstimateWithGoongAsync(DistanceEstimateInputModel input, string apiKey, CancellationToken cancellationToken)
        {
            try
            {
                string originText = BuildSearchAddress(input.PickupAddress, input.PickupCity, input.PickupCountryCode);
                string destinationText = BuildSearchAddress(input.DropoffAddress, input.DropoffCity, input.DropoffCountryCode);

                // Geocode origin và destination
                var originCoord = await GeocodeWithGoongAsync(originText, apiKey, cancellationToken);
                var destCoord = await GeocodeWithGoongAsync(destinationText, apiKey, cancellationToken);

                if (originCoord == null || destCoord == null)
                {
                    return null;
                }

                string url = $"https://rsapi.goong.io/DistanceMatrix?origins={originCoord.Value.Lat},{originCoord.Value.Lng}&destinations={destCoord.Value.Lat},{destCoord.Value.Lng}&vehicle=car&api_key={apiKey}";
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                var response = await _httpClient.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
                var root = doc.RootElement;

                if (root.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                {
                    var elements = rows[0].GetProperty("elements");
                    if (elements.GetArrayLength() > 0)
                    {
                        var firstElement = elements[0];
                        if (firstElement.TryGetProperty("status", out var status) && status.GetString() == "OK")
                        {
                            long distanceMeters = firstElement.GetProperty("distance").GetProperty("value").GetInt64();
                            long durationSeconds = firstElement.GetProperty("duration").GetProperty("value").GetInt64();

                            int km = Math.Max(1, (int)Math.Round(distanceMeters / 1000.0));
                            decimal hours = Math.Round((decimal)durationSeconds / 3600m, 1);

                            return new DistanceEstimateResultModel
                            {
                                DistanceKm = km,
                                EstimatedHours = hours > 0 ? hours : Math.Round(km / 60m, 1),
                                RouteSummary = $"{km} km (Goong Maps - {input.PickupCity ?? "VN"} → {input.DropoffCity ?? input.DropoffCountryCode})",
                                Source = "GoongApi"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate distance with Goong API, falling back to next provider.");
            }

            return null;
        }

        private async Task<(double Lat, double Lng)?> GeocodeWithGoongAsync(string address, string apiKey, CancellationToken cancellationToken)
        {
            try
            {
                string url = $"https://rsapi.goong.io/geocode?address={Uri.EscapeDataString(address)}&api_key={apiKey}";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode) return null;

                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                var root = doc.RootElement;
                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var location = results[0].GetProperty("geometry").GetProperty("location");
                    return (location.GetProperty("lat").GetDouble(), location.GetProperty("lng").GetDouble());
                }
            }
            catch
            {
                // Ignore geocode error
            }
            return null;
        }

        private async Task<DistanceEstimateResultModel?> TryEstimateWithOsrmAsync(DistanceEstimateInputModel input, CancellationToken cancellationToken)
        {
            try
            {
                var originCoord = ResolveCoordinates(input.PickupCity, input.PickupAddress, input.PickupCountryCode);
                var destCoord = ResolveCoordinates(input.DropoffCity, input.DropoffAddress, input.DropoffCountryCode);

                if (originCoord == null || destCoord == null)
                {
                    return null;
                }

                // OSRM API: driving route format: {lon1},{lat1};{lon2},{lat2}
                string url = $"https://router.project-osrm.org/route/v1/driving/{originCoord.Value.Lng.ToString(CultureInfo.InvariantCulture)},{originCoord.Value.Lat.ToString(CultureInfo.InvariantCulture)};{destCoord.Value.Lng.ToString(CultureInfo.InvariantCulture)},{destCoord.Value.Lat.ToString(CultureInfo.InvariantCulture)}?overview=false";

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(2)); // Short timeout

                var response = await _httpClient.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode) return null;

                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
                var root = doc.RootElement;
                if (root.TryGetProperty("code", out var code) && code.GetString() == "Ok" && root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                {
                    var route = routes[0];
                    double distanceMeters = route.GetProperty("distance").GetDouble();
                    double durationSeconds = route.GetProperty("duration").GetDouble();

                    int km = Math.Max(1, (int)Math.Round(distanceMeters / 1000.0));
                    decimal hours = Math.Round((decimal)durationSeconds / 3600m, 1);

                    return new DistanceEstimateResultModel
                    {
                        DistanceKm = km,
                        EstimatedHours = hours > 0 ? hours : Math.Round(km / 60m, 1),
                        RouteSummary = $"{km} km (OSRM Route - {input.PickupCity ?? "VN"} → {input.DropoffCity ?? input.DropoffCountryCode})",
                        Source = "OsrmApi"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "OSRM route estimation skipped/failed.");
            }

            return null;
        }

        private DistanceEstimateResultModel EstimateWithMatrix(DistanceEstimateInputModel input)
        {
            string originRegion = DetectRegion(input.PickupCity, input.PickupAddress);
            string destCountry = (input.DropoffCountryCode ?? "").Trim().ToUpperInvariant();
            string destCity = input.DropoffCity ?? "";

            int distanceKm = 500;
            string routeDesc = "Tuyến vận tải đường bộ tiêu chuẩn";

            if (destCountry == "KH") // Campuchia
            {
                if (originRegion == "North")
                {
                    distanceKm = 1450;
                    routeDesc = "Hà Nội/Miền Bắc → Cửa khẩu Mộc Bài → Phnom Penh";
                }
                else if (originRegion == "Central")
                {
                    distanceKm = 850;
                    routeDesc = "Miền Trung → Cửa khẩu Hoa Lư/Bavet → Phnom Penh";
                }
                else
                {
                    // Miền Nam / TP.HCM
                    distanceKm = MatchesCity(destCity, "Siem Reap") ? 460 : 250;
                    routeDesc = "TP.HCM/Miền Nam → Cửa khẩu Mộc Bài/Bavet → Phnom Penh";
                }
            }
            else if (destCountry == "TH") // Thái Lan
            {
                if (originRegion == "North")
                {
                    distanceKm = 1150;
                    routeDesc = "Hà Nội → Cửa khẩu Cầu Treo → Nakhon Phanom → Bangkok";
                }
                else
                {
                    distanceKm = 900;
                    routeDesc = "TP.HCM → Mộc Bài → Phnom Penh → Poipet → Bangkok";
                }
            }
            else if (destCountry == "LA") // Lào
            {
                if (originRegion == "North")
                {
                    distanceKm = 650;
                    routeDesc = "Hà Nội → Cửa khẩu Cầu Treo → Vientiane";
                }
                else if (originRegion == "Central")
                {
                    distanceKm = 720;
                    routeDesc = "Đà Nẵng/Miền Trung → Cửa khẩu Lao Bảo → Savannakhet → Vientiane";
                }
                else
                {
                    distanceKm = 1100;
                    routeDesc = "TP.HCM/Miền Nam → Cửa khẩu Bờ Y/Lao Bảo → Vientiane";
                }
            }
            else if (destCountry == "CN") // Trung Quốc
            {
                if (originRegion == "North")
                {
                    distanceKm = MatchesCity(destCity, "Guangzhou", "Quảng Châu") ? 850 : 350;
                    routeDesc = "Hà Nội → Cửa khẩu Quốc tế Hữu Nghị → Nam Ninh (Quảng Tây)";
                }
                else
                {
                    distanceKm = 1950;
                    routeDesc = "TP.HCM/Miền Nam → Quốc lộ 1A → Hữu Nghị Quan → Nam Ninh";
                }
            }
            else if (destCountry == "RU") // Nga: đường bộ đi qua Trung Quốc (và Kazakhstan / Mông Cổ)
            {
                bool siberia = MatchesCity(destCity, "Novosibirsk");
                if (originRegion == "North")
                {
                    distanceKm = siberia ? 6400 : 9300;
                    routeDesc = siberia
                        ? "Hà Nội → Hữu Nghị Quan → Trung Quốc → Mông Cổ → Novosibirsk"
                        : "Hà Nội → Hữu Nghị Quan → Trung Quốc → Kazakhstan → Moscow";
                }
                else
                {
                    distanceKm = siberia ? 8000 : 10900;
                    routeDesc = siberia
                        ? "TP.HCM → Hữu Nghị Quan → Trung Quốc → Mông Cổ → Novosibirsk"
                        : "TP.HCM → Hữu Nghị Quan → Trung Quốc → Kazakhstan → Moscow";
                }
            }
            else // Nội địa hoặc tuyến khác
            {
                if (originRegion == "South" && MatchesCity(destCity, "Hà Nội", "Hanoi"))
                {
                    distanceKm = 1720;
                    routeDesc = "Tuyến Bắc - Nam (TP.HCM ↔ Hà Nội)";
                }
                else if (originRegion == "South" && MatchesCity(destCity, "Bình Dương", "Đồng Nai", "Long An"))
                {
                    distanceKm = 45;
                    routeDesc = "Tuyến liên tỉnh Đông Nam Bộ";
                }
                else
                {
                    distanceKm = 150;
                    routeDesc = "Tuyến đường bộ nội địa";
                }
            }

            decimal estimatedHours = Math.Round(distanceKm / 55m, 1); // Vận chuyển ngựa tốc độ an toàn trung bình ~55 km/h

            return new DistanceEstimateResultModel
            {
                DistanceKm = distanceKm,
                EstimatedHours = estimatedHours,
                RouteSummary = $"{distanceKm} km ({routeDesc})",
                Source = "RouteMatrix"
            };
        }

        private (double Lat, double Lng)? ResolveCoordinates(string? city, string? address, string? countryCode)
        {
            if (!string.IsNullOrWhiteSpace(city))
            {
                foreach (var kvp in KnownCityCoordinates)
                {
                    if (MatchesCity(city, kvp.Key)) return kvp.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                foreach (var kvp in KnownCityCoordinates)
                {
                    if (address.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase)) return kvp.Value;
                }
            }

            // Fallback theo country
            string cc = (countryCode ?? "").Trim().ToUpperInvariant();
            if (cc == "VN") return KnownCityCoordinates["TP. Hồ Chí Minh"];
            if (cc == "KH") return KnownCityCoordinates["Phnom Penh"];
            if (cc == "TH") return KnownCityCoordinates["Bangkok"];
            if (cc == "LA") return KnownCityCoordinates["Vientiane"];
            if (cc == "CN") return KnownCityCoordinates["Nam Ninh"];
            if (cc == "RU") return KnownCityCoordinates["Moscow"];

            return null;
        }

        private static string DetectRegion(string? city, string? address)
        {
            string combined = $"{city} {address}".ToLowerInvariant();
            if (combined.Contains("hà nội") || combined.Contains("hanoi") || combined.Contains("hải phòng") ||
                combined.Contains("quảng ninh") || combined.Contains("lạng sơn") || combined.Contains("lào cai") || combined.Contains("bắc ninh"))
            {
                return "North";
            }

            if (combined.Contains("đà nẵng") || combined.Contains("huế") || combined.Contains("quảng trị") ||
                combined.Contains("hà tĩnh") || combined.Contains("nghệ an") || combined.Contains("nha trang"))
            {
                return "Central";
            }

            return "South"; // Mặc định miền Nam
        }

        private static bool MatchesCity(string? input, params string[] targets)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string clean = RemoveDiacritics(input).ToLowerInvariant();
            foreach (var target in targets)
            {
                string targetClean = RemoveDiacritics(target).ToLowerInvariant();
                if (clean.Contains(targetClean) || targetClean.Contains(clean)) return true;
            }
            return false;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
        }

        private static string BuildSearchAddress(string? address, string? city, string? countryCode)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(address)) parts.Add(address.Trim());
            if (!string.IsNullOrWhiteSpace(city)) parts.Add(city.Trim());
            if (!string.IsNullOrWhiteSpace(countryCode)) parts.Add(countryCode.Trim());
            return string.Join(", ", parts);
        }
    }
}
