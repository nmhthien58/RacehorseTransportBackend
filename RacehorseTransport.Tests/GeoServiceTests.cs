using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace RacehorseTransport.Tests;

public class GeoServiceTests
{
    // ---------- Giả lập HTTP và cache ----------

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public List<HttpRequestMessage> Requests { get; } = new();

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (Requests) Requests.Add(request);
            return Task.FromResult(_respond(request));
        }
    }

    private sealed class MemoryCache : ICacheService
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _items = new();

        public Task<T?> GetAsync<T>(string key) =>
            Task.FromResult(_items.TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json) : default);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl)
        {
            _items[key] = JsonSerializer.Serialize(value);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _items.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
        {
            var cached = await GetAsync<T>(key);
            if (cached != null) return cached;
            var value = await factory();
            await SetAsync(key, value, ttl);
            return value;
        }
    }

    // Cấu hình tối giản: chỉ cần đọc khóa theo tên (không thêm package cấu hình vào project test)
    private sealed class KeyConfiguration : IConfiguration
    {
        private readonly Dictionary<string, string?> _values;
        public KeyConfiguration(Dictionary<string, string?> values) => _values = values;

        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var v) ? v : null;
            set => _values[key] = value;
        }

        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new NotSupportedException();
    }

    private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Fail() => new(HttpStatusCode.InternalServerError);

    private static readonly GeoOptions Options = new()
    {
        GoongUrl = "https://goong.test",
        NominatimUrl = "https://nominatim.test",
        OsrmUrl = "https://osrm.test",
        NominatimMinIntervalMs = 0
    };

    private static GoongGeoProvider Goong(FakeHandler handler, string? apiKey = "test-key")
    {
        var config = new KeyConfiguration(new Dictionary<string, string?> { ["Goong:ApiKey"] = apiKey });
        return new GoongGeoProvider(new HttpClient(handler), MsOptions.Create(Options), config, NullLogger<GoongGeoProvider>.Instance);
    }

    private static NominatimGeocodingProvider Nominatim(FakeHandler handler) =>
        new(new HttpClient(handler), MsOptions.Create(Options), NullLogger<NominatimGeocodingProvider>.Instance);

    private static OsrmRoutingProvider Osrm(FakeHandler handler) =>
        new(new HttpClient(handler), MsOptions.Create(Options), NullLogger<OsrmRoutingProvider>.Instance);

    private static GeoService Service(GoongGeoProvider goong, NominatimGeocodingProvider nominatim, OsrmRoutingProvider osrm, ICacheService? cache = null) =>
        new(new IGeocodingProvider[] { goong, nominatim },
            new IRoutingProvider[] { goong, osrm },
            cache ?? new MemoryCache(),
            MsOptions.Create(Options),
            MsOptions.Create(new TransitOptions { TruckAverageSpeedKmh = 55 }));

    // Đoạn polyline mẫu trong tài liệu của Google: (38.5,-120.2), (40.7,-120.95), (43.252,-126.453)
    private const string SamplePolyline = "_p~iF~ps|U_ulLnnqC_mqNvxq`@";

    private static object GoongRoute(double meters, double seconds) => new
    {
        routes = new[]
        {
            new
            {
                legs = new[] { new { distance = new { value = meters }, duration = new { value = seconds } } },
                overview_polyline = new { points = SamplePolyline }
            }
        }
    };

    private static object OsrmRoute(double meters, double seconds) => new
    {
        code = "Ok",
        routes = new[] { new { distance = meters, duration = seconds, geometry = SamplePolyline } }
    };

    private static readonly GeoPointModel HoChiMinh = new(10.7769, 106.7009, "VN");
    private static readonly GeoPointModel HaNoi = new(21.0285, 105.8542, "VN");
    private static readonly GeoPointModel Nanning = new(22.8170, 108.3665, "CN");

    // ---------- GeoMath ----------

    [Fact]
    public void DecodePolyline_DecodesGoogleSample()
    {
        var points = GeoMath.DecodePolyline(SamplePolyline);

        Assert.Equal(3, points.Count);
        Assert.Equal(38.5, points[0].Lat, 5);
        Assert.Equal(-120.2, points[0].Lng, 5);
        Assert.Equal(40.7, points[1].Lat, 5);
        Assert.Equal(-120.95, points[1].Lng, 5);
        Assert.Equal(43.252, points[2].Lat, 5);
        Assert.Equal(-126.453, points[2].Lng, 5);
    }

    [Fact]
    public void DecodePolyline_EmptyGivesNoPoints()
    {
        Assert.Empty(GeoMath.DecodePolyline(null));
        Assert.Empty(GeoMath.DecodePolyline(""));
    }

    [Fact]
    public void Haversine_HoChiMinhToHanoiIsAbout1140Km()
    {
        double km = GeoMath.HaversineKm(HoChiMinh, HaNoi);

        Assert.InRange(km, 1130, 1150);
    }

    [Fact]
    public void Simplify_KeepsEndpointsAndLimitsCount()
    {
        var line = Enumerable.Range(0, 1000).Select(i => new GeoPointModel(10, 100 + i * 0.01)).ToList();

        var simple = GeoMath.Simplify(line, 50);

        Assert.True(simple.Count <= 50);
        Assert.Same(line[0], simple[0]);
        Assert.Same(line[^1], simple[^1]);
    }

    // ---------- Từng nhà cung cấp ----------

    [Fact]
    public async Task Goong_Search_ParsesResultsAsVietnamAndSendsKey()
    {
        var handler = new FakeHandler(_ => Json(new
        {
            results = new[]
            {
                new { formatted_address = "Quận 1, TP. Hồ Chí Minh", geometry = new { location = new { lat = 10.7769, lng = 106.7009 } } }
            }
        }));

        var places = await Goong(handler).SearchAsync("quan 1", "VN", 5, default);

        Assert.NotNull(places);
        var place = Assert.Single(places!);
        Assert.Equal("Quận 1, TP. Hồ Chí Minh", place.DisplayName);
        Assert.Equal("VN", place.CountryCode);
        Assert.Equal("Goong", place.Provider);
        Assert.Contains("api_key=test-key", handler.Requests[0].RequestUri!.Query);
        Assert.StartsWith("https://goong.test/geocode", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public void Goong_WithoutKey_SupportsNothing()
    {
        var goong = Goong(new FakeHandler(_ => Fail()), apiKey: "");

        Assert.False(goong.Supports("VN"));
        Assert.False(goong.Supports("VN", "VN"));
    }

    [Fact]
    public void Goong_OnlyVietnam()
    {
        var goong = Goong(new FakeHandler(_ => Fail()));

        Assert.True(goong.Supports("VN"));
        Assert.False(goong.Supports(null));
        Assert.False(goong.Supports("CN"));
        Assert.True(goong.Supports("VN", "VN"));
        Assert.False(goong.Supports("VN", "CN"));
    }

    [Fact]
    public async Task Goong_Route_SumsLegsAndDecodesLine()
    {
        var handler = new FakeHandler(_ => Json(GoongRoute(1_720_000, 30 * 3600)));

        var leg = await Goong(handler).RouteLegAsync(HoChiMinh, HaNoi, default);

        Assert.NotNull(leg);
        Assert.Equal(1720m, leg!.DistanceKm);
        Assert.Equal(30m, leg.DurationHours);
        Assert.Equal(3, leg.Polyline.Count);
        Assert.Contains("vehicle=truck", handler.Requests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task Nominatim_Search_ParsesStringCoordinatesAndSendsUserAgent()
    {
        var handler = new FakeHandler(_ => Json(new[]
        {
            new { display_name = "Nanning, Guangxi, China", lat = "22.8170", lon = "108.3665", address = new { country_code = "cn" } }
        }));

        var places = await Nominatim(handler).SearchAsync("Nanning", "CN", 5, default);

        var place = Assert.Single(places!);
        Assert.Equal(22.817, place.Lat, 4);
        Assert.Equal(108.3665, place.Lng, 4);
        Assert.Equal("CN", place.CountryCode);
        var request = handler.Requests[0];
        Assert.Contains("countrycodes=cn", request.RequestUri!.Query);
        Assert.Contains("RacehorseTransport", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task Nominatim_ReverseAtSea_ReturnsNull()
    {
        var handler = new FakeHandler(_ => Json(new { error = "Unable to geocode" }));

        Assert.Null(await Nominatim(handler).ReverseAsync(15, 112, default));
    }

    [Fact]
    public async Task Osrm_Route_UsesLngLatOrderAndParses()
    {
        var handler = new FakeHandler(_ => Json(OsrmRoute(350_500, 5.5 * 3600)));

        var leg = await Osrm(handler).RouteLegAsync(HaNoi, Nanning, default);

        Assert.Equal(350.5m, leg!.DistanceKm);
        Assert.Equal(5.5m, leg.DurationHours);
        Assert.Contains("/route/v1/driving/105.8542,21.0285;108.3665,22.817", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task Osrm_NoRoute_ReturnsNull()
    {
        var handler = new FakeHandler(_ => Json(new { code = "NoRoute", routes = Array.Empty<object>() }));

        Assert.Null(await Osrm(handler).RouteLegAsync(HaNoi, Nanning, default));
    }

    // ---------- GeoService: chọn nhà cung cấp, dự phòng ----------

    [Fact]
    public async Task Route_InsideVietnam_UsesGoong()
    {
        var goong = new FakeHandler(_ => Json(GoongRoute(1_720_000, 30 * 3600)));
        var osrm = new FakeHandler(_ => Fail());
        var service = Service(Goong(goong), Nominatim(new FakeHandler(_ => Fail())), Osrm(osrm));

        var route = await service.RouteAsync(new[] { HoChiMinh, HaNoi });

        var leg = Assert.Single(route.Legs);
        Assert.Equal("Goong", leg.Provider);
        Assert.False(route.IsEstimate);
        Assert.Equal(1720m, route.TotalDistanceKm);
        Assert.Empty(osrm.Requests);
    }

    [Fact]
    public async Task Route_CrossBorder_UsesOsrm()
    {
        var goong = new FakeHandler(_ => Fail());
        var osrm = new FakeHandler(_ => Json(OsrmRoute(350_000, 5 * 3600)));
        var service = Service(Goong(goong), Nominatim(new FakeHandler(_ => Fail())), Osrm(osrm));

        var route = await service.RouteAsync(new[] { HaNoi, Nanning });

        Assert.Equal("Osrm", route.Legs[0].Provider);
        Assert.Empty(goong.Requests);
    }

    [Fact]
    public async Task Route_GoongFails_FallsBackToOsrm()
    {
        var goong = new FakeHandler(_ => Fail());
        var osrm = new FakeHandler(_ => Json(OsrmRoute(1_700_000, 28 * 3600)));
        var service = Service(Goong(goong), Nominatim(new FakeHandler(_ => Fail())), Osrm(osrm));

        var route = await service.RouteAsync(new[] { HoChiMinh, HaNoi });

        Assert.Single(goong.Requests);
        Assert.Equal("Osrm", route.Legs[0].Provider);
        Assert.False(route.IsEstimate);
    }

    [Fact]
    public async Task Route_AllProvidersFail_EstimatesFromStraightLine()
    {
        var service = Service(Goong(new FakeHandler(_ => Fail())), Nominatim(new FakeHandler(_ => Fail())), Osrm(new FakeHandler(_ => Fail())));

        var route = await service.RouteAsync(new[] { HoChiMinh, HaNoi });

        var leg = Assert.Single(route.Legs);
        Assert.True(route.IsEstimate);
        Assert.Equal(GeoService.EstimateProvider, leg.Provider);
        decimal expectedKm = Math.Round((decimal)GeoMath.HaversineKm(HoChiMinh, HaNoi) * 1.3m, 2);
        Assert.Equal(expectedKm, leg.DistanceKm);
        Assert.Equal(Math.Round(expectedKm / 55m, 2), leg.DurationHours);
        Assert.Equal(2, leg.Polyline.Count);
    }

    [Fact]
    public async Task Route_ThreePoints_SumsLegs()
    {
        var osrm = new FakeHandler(_ => Json(OsrmRoute(100_000, 3600)));
        var service = Service(Goong(new FakeHandler(_ => Fail()), apiKey: ""), Nominatim(new FakeHandler(_ => Fail())), Osrm(osrm));

        var route = await service.RouteAsync(new[] { HoChiMinh, HaNoi, Nanning });

        Assert.Equal(2, route.Legs.Count);
        Assert.Equal(0, route.Legs[0].FromIndex);
        Assert.Equal(2, route.Legs[1].ToIndex);
        Assert.Equal(200m, route.TotalDistanceKm);
        Assert.Equal(2m, route.TotalDurationHours);
    }

    [Fact]
    public async Task Route_SecondCallComesFromCache()
    {
        var osrm = new FakeHandler(_ => Json(OsrmRoute(350_000, 5 * 3600)));
        var cache = new MemoryCache();
        var service = Service(Goong(new FakeHandler(_ => Fail())), Nominatim(new FakeHandler(_ => Fail())), Osrm(osrm), cache);

        await service.RouteAsync(new[] { new GeoPointModel(HaNoi.Lat, HaNoi.Lng, "VN"), new GeoPointModel(Nanning.Lat, Nanning.Lng, "CN") });
        await service.RouteAsync(new[] { new GeoPointModel(HaNoi.Lat, HaNoi.Lng, "VN"), new GeoPointModel(Nanning.Lat, Nanning.Lng, "CN") });

        Assert.Single(osrm.Requests);
    }

    [Fact]
    public async Task Route_PointWithoutCountry_IsReverseGeocodedFirst()
    {
        var nominatim = new FakeHandler(_ => Json(new { display_name = "Hà Nội", lat = "21.0285", lon = "105.8542", address = new { country_code = "vn" } }));
        var goong = new FakeHandler(req => req.RequestUri!.AbsolutePath.Contains("Direction")
            ? Json(GoongRoute(1_720_000, 30 * 3600))
            : Json(new { results = Array.Empty<object>() }));
        var service = Service(Goong(goong), Nominatim(nominatim), Osrm(new FakeHandler(_ => Fail())));

        var route = await service.RouteAsync(new[] { HoChiMinh, new GeoPointModel(HaNoi.Lat, HaNoi.Lng) });

        Assert.Equal("Goong", route.Legs[0].Provider); // nước của điểm cuối tra ra VN -> chặng VN-VN dùng Goong
        Assert.Single(nominatim.Requests);
    }

    [Fact]
    public async Task Route_NeedsTwoPoints()
    {
        var service = Service(Goong(new FakeHandler(_ => Fail())), Nominatim(new FakeHandler(_ => Fail())), Osrm(new FakeHandler(_ => Fail())));

        await Assert.ThrowsAsync<BusinessException>(() => service.RouteAsync(new[] { HoChiMinh }));
    }

    [Fact]
    public async Task Search_Vietnam_GoongFails_FallsBackToNominatim()
    {
        var nominatim = new FakeHandler(_ => Json(new[]
        {
            new { display_name = "Quận 1", lat = "10.7769", lon = "106.7009", address = new { country_code = "vn" } }
        }));
        var service = Service(Goong(new FakeHandler(_ => Fail())), Nominatim(nominatim), Osrm(new FakeHandler(_ => Fail())));

        var places = await service.SearchAsync("quan 1", "vn");

        Assert.Equal("Nominatim", Assert.Single(places).Provider);
    }

    [Fact]
    public async Task Search_InvalidCountry_IsRejected()
    {
        var service = Service(Goong(new FakeHandler(_ => Fail())), Nominatim(new FakeHandler(_ => Fail())), Osrm(new FakeHandler(_ => Fail())));

        await Assert.ThrowsAsync<BusinessException>(() => service.SearchAsync("hanoi", "VNM"));
    }

    [Fact]
    public async Task Reverse_InVietnam_UsesNominatimCountryAndGoongAddress()
    {
        var nominatim = new FakeHandler(_ => Json(new { display_name = "Hanoi, Vietnam", lat = "21.03", lon = "105.85", address = new { country_code = "vn" } }));
        var goong = new FakeHandler(_ => Json(new
        {
            results = new[] { new { formatted_address = "Hoàn Kiếm, Hà Nội", geometry = new { location = new { lat = 21.03, lng = 105.85 } } } }
        }));
        var service = Service(Goong(goong), Nominatim(nominatim), Osrm(new FakeHandler(_ => Fail())));

        var place = await service.ReverseAsync(21.0285, 105.8542);

        Assert.Equal("Hoàn Kiếm, Hà Nội", place!.DisplayName);
        Assert.Equal("VN", place.CountryCode);
        Assert.Equal(21.0285, place.Lat); // giữ đúng điểm người dùng chọn
    }

    [Fact]
    public async Task Reverse_OutsideVietnam_DoesNotCallGoong()
    {
        var nominatim = new FakeHandler(_ => Json(new { display_name = "Nanning", lat = "22.8", lon = "108.3", address = new { country_code = "cn" } }));
        var goong = new FakeHandler(_ => Fail());
        var service = Service(Goong(goong), Nominatim(nominatim), Osrm(new FakeHandler(_ => Fail())));

        var place = await service.ReverseAsync(22.817, 108.3665);

        Assert.Equal("CN", place!.CountryCode);
        Assert.Empty(goong.Requests);
    }

    // ---------- Các nước đi qua ----------

    // Đường thẳng theo kinh độ, mỗi điểm cách nhau ~10 km; nước được quyết định theo kinh độ
    private static List<GeoPointModel> Line(double fromLng, double toLng)
    {
        var points = new List<GeoPointModel>();
        for (double lng = fromLng; lng <= toLng + 1e-9; lng += 0.1)
        {
            points.Add(new GeoPointModel(15, lng));
        }
        return points;
    }

    private static Func<GeoPointModel, Task<string?>> CountryByLng(params (double fromLng, string? code)[] zones) =>
        p => Task.FromResult(zones.Last(z => p.Lng >= z.fromLng - 1e-9).code);

    [Fact]
    public async Task CountriesAlong_FindsCountriesInOrderAndCollapsesRepeats()
    {
        var line = Line(100, 110); // ~1070 km
        var lookup = CountryByLng((100, "TH"), (103, "KH"), (106, "VN"));

        var countries = await GeoService.CountriesAlongAsync(line, lookup, trustKm: 300, minSegmentKm: 20, maxLookups: 100);

        Assert.Equal(new[] { "TH", "KH", "VN" }, countries);
    }

    [Fact]
    public async Task CountriesAlong_FindsNarrowCountryBetweenDifferentEnds()
    {
        var line = Line(100, 104);
        var lookup = CountryByLng((100, "VN"), (101.5, "LA"), (102.0, "TH")); // Lào chỉ dài ~50 km

        var countries = await GeoService.CountriesAlongAsync(line, lookup, trustKm: 300, minSegmentKm: 20, maxLookups: 100);

        Assert.Equal(new[] { "VN", "LA", "TH" }, countries);
    }

    [Fact]
    public async Task CountriesAlong_DomesticRoute_NeedsFewLookups()
    {
        var line = Line(100, 116); // ~1700 km trong một nước
        int calls = 0;
        Func<GeoPointModel, Task<string?>> lookup = _ => { calls++; return Task.FromResult<string?>("VN"); };

        var countries = await GeoService.CountriesAlongAsync(line, lookup, trustKm: 300, minSegmentKm: 20, maxLookups: 100);

        Assert.Equal(new[] { "VN" }, countries);
        Assert.True(calls <= 12, $"used {calls} lookups");
    }

    [Fact]
    public async Task CountriesAlong_SkipsPointsAtSea()
    {
        var line = Line(100, 103);
        var lookup = CountryByLng((100, "VN"), (101, null), (102, "PH"));

        var countries = await GeoService.CountriesAlongAsync(line, lookup, trustKm: 300, minSegmentKm: 20, maxLookups: 100);

        Assert.Equal(new[] { "VN", "PH" }, countries);
    }

    [Fact]
    public async Task CountriesAlong_RespectsLookupLimit()
    {
        var line = Line(100, 116);
        int calls = 0;
        Func<GeoPointModel, Task<string?>> lookup = p => { calls++; return Task.FromResult<string?>(((int)p.Lng % 2 == 0) ? "AA" : "BB"); };

        await GeoService.CountriesAlongAsync(line, lookup, trustKm: 300, minSegmentKm: 1, maxLookups: 10);

        Assert.True(calls <= 11, $"used {calls} lookups");
    }

    [Fact]
    public async Task CountriesAlong_EmptyAndSinglePoint()
    {
        var lookup = CountryByLng((0, "VN"));

        Assert.Empty(await GeoService.CountriesAlongAsync(new List<GeoPointModel>(), lookup, 300, 20, 10));
        Assert.Equal(new[] { "VN" }, await GeoService.CountriesAlongAsync(new[] { new GeoPointModel(10, 106) }, lookup, 300, 20, 10));
    }

    [Fact]
    public async Task CountriesAlong_StopsWhenBudgetExpires_KeepsKnownEnds()
    {
        var line = Line(100, 110);
        line[0].CountryCode = "TH";
        line[^1].CountryCode = "VN";
        using var stop = new CancellationTokenSource();
        int calls = 0;
        Func<GeoPointModel, Task<string?>> lookup = _ =>
        {
            calls++;
            stop.Cancel(); // hết giờ ngay sau lượt tra đầu tiên
            return Task.FromResult<string?>("KH");
        };

        var countries = await GeoService.CountriesAlongAsync(line, lookup, trustKm: 300, minSegmentKm: 20, maxLookups: 100, stop.Token);

        Assert.Equal(1, calls);
        Assert.Equal(new[] { "TH", "KH", "VN" }, countries);
    }

    [Fact]
    public async Task CountriesAlong_CancelledLookup_ReturnsKnownCountriesWithoutThrowing()
    {
        var line = Line(100, 110);
        line[0].CountryCode = "TH";
        line[^1].CountryCode = "VN";
        using var stop = new CancellationTokenSource();
        Func<GeoPointModel, Task<string?>> lookup = _ =>
        {
            stop.Cancel();
            throw new OperationCanceledException(stop.Token);
        };

        var countries = await GeoService.CountriesAlongAsync(line, lookup, trustKm: 300, minSegmentKm: 20, maxLookups: 100, stop.Token);

        Assert.Equal(new[] { "TH", "VN" }, countries);
    }

    [Fact]
    public async Task CountriesAlong_KnownEndsDoNotUseLookups()
    {
        var line = Line(100, 101);
        line[0].CountryCode = "VN";
        line[^1].CountryCode = "VN";
        int calls = 0;
        Func<GeoPointModel, Task<string?>> lookup = _ => { calls++; return Task.FromResult<string?>("VN"); };

        var countries = await GeoService.CountriesAlongAsync(line, lookup, trustKm: 300, minSegmentKm: 50, maxLookups: 12);

        Assert.Equal(new[] { "VN" }, countries);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CountryCodeAt_UsesNominatimOnly_AndCachesByGridCell()
    {
        var goongHandler = new FakeHandler(_ => Fail());
        var nominatimHandler = new FakeHandler(_ => Json(new { lat = "15.1", lon = "105.1", display_name = "Laos", address = new { country_code = "la" } }));
        var service = Service(Goong(goongHandler), Nominatim(nominatimHandler), Osrm(new FakeHandler(_ => Fail())));

        Assert.Equal("LA", await service.CountryCodeAtAsync(15.1, 105.1));
        Assert.Equal("LA", await service.CountryCodeAtAsync(15.3, 105.4)); // cùng ô 0,5°

        Assert.Single(nominatimHandler.Requests);
        Assert.Contains("zoom=3", nominatimHandler.Requests[0].RequestUri!.Query);
        Assert.Empty(goongHandler.Requests);
    }

    [Fact]
    public void GridCell_RoundsDownToCell()
    {
        Assert.Equal(GeoService.GridCell(15.1, 105.4, 0.5), GeoService.GridCell(15.4, 105.0, 0.5));
        Assert.NotEqual(GeoService.GridCell(15.4, 105.0, 0.5), GeoService.GridCell(15.6, 105.0, 0.5));
        Assert.Equal("-0.5,-1", GeoService.GridCell(-0.1, -0.6, 0.5));
    }
}
