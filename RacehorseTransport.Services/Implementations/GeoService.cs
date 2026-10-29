using Microsoft.Extensions.Options;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // BE-01 / R-11: điều phối các nhà cung cấp bản đồ.
    //   - Chọn nhà cung cấp đầu tiên (theo thứ tự đăng ký) phục vụ được vùng đó: Goong cho Việt Nam, Nominatim / OSRM cho phần còn lại.
    //   - Nhà cung cấp lỗi thì thử nhà cung cấp kế tiếp; chỉ đường lỗi hết thì ước tính theo đường chim bay (IsEstimate = true).
    //   - Kết quả thành công được cache CacheDays ngày, khóa theo tọa độ làm tròn 5 chữ số.
    public class GeoService : IGeoService
    {
        public const string EstimateProvider = "Estimate";

        private readonly IReadOnlyList<IGeocodingProvider> _geocoders;
        private readonly IReadOnlyList<IRoutingProvider> _routers;
        private readonly ICacheService _cache;
        private readonly GeoOptions _options;
        private readonly TransitOptions _transit;

        public GeoService(
            IEnumerable<IGeocodingProvider> geocoders,
            IEnumerable<IRoutingProvider> routers,
            ICacheService cache,
            IOptions<GeoOptions> options,
            IOptions<TransitOptions> transit)
        {
            _geocoders = geocoders.ToList();
            _routers = routers.ToList();
            _cache = cache;
            _options = options.Value;
            _transit = transit.Value;
        }

        private TimeSpan CacheTtl => TimeSpan.FromDays(Math.Max(1, _options.CacheDays));

        // =============================================================================================
        // Tìm địa chỉ
        // =============================================================================================

        public async Task<IReadOnlyList<GeoPlaceModel>> SearchAsync(string query, string? countryCode, CancellationToken cancellationToken = default)
        {
            query = query.Trim();
            if (query.Length < 2)
            {
                throw new BusinessException("Type at least 2 characters to search for an address.");
            }
            string? country = NormalizeCountry(countryCode);

            string key = $"geo:search:{country ?? "*"}:{query.ToLowerInvariant()}";
            var cached = await _cache.GetAsync<List<GeoPlaceModel>>(key);
            if (cached != null)
            {
                return cached;
            }

            foreach (var provider in _geocoders.Where(p => p.Supports(country)))
            {
                var places = await provider.SearchAsync(query, country, _options.MaxSearchResults, cancellationToken);
                if (places == null || places.Count == 0)
                {
                    continue; // lỗi hoặc không có kết quả: thử nhà cung cấp kế tiếp
                }

                var result = places.ToList();
                await _cache.SetAsync(key, result, CacheTtl);
                return result;
            }
            return Array.Empty<GeoPlaceModel>();
        }

        // =============================================================================================
        // Tọa độ -> địa chỉ
        // =============================================================================================

        public async Task<GeoPlaceModel?> ReverseAsync(double lat, double lng, CancellationToken cancellationToken = default)
        {
            ValidateCoordinate(lat, lng);

            string key = $"geo:reverse:{GeoMath.Key(lat, lng)}";
            var cached = await _cache.GetAsync<GeoPlaceModel>(key);
            if (cached != null)
            {
                return cached;
            }

            // Chưa biết nước: hỏi nhà cung cấp phủ toàn cầu trước để biết mã nước
            GeoPlaceModel? place = null;
            foreach (var provider in _geocoders.Where(p => p.Supports(null)))
            {
                place = await provider.ReverseAsync(lat, lng, cancellationToken);
                if (place != null)
                {
                    break;
                }
            }

            // Điểm ở một nước có nhà cung cấp riêng (Goong cho Việt Nam): lấy địa chỉ của nhà cung cấp đó cho chính xác hơn
            var local = _geocoders.FirstOrDefault(p => !p.Supports(null) && p.Supports(place?.CountryCode));
            if (local != null)
            {
                var localPlace = await local.ReverseAsync(lat, lng, cancellationToken);
                if (localPlace != null && !string.IsNullOrWhiteSpace(localPlace.DisplayName))
                {
                    localPlace.CountryCode = place!.CountryCode;
                    place = localPlace;
                }
            }

            if (place != null)
            {
                await _cache.SetAsync(key, place, CacheTtl);
            }
            return place;
        }

        // =============================================================================================
        // Chỉ đường
        // =============================================================================================

        public async Task<GeoRouteModel> RouteAsync(IReadOnlyList<GeoPointModel> points, bool includeCountries = false, CancellationToken cancellationToken = default)
        {
            if (points.Count < 2)
            {
                throw new BusinessException("A route needs at least 2 points.");
            }
            foreach (var point in points)
            {
                ValidateCoordinate(point.Lat, point.Lng);
                point.CountryCode = NormalizeCountry(point.CountryCode);
            }

            // Nước của từng điểm quyết định nhà cung cấp chỉ đường cho chặng
            foreach (var point in points.Where(p => p.CountryCode == null))
            {
                point.CountryCode = (await ReverseAsync(point.Lat, point.Lng, cancellationToken))?.CountryCode;
            }

            var route = new GeoRouteModel();
            var fullLine = new List<GeoPointModel>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                var leg = await RouteLegAsync(points[i], points[i + 1], cancellationToken);
                leg.FromIndex = i;
                leg.ToIndex = i + 1;
                route.Legs.Add(leg);

                // Nối đường vẽ các chặng, bỏ điểm đầu trùng với điểm cuối chặng trước
                fullLine.AddRange(fullLine.Count > 0 ? leg.Polyline.Skip(1) : leg.Polyline);
            }

            route.TotalDistanceKm = route.Legs.Sum(l => l.DistanceKm);
            route.TotalDurationHours = route.Legs.Sum(l => l.DurationHours);
            route.IsEstimate = route.Legs.Any(l => l.IsEstimate);

            if (includeCountries)
            {
                // Hai đầu tuyến đã biết nước (từ bước trên): gắn vào bản sao để không tra lại và luôn có khi hết giờ
                var line = fullLine.ToList();
                if (line.Count > 0)
                {
                    line[0] = new GeoPointModel(line[0].Lat, line[0].Lng, points[0].CountryCode);
                    line[^1] = new GeoPointModel(line[^1].Lat, line[^1].Lng, points[^1].CountryCode);
                }
                route.CountryCodes = (await CountriesAlongAsync(line, cancellationToken)).ToList();
            }

            // Trả về đường đã rút gọn: từng chặng và cả tuyến
            foreach (var leg in route.Legs)
            {
                leg.Polyline = GeoMath.Simplify(leg.Polyline, _options.MaxPolylinePoints);
            }
            route.Polyline = GeoMath.Simplify(fullLine, _options.MaxPolylinePoints);
            return route;
        }

        private async Task<GeoRouteLegModel> RouteLegAsync(GeoPointModel from, GeoPointModel to, CancellationToken cancellationToken)
        {
            foreach (var provider in _routers.Where(p => p.Supports(from.CountryCode, to.CountryCode)))
            {
                string key = $"geo:leg:{provider.Name}:{GeoMath.Key(from)};{GeoMath.Key(to)}";
                var cached = await _cache.GetAsync<GeoRouteLegModel>(key);
                if (cached != null)
                {
                    return cached;
                }

                var leg = await provider.RouteLegAsync(from, to, cancellationToken);
                if (leg == null)
                {
                    continue;
                }
                if (leg.Polyline.Count == 0)
                {
                    leg.Polyline = new List<GeoPointModel> { Copy(from), Copy(to) };
                }
                await _cache.SetAsync(key, leg, CacheTtl);
                return leg;
            }

            return EstimateLeg(from, to, _options.FallbackRoadFactor, _transit.TruckAverageSpeedKmh);
        }

        // Dự phòng khi mọi nhà cung cấp lỗi: đường chim bay × hệ số đường bộ, tốc độ trung bình xe chở ngựa
        public static GeoRouteLegModel EstimateLeg(GeoPointModel from, GeoPointModel to, decimal roadFactor, decimal averageSpeedKmh)
        {
            decimal km = Math.Round((decimal)GeoMath.HaversineKm(from, to) * roadFactor, 2);
            return new GeoRouteLegModel
            {
                DistanceKm = km,
                DurationHours = averageSpeedKmh > 0 ? Math.Round(km / averageSpeedKmh, 2) : 0,
                Provider = EstimateProvider,
                IsEstimate = true,
                Polyline = new List<GeoPointModel> { Copy(from), Copy(to) }
            };
        }

        // =============================================================================================
        // Các nước tuyến đi qua
        // =============================================================================================

        // Ước tính nhanh: chỉ tra mã nước, có giới hạn số lượt và thời gian. Điều phối viên xác nhận lại ở bước lập tuyến.
        public async Task<IReadOnlyList<string>> CountriesAlongAsync(IReadOnlyList<GeoPointModel> polyline, CancellationToken cancellationToken = default)
        {
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_options.CountryLookupBudgetSeconds > 0)
            {
                budget.CancelAfter(TimeSpan.FromSeconds(_options.CountryLookupBudgetSeconds));
            }
            return await CountriesAlongAsync(
                polyline,
                p => CountryCodeAtAsync(p.Lat, p.Lng, budget.Token),
                _options.CountryTrustSegmentKm,
                _options.CountryMinSegmentKm,
                _options.MaxCountryLookups,
                budget.Token);
        }

        // Mã nước tại một điểm, cache theo ô lưới thô để các tuyến gần nhau dùng lại.
        // Chỉ hỏi nhà cung cấp toàn cầu (Nominatim), không gọi Goong vì không cần địa chỉ.
        public async Task<string?> CountryCodeAtAsync(double lat, double lng, CancellationToken cancellationToken = default)
        {
            ValidateCoordinate(lat, lng);
            string key = $"geo:country:{GridCell(lat, lng, _options.CountryGridDegrees)}";
            var cached = await _cache.GetAsync<string>(key);
            if (cached != null)
            {
                return cached.Length == 0 ? null : cached;
            }

            foreach (var provider in _geocoders.Where(p => p.Supports(null)))
            {
                var code = await provider.CountryCodeAsync(lat, lng, cancellationToken);
                if (!string.IsNullOrEmpty(code))
                {
                    await _cache.SetAsync(key, code, TimeSpan.FromDays(Math.Max(1, _options.CountryCacheDays)));
                    return code;
                }
            }
            return null; // lỗi hoặc giữa biển: không cache để lần sau tra lại
        }

        public static string GridCell(double lat, double lng, double degrees)
        {
            double size = degrees > 0 ? degrees : 0.5;
            return string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"{Math.Floor(lat / size) * size:0.###},{Math.Floor(lng / size) * size:0.###}");
        }

        // Hàm thuần (nhận cách tra nước từ ngoài) để viết unit test.
        // Chỉ tra nước ở hai đầu một đoạn: cùng nước và đoạn ngắn hơn trustKm -> coi như không qua nước khác;
        // ngược lại chia đôi đoạn (theo quãng đường) và tra điểm giữa. Ít lượt tra hơn nhiều so với lấy mẫu đều,
        // quan trọng vì Nominatim chỉ cho 1 request / giây.
        public static async Task<IReadOnlyList<string>> CountriesAlongAsync(
            IReadOnlyList<GeoPointModel> polyline,
            Func<GeoPointModel, Task<string?>> countryAt,
            decimal trustKm,
            decimal minSegmentKm,
            int maxLookups,
            CancellationToken stopToken = default)
        {
            if (polyline.Count == 0)
            {
                return Array.Empty<string>();
            }

            var cumulative = GeoMath.CumulativeKm(polyline);
            var known = new Dictionary<int, string?>();
            int lookups = 0;

            async Task<string?> At(int index)
            {
                if (known.TryGetValue(index, out var code))
                {
                    return code;
                }
                // Điểm đã biết nước (đầu / cuối tuyến) thì không tốn lượt tra
                if (!string.IsNullOrEmpty(polyline[index].CountryCode))
                {
                    return known[index] = polyline[index].CountryCode;
                }
                // Hết thời gian: không tra thêm, không ném lỗi
                if (stopToken.IsCancellationRequested)
                {
                    return null;
                }
                lookups++;
                try
                {
                    code = await countryAt(polyline[index]);
                }
                catch (OperationCanceledException) when (stopToken.IsCancellationRequested)
                {
                    return null;
                }
                known[index] = code;
                return code;
            }

            var sequence = new List<string?> { await At(0) };

            async Task Walk(int from, int to, string? fromCountry, string? toCountry)
            {
                double length = cumulative[to] - cumulative[from];
                if (to - from <= 1 || lookups >= maxLookups || length <= (double)minSegmentKm || stopToken.IsCancellationRequested)
                {
                    return;
                }
                if (fromCountry != null && fromCountry == toCountry && length <= (double)trustKm)
                {
                    return;
                }

                // Điểm nằm gần giữa đoạn nhất theo quãng đường
                double half = cumulative[from] + length / 2;
                int mid = from + 1;
                while (mid < to - 1 && cumulative[mid] < half)
                {
                    mid++;
                }

                string? midCountry = await At(mid);
                await Walk(from, mid, fromCountry, midCountry);
                sequence.Add(midCountry);
                await Walk(mid, to, midCountry, toCountry);
            }

            int last = polyline.Count - 1;
            if (last > 0)
            {
                string? lastCountry = await At(last);
                await Walk(0, last, sequence[0], lastCountry);
                sequence.Add(lastCountry);
            }

            // Bỏ điểm không có nước (giữa biển), gộp trùng liên tiếp
            var result = new List<string>();
            foreach (var code in sequence.Where(c => !string.IsNullOrEmpty(c)))
            {
                if (result.Count == 0 || result[^1] != code)
                {
                    result.Add(code!);
                }
            }
            return result;
        }

        // ---------------------------------------------------------------------------------------------

        private static GeoPointModel Copy(GeoPointModel p) => new(p.Lat, p.Lng);

        private static string? NormalizeCountry(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return null;
            }
            string code = countryCode.Trim().ToUpperInvariant();
            if (code.Length != 2 || !code.All(char.IsLetter))
            {
                throw new BusinessException($"Country code '{countryCode}' is invalid. Use a 2-letter ISO code such as VN.");
            }
            return code;
        }

        private static void ValidateCoordinate(double lat, double lng)
        {
            if (double.IsNaN(lat) || double.IsNaN(lng) || lat < -90 || lat > 90 || lng < -180 || lng > 180)
            {
                throw new BusinessException($"Coordinates ({lat}, {lng}) are invalid. Latitude must be between -90 and 90, longitude between -180 and 180.");
            }
        }
    }
}
