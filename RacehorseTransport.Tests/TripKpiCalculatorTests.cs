using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class TripKpiCalculatorTests
{
    private static readonly DateTimeOffset PlannedEnd =
        new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, OnTimeStatuses.OnTime, 100)]
    [InlineData(30, OnTimeStatuses.OnTime, 100)]
    [InlineData(31, OnTimeStatuses.DelayedOperationalFault, 95)]
    [InlineData(120, OnTimeStatuses.DelayedOperationalFault, 80)]
    [InlineData(600, OnTimeStatuses.DelayedOperationalFault, 50)]
    public void Calculate_WithoutForceMajeure_ReturnsExpectedKpi(
        int delayMinutes,
        string expectedStatus,
        int expectedScore)
    {
        var result = TripKpiCalculator.Calculate(
            PlannedEnd,
            PlannedEnd.AddMinutes(delayMinutes),
            hasForceMajeure: false);

        Assert.Equal(delayMinutes, result.DelayMinutes);
        Assert.Equal(expectedStatus, result.OnTimeStatus);
        Assert.Equal((decimal)expectedScore, result.KpiScore);
    }

    [Theory]
    [InlineData(0, OnTimeStatuses.OnTime)]
    [InlineData(30, OnTimeStatuses.OnTime)]
    [InlineData(31, OnTimeStatuses.DelayedForceMajeure)]
    [InlineData(120, OnTimeStatuses.DelayedForceMajeure)]
    [InlineData(600, OnTimeStatuses.DelayedForceMajeure)]
    public void Calculate_WithForceMajeure_ReturnsExpectedKpi(
        int delayMinutes,
        string expectedStatus)
    {
        var result = TripKpiCalculator.Calculate(
            PlannedEnd,
            PlannedEnd.AddMinutes(delayMinutes),
            hasForceMajeure: true);

        Assert.Equal(delayMinutes, result.DelayMinutes);
        Assert.Equal(expectedStatus, result.OnTimeStatus);
        Assert.Equal(100m, result.KpiScore);
    }

    [Fact]
    public void Calculate_WhenTripFinishesEarly_DoesNotReturnNegativeDelay()
    {
        var result = TripKpiCalculator.Calculate(
            PlannedEnd,
            PlannedEnd.AddMinutes(-45),
            hasForceMajeure: false);

        Assert.Equal(0, result.DelayMinutes);
        Assert.Equal(OnTimeStatuses.OnTime, result.OnTimeStatus);
        Assert.Equal(100m, result.KpiScore);
    }
}
