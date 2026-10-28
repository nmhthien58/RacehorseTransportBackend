using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Helpers;
using Xunit;

namespace RacehorseTransport.Tests;

// R-18: tuyến A -> D đặt được bằng phương thức nào
public class ServiceRouteRulesTests
{
    private static ServiceRouteRules.CountryState Country(
        string code, bool active = true, bool ground = false, bool air = false, int airports = 0) =>
        new(code, active, ground, air, airports);

    [Fact]
    public void Ground_IsAvailable_WhenDestinationHasActiveGroundFreight()
    {
        var problem = ServiceRouteRules.Problem(
            TransportModes.Ground, "VN", Country("VN"), "SG", Country("SG", ground: true));

        Assert.Null(problem);
    }

    [Fact]
    public void Ground_IsRejected_WhenDestinationHasNoGroundFreight()
    {
        var problem = ServiceRouteRules.Problem(
            TransportModes.Ground, "VN", Country("VN"), "JP", Country("JP", air: true, airports: 3));

        Assert.Contains("Ground transport", problem);
    }

    [Fact]
    public void Air_NeedsAnActiveAirportAtBothEnds()
    {
        var destination = Country("SG", air: true, airports: 1);

        Assert.Null(ServiceRouteRules.Problem(TransportModes.Air, "VN", Country("VN", airports: 3), "SG", destination));
        Assert.Contains("no airport", ServiceRouteRules.Problem(TransportModes.Air, "LA", Country("LA"), "SG", destination));
        Assert.Contains("Air transport", ServiceRouteRules.Problem(
            TransportModes.Air, "VN", Country("VN", airports: 3), "SG", Country("SG", air: true, airports: 0)));
    }

    [Fact]
    public void InactiveOrUnknownCountries_AreRejected()
    {
        Assert.Contains("pick up", ServiceRouteRules.Problem(
            TransportModes.Ground, "XX", null, "SG", Country("SG", ground: true)));
        Assert.Contains("pick up", ServiceRouteRules.Problem(
            TransportModes.Ground, "VN", Country("VN", active: false), "SG", Country("SG", ground: true)));
        Assert.Contains("deliver", ServiceRouteRules.Problem(
            TransportModes.Ground, "VN", Country("VN"), "SG", Country("SG", active: false, ground: true)));
    }

    [Fact]
    public void TransitOnlyCountry_IsNotADestination()
    {
        // Kazakhstan: có giấy quá cảnh nhưng không có cước nên không nhận làm nước đến
        var kazakhstan = Country("KZ");

        Assert.False(ServiceRouteRules.GroundAvailable(kazakhstan));
        Assert.False(ServiceRouteRules.AirAvailable(kazakhstan));
    }
}
