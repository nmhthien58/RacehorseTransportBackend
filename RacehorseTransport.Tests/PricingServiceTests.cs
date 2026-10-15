using System.Linq.Expressions;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;
using RacehorseTransport.Services.Interfaces;
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
        DateTimeOffset departure = isExpress
            ? DateTimeOffset.UtcNow.AddHours(12)
            : DateTimeOffset.UtcNow.AddDays(30);

        var input = CreateInput(
            new[] { StallClasses.Shared },
            departure,
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

    private static PricingService CreateService()
    {
        var prices = new List<PriceItem>
        {
            Price("FREIGHT", PriceCategories.Freight, PriceCalcTypes.PerHorse, 1000m, TransportModes.Air, "TH"),
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
            Price("ESCORT", PriceCategories.Escort, PriceCalcTypes.PerGroom, 300m, TransportModes.Air),
            Price("INS_TRANSIT", PriceCategories.Insurance, PriceCalcTypes.Percent, 1m)
        };

        // PricingService chỉ cần kho dữ liệu khi bộ nhớ đệm chưa có dữ liệu. Kiểm thử đơn vị cung cấp
        // sẵn bảng giá qua bộ nhớ đệm để tập trung kiểm tra đúng công thức nghiệp vụ.
            return new PricingService(
                 new UnusedRepository<PriceItem>(),
                 new SeededPriceCache(prices),
                 new UnusedDistanceService());
    }

    private static QuoteInputModel CreateInput(
        IEnumerable<string> stallClasses,
        DateTimeOffset? departure = null,
        bool requiresClimateControl = false)
    {
        DateTimeOffset departureDate = departure ?? DateTimeOffset.UtcNow.AddDays(30);

        return new QuoteInputModel
        {
            TransportMode = TransportModes.Air,
            DropoffCountryCode = "TH",
            DepartureDate = departureDate,
            DeliveryDate = departureDate.AddDays(1),
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

    private sealed class UnusedRepository<T> : IRepository<T> where T : class
    {
        public Task<DistanceEstimateResultModel> EstimateGroundDistanceAsync(
        DistanceEstimateInputModel input,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Distance service must not be called for air-pricing tests.");
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
