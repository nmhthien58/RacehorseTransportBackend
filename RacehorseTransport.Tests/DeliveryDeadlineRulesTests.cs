using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class DeliveryDeadlineRulesTests
{
    private static readonly TimeSpan Vn = TimeSpan.FromHours(7);
    // Hết ngày 20/11 giờ VN
    private static readonly DateTimeOffset PickupEnd = new(2026, 11, 21, 0, 0, 0, Vn);

    [Fact]
    public void RoundsUpToEndOfArrivalDay_ThenAddsBuffer()
    {
        // 21/11 00:00 + 37,6 giờ = 22/11 13:36 -> hết ngày 22/11 -> + 2 ngày = 24/11
        Assert.Equal(new DateOnly(2026, 11, 24), DeliveryDeadlineRules.EarliestDeliveryDate(PickupEnd, 37.6m, 2));
    }

    [Theory]
    [InlineData(0, 22)]
    [InlineData(2, 24)]
    [InlineData(3, 25)]
    public void BufferDaysFollowConfiguration(int bufferDays, int expectedDay)
    {
        Assert.Equal(new DateOnly(2026, 11, expectedDay), DeliveryDeadlineRules.EarliestDeliveryDate(PickupEnd, 37.6m, bufferDays));
    }

    [Fact]
    public void ArrivalExactlyAtMidnight_CountsAsPreviousDay()
    {
        // 21/11 00:00 + 24 giờ = 22/11 00:00 = vừa hết ngày 21/11
        Assert.Equal(new DateOnly(2026, 11, 21), DeliveryDeadlineRules.EarliestDeliveryDate(PickupEnd, 24m, 0));
    }

    [Fact]
    public void UsesCustomerTimeZone()
    {
        // Cùng thời điểm nhưng khách ở UTC: 21/11 00:00 VN = 20/11 17:00 UTC; + 10 giờ = 21/11 03:00 UTC
        var utcEnd = PickupEnd.ToOffset(TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 11, 21), DeliveryDeadlineRules.EarliestDeliveryDate(PickupEnd, 10m, 0));
        Assert.Equal(new DateOnly(2026, 11, 21), DeliveryDeadlineRules.EarliestDeliveryDate(utcEnd, 10m, 0));
        Assert.Equal(new DateOnly(2026, 11, 20), DeliveryDeadlineRules.EarliestDeliveryDate(utcEnd, 5m, 0));
    }

    [Fact]
    public void ComparesByDate()
    {
        var earliest = new DateOnly(2026, 11, 24);
        Assert.True(DeliveryDeadlineRules.IsDeadlineAllowed(new DateTimeOffset(2026, 11, 24, 0, 1, 0, Vn), earliest));
        Assert.True(DeliveryDeadlineRules.IsDeadlineAllowed(new DateTimeOffset(2026, 11, 25, 9, 0, 0, Vn), earliest));
        Assert.False(DeliveryDeadlineRules.IsDeadlineAllowed(new DateTimeOffset(2026, 11, 23, 23, 59, 0, Vn), earliest));
    }

    [Fact]
    public void NegativeBufferIsTreatedAsZero()
    {
        Assert.Equal(new DateOnly(2026, 11, 22), DeliveryDeadlineRules.EarliestDeliveryDate(PickupEnd, 37.6m, -1));
    }
}
