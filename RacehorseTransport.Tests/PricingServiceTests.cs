using System.Linq.Expressions;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;
using Xunit;

namespace RacehorseTransport.Tests;

public class PricingServiceTests
{
    [Theory]
    [InlineData(StallClasses.Shared, 1000)]
    [InlineData(StallClasses.Comfort, 1400)]
    [InlineData(StallClasses.Private, 2200)]
    public async Task CalculateQuote_UsesMultiplierForEachStallClass(
        string stallClass,
        int expectedFreight)
    {
        var service = CreateService();
        var input = CreateInput(new[] { stallClass });

        var quote = await service.CalculateQuoteAsync(input);

        var freight = Assert.Single(
            quote.Lines,
            x => x.Code.StartsWith("FREIGHT_", StringComparison.Ordinal));
        Assert.Equal((decimal)expectedFreight, freight.Amount);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 100)]
    [InlineData(4, 400)]
    [InlineData(7, 1050)]
    public async Task CalculateQuote_AppliesHighestVolumeDiscount(
        int horseCount,
        int expectedDiscount)
    {
        var service = CreateService();
        var input = CreateInput(
            Enumerable.Repeat(StallClasses.Shared, horseCount));

        var quote = await service.CalculateQuoteAsync(input);
        var discountLines = quote.Lines
            .Where(x => x.Code.StartsWith("DISC_", StringComparison.Ordinal))
            .ToList();

        if (expectedDiscount == 0)
        {
            Assert.Empty(discountLines);
        }
        else
        {
            var discount = Assert.Single(discountLines);
            Assert.Equal(-(decimal)expectedDiscount, discount.Amount);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CalculateQuote_AddsOnlyApplicableSurcharges(
        bool isExpress,
        bool requiresClimateControl)
    {
        var service = CreateService();
        // Express khi pickup nằm trong ExpressWindowDays ngày tính từ ngày pickup sớm nhất được phép
        DateTimeOffset pickup = isExpress
            ? EarliestPickup()
            : EarliestPickup().AddDays(Options.ExpressWindowDays + 3);

        var input = CreateInput(
            new[] { StallClasses.Shared },
            pickup,
            requiresClimateControl: requiresClimateControl);

        var quote = await service.CalculateQuoteAsync(input);
        var codes = quote.Lines.Select(x => x.Code).ToHashSet();

        Assert.Contains("SUR_FUEL", codes);
        Assert.Equal(80m, Assert.Single(quote.Lines, x => x.Code == "SUR_FUEL").Amount);
        Assert.Equal(isExpress, codes.Contains("SUR_EXPRESS"));
        Assert.Equal(requiresClimateControl, codes.Contains("SUR_CLIMATE"));

        if (isExpress)
        {
            Assert.Equal(200m, Assert.Single(quote.Lines, x => x.Code == "SUR_EXPRESS").Amount);
        }

        if (requiresClimateControl)
        {
            Assert.Equal(100m, Assert.Single(quote.Lines, x => x.Code == "SUR_CLIMATE").Amount);
        }

        Assert.Equal(isExpress, quote.IsExpress);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CalculateQuote_AddsInsuranceOnlyWhenValueIsDeclared(
        bool hasDeclaredValue)
    {
        var service = CreateService();
        var input = CreateInput(new[] { StallClasses.Shared });
        input.DeclaredValue = hasDeclaredValue ? 10_000m : null;

        var quote = await service.CalculateQuoteAsync(input);
        var insuranceLines = quote.Lines
            .Where(x => x.Code == "INS_TRANSIT")
            .ToList();

        if (hasDeclaredValue)
        {
            var insurance = Assert.Single(insuranceLines);
            Assert.Equal(100m, insurance.Amount);
        }
        else
        {
            Assert.Empty(insuranceLines);
        }
    }

    [Fact]
    public async Task CalculateQuote_RejectsPickupBeforeLeadTime()
    {
        var service = CreateService();
        var input = CreateInput(new[] { StallClasses.Shared }, EarliestPickup().AddDays(-1));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CalculateQuoteAsync(input));
        Assert.Contains("PickupWindowStart", ex.Message);
    }

    [Fact]
    public async Task CalculateQuote_RejectsDeadlineShorterThanTransitTime()
    {
        var service = CreateService();
        var input = CreateInput(new[] { StallClasses.Shared }, EarliestPickup().AddDays(10));
        input.DeliveryDeadline = input.PickupWindowStart!.Value.AddHours(5); // bay mất ~ 32 giờ

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CalculateQuoteAsync(input));
        Assert.Contains("DeliveryDeadline", ex.Message);
    }

    [Fact]
    public async Task CalculateQuote_DeadlineMustCoverPickupOnLastDayOfWindow()
    {
        var service = CreateService();
        var input = CreateInput(new[] { StallClasses.Shared }, EarliestPickup().AddDays(10));
        input.PickupWindowEnd = input.PickupWindowStart!.Value.AddDays(3);

        // Đủ nếu đón ngày đầu, nhưng thiếu nếu ngựa được đón vào ngày cuối của khoảng đón
        input.DeliveryDeadline = input.PickupWindowStart.Value.AddDays(2);
        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CalculateQuoteAsync(input));
        Assert.Contains("DeliveryDeadline", ex.Message);

        input.DeliveryDeadline = null;
        var quote = await service.CalculateQuoteAsync(input);
        Assert.True(quote.EarliestDeliveryDeadline >= input.PickupWindowEnd.Value.AddDays(1));
    }

    [Fact]
    public async Task CalculateQuote_TightDeadlineIsExpress()
    {
        var service = CreateService();
        var input = CreateInput(new[] { StallClasses.Shared }, EarliestPickup().AddDays(20));

        var relaxed = await service.CalculateQuoteAsync(input);
        Assert.False(relaxed.IsExpress);

        input.DeliveryDeadline = relaxed.EarliestDeliveryDeadline.AddHours(2);
        var tight = await service.CalculateQuoteAsync(input);
        Assert.True(tight.IsExpress);
    }

    [Fact]
    public async Task CalculateQuote_WithoutDates_ReturnsEarliestPickupAndIsNotExpress()
    {
        var service = CreateService();
        var input = CreateInput(new[] { StallClasses.Shared });
        input.PickupWindowStart = null;
        input.PickupWindowEnd = null;
        input.DeliveryDeadline = null;

        var quote = await service.CalculateQuoteAsync(input);

        Assert.False(quote.IsExpress);
        Assert.Equal(DateOnly.FromDateTime(EarliestPickup().UtcDateTime), quote.EarliestPickupDate);
        Assert.Equal(quote.EarliestPickupDate.AddDays(Options.ExpressWindowDays), quote.ExpressUntilDate);
        Assert.True(quote.Transit.TotalHours > 0);
    }

    private static readonly BookingOptions Options = new();

    private static DateTimeOffset EarliestPickup()
    {
        return new DateTimeOffset(DateTime.UtcNow.Date.AddDays(Options.LeadTimeDays), TimeSpan.Zero);
    }

    [Fact]
    public async Task CalculateQuote_ChargesTransitFeePerCountryAndHorse()
    {
        var service = CreateService();
        var input = CreateInput(new[] { StallClasses.Shared, StallClasses.Shared });
        input.TransitCountryCodes = new List<string> { "my", "SG" };

        var quote = await service.CalculateQuoteAsync(input);

        var fee = Assert.Single(quote.Lines, x => x.Code == "FEE_TRANSIT");
        Assert.Equal(4m, fee.Quantity);      // 2 nước x 2 ngựa
        Assert.Equal(600m, fee.Amount);      // 150 / ngựa / nước
        Assert.Equal(new[] { "MY", "SG" }, quote.TransitCountryCodes);
    }

    [Fact]
    public async Task CalculateQuote_WithoutTransit_HasNoTransitFee()
    {
        var service = CreateService();
        var quote = await service.CalculateQuoteAsync(CreateInput(new[] { StallClasses.Shared }));

        Assert.DoesNotContain(quote.Lines, x => x.Code == "FEE_TRANSIT");
    }

    [Fact]
    public async Task CalculateQuote_RejectsTransitEqualToDestination()
    {
        var service = CreateService();
        var input = CreateInput(new[] { StallClasses.Shared });
        input.TransitCountryCodes = new List<string> { "TH" };

        await Assert.ThrowsAsync<BusinessException>(() => service.CalculateQuoteAsync(input));
    }

    [Fact]
    public async Task CalculateQuote_LeadTimeFollowsRouteClearanceDays()
    {
        var service = CreateService(clearanceDays: 30);
        var input = CreateInput(new[] { StallClasses.Shared });
        input.PickupWindowStart = null;
        input.PickupWindowEnd = null;
        input.DeliveryDeadline = null;

        var quote = await service.CalculateQuoteAsync(input);

        Assert.Equal(30, quote.ClearanceDays);
        Assert.Equal(Options.LeadTimeDaysFor(30), quote.LeadTimeDays);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(Options.LeadTimeDaysFor(30)), quote.EarliestPickupDate);
    }

    [Fact]
    public async Task CalculateQuote_UsesQuotedAtAsReferenceDate()
    {
        var service = CreateService();
        // Đơn tạo 5 ngày trước với ngày pickup sớm nhất lúc đó: tính lại giá hôm nay vẫn hợp lệ
        var createdAt = DateTimeOffset.UtcNow.AddDays(-5);
        var input = CreateInput(new[] { StallClasses.Shared },
            new DateTimeOffset(createdAt.UtcDateTime.Date.AddDays(Options.LeadTimeDays), TimeSpan.Zero));
        input.QuotedAt = createdAt;

        var quote = await service.CalculateQuoteAsync(input);

        Assert.True(quote.Total > 0);
    }

    // ---------- R-10: khoảng giá ước tính (BE-03) ----------

    // Đường bộ, khoảng đón xa (không Express), 1 con chuồng ghép, không bảo hiểm
    private static QuoteInputModel GroundInput(string origin, string destination, bool withCoordinates = true)
    {
        DateTimeOffset pickupStart = EarliestPickup().AddDays(Options.ExpressWindowDays + 30);
        return new QuoteInputModel
        {
            TransportMode = TransportModes.Ground,
            PickupCountryCode = origin,
            DropoffCountryCode = destination,
            PickupLat = withCoordinates ? 21.0285m : null,
            PickupLng = withCoordinates ? 105.8542m : null,
            DropoffLat = withCoordinates ? 13.7563m : null,
            DropoffLng = withCoordinates ? 100.5018m : null,
            PickupWindowStart = pickupStart,
            PickupWindowEnd = pickupStart.AddDays(2),
            DeliveryDeadline = pickupStart.AddDays(40),
            StallClasses = new List<string> { StallClasses.Shared }
        };
    }

    [Fact]
    public async Task QuoteRange_UsesMapDistanceAndCountriesAlongTheRoute()
    {
        var geo = new FakeGeoService(500, "VN", "LA", "TH");
        var service = CreateService(geo: geo);

        var quote = await service.CalculateQuoteRangeAsync(GroundInput("VN", "TH"));

        Assert.Equal(500, quote.DistanceKm);
        Assert.Equal(new[] { "LA" }, quote.TransitCountryCodes);
        Assert.True(geo.Called);
        Assert.DoesNotContain(quote.Lines, l => l.Code == "FEE_TRANSIT"); // giá thấp nhất chưa có phí quá cảnh
        Assert.Equal(quote.Total, quote.QuoteMin);
    }

    [Fact]
    public async Task QuoteRange_MaxAddsLongerDistanceAndTransitFeesOfCountriesAlong()
    {
        var service = CreateService(geo: new FakeGeoService(500, "VN", "LA", "TH"));

        var quote = await service.CalculateQuoteRangeAsync(GroundInput("VN", "TH"));

        // 500 km × 1,25 = 625 km: cước +250 (2 USD/km), phụ phí nhiên liệu 8% trên phần tăng +20, phí quá cảnh Lào 150
        Assert.Equal(quote.QuoteMin + 250 + 20 + 150, quote.QuoteMax);
    }

    [Fact]
    public async Task QuoteRange_MaxCountsAtMostTwoTransitCountries()
    {
        var service = CreateService(geo: new FakeGeoService(500, "VN", "LA", "TH", "MY", "SG"));

        var quote = await service.CalculateQuoteRangeAsync(GroundInput("VN", "SG"));

        Assert.Equal(new[] { "LA", "TH", "MY" }, quote.TransitCountryCodes);
        Assert.Equal(quote.QuoteMin + 250 + 20 + 2 * 150, quote.QuoteMax);
    }

    [Fact]
    public async Task QuoteRange_DomesticRoute_HasNoReserveCountry()
    {
        var service = CreateService(geo: new FakeGeoService(500, "VN"));

        var quote = await service.CalculateQuoteRangeAsync(GroundInput("VN", "VN"));

        Assert.Empty(quote.TransitCountryCodes);
        Assert.Equal(quote.QuoteMin + 250 + 20, quote.QuoteMax);
    }

    [Fact]
    public async Task QuoteRange_WithoutCoordinates_IgnoresTransitCountriesTypedByCustomer()
    {
        var service = CreateService(geo: new FakeGeoService(500, "VN", "LA", "TH"));
        var input = GroundInput("VN", "TH", withCoordinates: false);
        input.DistanceKm = 500;
        input.TransitCountryCodes = new List<string> { "LA" };

        var quote = await service.CalculateQuoteRangeAsync(input);

        Assert.Empty(quote.TransitCountryCodes);
        Assert.DoesNotContain(quote.Lines, l => l.Code == "FEE_TRANSIT");
    }

    [Fact]
    public async Task QuoteRange_Air_MinEqualsMax()
    {
        var service = CreateService(geo: new FakeGeoService(500));

        var quote = await service.CalculateQuoteRangeAsync(CreateInput(new[] { StallClasses.Shared }));

        Assert.Equal(quote.QuoteMin, quote.QuoteMax);
    }

    [Fact]
    public async Task QuoteRange_FlagsEstimatedRoute()
    {
        var service = CreateService(geo: new FakeGeoService(500, "VN") { IsEstimate = true });

        var quote = await service.CalculateQuoteRangeAsync(GroundInput("VN", "VN"));

        Assert.True(quote.RouteIsEstimate);
    }

    // Đơn bay được báo giá chốt ngay khi duyệt sơ bộ: không có tuyến, không có km, cước tính theo con theo nước đến
    [Fact]
    public async Task FinalQuote_AirBookingWithoutRoutePlan_PricedPerHorse()
    {
        var service = CreateService();
        var pickup = EarliestPickup().AddDays(Options.ExpressWindowDays + 10);
        var booking = new BookingRequest
        {
            BookingCode = "BK-AIR",
            TransportMode = TransportModes.Air,
            PickupCountryCode = "VN",
            DropoffCountryCode = "TH",
            PickupWindowStart = pickup,
            PickupWindowEnd = pickup.AddDays(3),
            DeliveryDeadline = pickup.AddDays(10)
        };
        var horses = new List<BookingHorse>
        {
            new() { StallClass = StallClasses.Shared },
            new() { StallClass = StallClasses.Shared }
        };

        var quote = await service.CalculateQuoteAsync(FinalQuoteIssuer.BuildQuoteInput(booking, horses, plan: null));

        var freight = Assert.Single(quote.Lines, l => l.Code == "FREIGHT_SHARED");
        Assert.Equal(2, freight.Quantity);
        Assert.Equal(2000m, freight.Amount);
        Assert.DoesNotContain(quote.Lines, l => l.Code == "FEE_TRANSIT");
    }

    private sealed class FakeGeoService : IGeoService
    {
        private readonly decimal _km;
        private readonly List<string> _countries;
        public bool IsEstimate { get; init; }
        public bool Called { get; private set; }

        public FakeGeoService(decimal km, params string[] countries)
        {
            _km = km;
            _countries = countries.ToList();
        }

        public Task<IReadOnlyList<GeoPlaceModel>> SearchAsync(string query, string? countryCode, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GeoPlaceModel?> ReverseAsync(double lat, double lng, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GeoRouteModel> RouteAsync(IReadOnlyList<GeoPointModel> points, bool includeCountries = false, CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.FromResult(new GeoRouteModel
            {
                TotalDistanceKm = _km,
                IsEstimate = IsEstimate,
                CountryCodes = includeCountries ? _countries : null
            });
        }

        public Task<IReadOnlyList<string>> CountriesAlongAsync(IReadOnlyList<GeoPointModel> polyline, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(_countries);
    }

    private static PricingService CreateService(int? clearanceDays = null, IGeoService? geo = null)
    {
        var prices = new List<PriceItem>
        {
            Price("FREIGHT", PriceCategories.Freight, PriceCalcTypes.PerHorse, 1000m, TransportModes.Air, "TH"),
            Price("FREIGHT", PriceCategories.Freight, PriceCalcTypes.PerKmPerHorse, 2m, TransportModes.Ground),
            Price("STALL_SHARED", PriceCategories.StallClass, PriceCalcTypes.Multiplier, 1m),
            Price("STALL_COMFORT", PriceCategories.StallClass, PriceCalcTypes.Multiplier, 1.4m),
            Price("STALL_PRIVATE", PriceCategories.StallClass, PriceCalcTypes.Multiplier, 2.2m),
            Price("DISC_VOL_2", PriceCategories.Discount, PriceCalcTypes.Percent, 5m, minHorses: 2),
            Price("DISC_VOL_4", PriceCategories.Discount, PriceCalcTypes.Percent, 10m, minHorses: 4),
            Price("DISC_VOL_7", PriceCategories.Discount, PriceCalcTypes.Percent, 15m, minHorses: 7),
            Price("SUR_FUEL", PriceCategories.Surcharge, PriceCalcTypes.Percent, 8m),
            Price("SUR_EXPRESS", PriceCategories.Surcharge, PriceCalcTypes.Percent, 20m),
            Price("SUR_CLIMATE", PriceCategories.Surcharge, PriceCalcTypes.Percent, 10m),
            Price("FEE_DOCS", PriceCategories.Clearance, PriceCalcTypes.PerHorse, 100m),
            Price("FEE_CLEARANCE", PriceCategories.Clearance, PriceCalcTypes.PerHorse, 200m, TransportModes.Air),
            Price("FEE_TRANSIT", PriceCategories.Clearance, PriceCalcTypes.PerHorse, 150m),
            Price("ESCORT", PriceCategories.Escort, PriceCalcTypes.PerGroom, 300m, TransportModes.Air),
            Price("INS_TRANSIT", PriceCategories.Insurance, PriceCalcTypes.Percent, 1m)
        };

        // PricingService chỉ cần kho dữ liệu khi bộ nhớ đệm chưa có dữ liệu. Kiểm thử đơn vị cung cấp
        // sẵn bảng giá qua bộ nhớ đệm để tập trung kiểm tra đúng công thức nghiệp vụ.
        return new PricingService(
            new UnusedRepository<PriceItem>(),
            new SeededPriceCache(prices),
            new UnusedDistanceService(),
            new TransitTimeEstimator(Microsoft.Extensions.Options.Options.Create(new TransitOptions())),
            new FixedClearanceSchedule(clearanceDays ?? Options.DefaultClearanceDays),
            geo ?? new FakeGeoService(0),
            Microsoft.Extensions.Options.Options.Create(Options));
    }

    // Thời gian làm hồ sơ của tuyến cố định cho kiểm thử (bản thật đọc country_doc_requirements.processing_days)
    private sealed class FixedClearanceSchedule : IClearanceScheduleService
    {
        private readonly int _days;

        public FixedClearanceSchedule(int days)
        {
            _days = days;
        }

        public Task<int> GetClearanceDaysAsync(string originCountry, IReadOnlyList<string> transitCountries, string destinationCountry)
            => Task.FromResult(_days);
    }

    // Mặc định: pickup xa hơn mốc Express, hạn giao rộng rãi
    private static QuoteInputModel CreateInput(
        IEnumerable<string> stallClasses,
        DateTimeOffset? pickup = null,
        bool requiresClimateControl = false)
    {
        DateTimeOffset pickupStart = pickup ?? EarliestPickup().AddDays(Options.ExpressWindowDays + 10);

        return new QuoteInputModel
        {
            TransportMode = TransportModes.Air,
            DropoffCountryCode = "TH",
            PickupWindowStart = pickupStart,
            PickupWindowEnd = pickupStart.AddDays(3),
            DeliveryDeadline = pickupStart.AddDays(10),
            RequiresClimateControl = requiresClimateControl,
            StallClasses = stallClasses.ToList()
        };
    }

    private static PriceItem Price(
        string code,
        string category,
        string calcType,
        decimal value,
        string? transportMode = null,
        string? destination = null,
        int? minHorses = null)
    {
        return new PriceItem
        {
            ItemCode = code,
            ItemName = code,
            Category = category,
            CalcType = calcType,
            Value = value,
            TransportMode = transportMode,
            DestCountryCode = destination,
            MinHorses = minHorses,
            IsActive = true
        };
    }

    private sealed class SeededPriceCache : ICacheService
    {
        private readonly List<PriceItem> _prices;

        public SeededPriceCache(List<PriceItem> prices)
        {
            _prices = prices;
        }

        public Task<T?> GetAsync<T>(string key) => Task.FromResult(default(T));

        public Task SetAsync<T>(string key, T value, TimeSpan ttl) => Task.CompletedTask;

        public Task RemoveAsync(string key) => Task.CompletedTask;

        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
        {
            return Task.FromResult((T)(object)_prices);
        }
    }

    // Các ca kiểm thử chỉ dùng đường hàng không nên không được gọi tới dịch vụ tính quãng đường.
    private sealed class UnusedDistanceService : IDistanceService
    {
        public Task<DistanceEstimateResultModel> EstimateGroundDistanceAsync(
            DistanceEstimateInputModel input,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(
                "Distance service must not be called for air-pricing tests.");
    }

    private sealed class UnusedRepository<T> : IRepository<T> where T : class
    {
        private static InvalidOperationException UnexpectedCall() =>
            new("The repository must not be called when the test cache is pre-populated.");

        public Task<T?> GetByIdAsync(object id) => throw UnexpectedCall();
        public Task<IReadOnlyList<T>> GetAllAsync() => throw UnexpectedCall();
        public IQueryable<T> GetQueryable() => throw UnexpectedCall();
        public Task<T> AddAsync(T entity) => throw UnexpectedCall();
        public Task AddRangeAsync(IEnumerable<T> entities) => throw UnexpectedCall();
        public Task UpdateAsync(T entity) => throw UnexpectedCall();
        public Task DeleteAsync(T entity) => throw UnexpectedCall();
        public Task DeleteRangeAsync(IEnumerable<T> entities) => throw UnexpectedCall();
        public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => throw UnexpectedCall();
        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) => throw UnexpectedCall();
        public Task<int> SaveChangesAsync() => throw UnexpectedCall();
    }
}
