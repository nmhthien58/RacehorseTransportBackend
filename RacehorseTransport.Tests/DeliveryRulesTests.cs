using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class DeliveryRulesTests
{
    private static readonly DateTimeOffset Window = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    private const int Free = 2;
    private const int Max = 24;

    private static int Hours(DateTimeOffset arrived, TimeSpan waited, DateTimeOffset? window) =>
        DeliveryRules.WaitingHours(arrived, arrived + waited, window, Free, Max);

    [Fact]
    public void ArrivedBeforeNotifiedWindow_IsFree()
    {
        Assert.Equal(0, Hours(Window.AddMinutes(-1), TimeSpan.FromHours(10), Window));
    }

    [Fact]
    public void NoWindowNotified_IsFree()
    {
        Assert.Equal(0, Hours(Window, TimeSpan.FromHours(10), null));
    }

    [Fact]
    public void TwoHours_IsFree()
    {
        Assert.Equal(0, Hours(Window, TimeSpan.FromHours(2), Window));
    }

    [Fact]
    public void ThreeHoursTen_BillsTwoHours()
    {
        int hours = Hours(Window.AddMinutes(30), new TimeSpan(3, 10, 0), Window);
        Assert.Equal(2, hours);
        Assert.Equal(80m, DeliveryRules.WaitingFee(hours, 40));
    }

    [Fact]
    public void CappedAtMaxHours()
    {
        Assert.Equal(24, Hours(Window, TimeSpan.FromHours(40), Window));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(12.3, 26)] // 13 km × 2
    public void ExtraDistanceCost_RoundsKmUp(decimal km, decimal expected)
    {
        Assert.Equal(expected, DeliveryRules.ExtraDistanceCost(km, 2));
    }
}
