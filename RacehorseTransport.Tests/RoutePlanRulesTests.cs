using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class RoutePlanRulesTests
{
    private const string O = RouteStopTypes.Origin;
    private const string R = RouteStopTypes.RestStop;
    private const string G = RouteStopTypes.BorderGate;
    private const string D = RouteStopTypes.Destination;

    private static List<IReadOnlyList<string>> Legs(params string[][] legs) => legs.Select(l => (IReadOnlyList<string>)l).ToList();

    private static List<string> Names(int count) => Enumerable.Range(0, count).Select(i => $"S{i}").ToList();

    [Fact]
    public void DomesticRoute_NoWarnings()
    {
        var warnings = RoutePlanRules.BorderWarnings(new[] { O, R, D }, Names(3), Legs(new[] { "VN" }, new[] { "VN" }));

        Assert.Empty(warnings);
    }

    [Fact]
    public void CrossingWithoutGate_Warns()
    {
        var warnings = RoutePlanRules.BorderWarnings(new[] { O, D }, Names(2), Legs(new[] { "VN", "CN" }));

        var w = Assert.Single(warnings);
        Assert.Equal(0, w.FromIndex);
        Assert.Equal(1, w.ToIndex);
        Assert.Contains("VN → CN", w.Message);
    }

    [Fact]
    public void CrossingWithGateAtEitherEnd_IsFine()
    {
        // Pickup -> cửa khẩu Hữu Nghị (tra ra CN) -> Nam Ninh
        var warnings = RoutePlanRules.BorderWarnings(new[] { O, G, D }, Names(3), Legs(new[] { "VN", "CN" }, new[] { "CN" }));

        Assert.Empty(warnings);
    }

    [Fact]
    public void LegBetweenTwoGates_MayTouchThreeCountries()
    {
        // VN -> cửa khẩu VN/LA -> cửa khẩu LA/TH -> TH
        var warnings = RoutePlanRules.BorderWarnings(
            new[] { O, G, G, D }, Names(4),
            Legs(new[] { "VN" }, new[] { "VN", "LA", "TH" }, new[] { "TH" }));

        Assert.Empty(warnings);
    }

    [Fact]
    public void CrossingAWholeCountryWithOneGate_Warns()
    {
        // Một cửa khẩu nhưng chặng băng qua cả Lào sang Thái Lan
        var warnings = RoutePlanRules.BorderWarnings(new[] { O, G, D }, Names(3), Legs(new[] { "VN" }, new[] { "VN", "LA", "TH" }));

        var w = Assert.Single(warnings);
        Assert.Equal(1, w.FromIndex);
    }

    [Fact]
    public void TransitCountries_DropOriginAndDestination_KeepOrder()
    {
        var transit = RoutePlanRules.TransitCountries(new[] { "VN", "LA", "VN", "LA", "TH" }, "VN", "TH");

        Assert.Equal(new[] { "LA" }, transit);
    }

    [Fact]
    public void TransitCountries_MultipleInOrder()
    {
        Assert.Equal(new[] { "CN", "KZ" }, RoutePlanRules.TransitCountries(new[] { "VN", "CN", "KZ", "RU" }, "VN", "RU"));
    }

    [Theory]
    [InlineData(new[] { "VN" }, 0)]
    [InlineData(new[] { "VN", "CN" }, 1)]
    [InlineData(new[] { "VN", "LA", "TH" }, 2)]
    public void BorderCrossings_CountsCountryChanges(string[] along, int expected)
    {
        Assert.Equal(expected, RoutePlanRules.BorderCrossings(along));
    }
}
