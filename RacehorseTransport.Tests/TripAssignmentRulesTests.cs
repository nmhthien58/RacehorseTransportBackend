using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class TripAssignmentRulesTests
{
    [Fact]
    public void EightDrivingHours_AllowsOneDriver()
    {
        TripAssignmentRules.EnsureDriverCount(8m, 1, 9m);
    }

    [Fact]
    public void TenDrivingHours_RejectsOneDriver()
    {
        var error = Assert.Throws<BusinessException>(() =>
            TripAssignmentRules.EnsureDriverCount(10m, 1, 9m));

        Assert.Contains("at least 2 drivers", error.Message);
    }

    [Fact]
    public void TenDrivingHours_AllowsTwoDrivers()
    {
        TripAssignmentRules.EnsureDriverCount(10m, 2, 9m);
    }

    [Fact]
    public void CountryOutsideApprovedRoute_IsRejected()
    {
        var error = Assert.Throws<BusinessException>(() =>
            TripAssignmentRules.EnsureCountriesAreApproved(
                "BKG-2026-0001",
                new[] { "VN", "CN", "LA", "TH" },
                new[] { "VN", "LA", "TH" }));

        Assert.Contains("CN", error.Message);
        Assert.Contains("outside its approved route", error.Message);
    }

    [Fact]
    public void CountriesWithinApprovedRoute_AreAllowed()
    {
        TripAssignmentRules.EnsureCountriesAreApproved(
            "BKG-2026-0001",
            new[] { "vn", "LA", "TH" },
            new[] { "VN", "LA", "TH" });
    }
}
