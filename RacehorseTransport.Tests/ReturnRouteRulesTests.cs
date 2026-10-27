using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class ReturnRouteRulesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

    // Origin(0h) -> Rest(3h, 100km, nghỉ 60') -> Gate(6h, 150km) -> Destination(10h, 250km)
    private static List<RouteCheckpoint> Route() => new()
    {
        new() { CheckpointId = 1, SequenceOrder = 1, CheckpointName = "Ba Vì", CheckpointType = CheckpointTypes.Origin, PlannedTime = T0, Status = CheckpointStatuses.Departed, ActualArrivalTime = T0, ActualDepartureTime = T0, IsActive = true },
        new() { CheckpointId = 2, SequenceOrder = 2, CheckpointName = "Trạm nghỉ", CheckpointType = CheckpointTypes.RestStop, PlannedTime = T0.AddHours(3), DistanceKmFromPrevious = 100, MandatoryRestMinutes = 60, Status = CheckpointStatuses.Pending, IsActive = true },
        new() { CheckpointId = 3, SequenceOrder = 3, CheckpointName = "Hữu Nghị", CheckpointType = CheckpointTypes.BorderGate, PlannedTime = T0.AddHours(6), DistanceKmFromPrevious = 150, Status = CheckpointStatuses.Pending, IsActive = true },
        new() { CheckpointId = 4, SequenceOrder = 4, CheckpointName = "Nam Ninh", CheckpointType = CheckpointTypes.Destination, PlannedTime = T0.AddHours(10), DistanceKmFromPrevious = 250, Status = CheckpointStatuses.Pending, IsActive = true },
    };

    [Fact]
    public void Build_AtBorderGate_ReturnsThroughRestStopToOrigin()
    {
        var route = Route();
        route[1].ActualArrivalTime = T0.AddHours(3); route[1].ActualDepartureTime = T0.AddHours(4); route[1].Status = CheckpointStatuses.Departed;
        route[2].ActualArrivalTime = T0.AddHours(6); route[2].Status = CheckpointStatuses.Arrived; // bị từ chối tại cửa khẩu
        var now = T0.AddHours(8);

        var plan = ReturnRouteRules.Build(route, now);

        Assert.Equal(3, plan.TurnAround.CheckpointId);
        Assert.Equal(new[] { 4 }, plan.Deactivate.Select(c => c.CheckpointId));
        Assert.Equal(2, plan.NewStops.Count);
        Assert.Equal("Trạm nghỉ (quay về)", plan.NewStops[0].Name);
        Assert.Equal(CheckpointTypes.RestStop, plan.NewStops[0].CheckpointType);
        Assert.Equal(150, plan.NewStops[0].DistanceKmFromPrevious);
        Assert.Equal(now.AddHours(3), plan.NewStops[0].PlannedTime);
        Assert.Equal("Ba Vì (quay về)", plan.NewStops[1].Name);
        Assert.Equal(CheckpointTypes.Destination, plan.NewStops[1].CheckpointType);
        Assert.Equal(100, plan.NewStops[1].DistanceKmFromPrevious);
        Assert.Equal(now.AddHours(3).AddMinutes(60).AddHours(3), plan.NewStops[1].PlannedTime);
    }

    [Fact]
    public void Build_NotStarted_Throws()
    {
        var route = Route();
        route[0].ActualArrivalTime = null;
        Assert.Throws<BusinessException>(() => ReturnRouteRules.Build(route, T0));
    }

    [Fact]
    public void UnusedRatio_ByDistance()
    {
        var route = Route();
        // Quay đầu ở cửa khẩu: còn 250 km chưa đi / 500 km cả chặng
        Assert.Equal(0.5m, ReturnRouteRules.UnusedRatio(route, route[0], route[3], route[2]));
    }

    [Fact]
    public void UnusedRatio_NotPickedUpYet_IsOne_AndAfterDropoff_IsZero()
    {
        var route = Route();
        Assert.Equal(1m, ReturnRouteRules.UnusedRatio(route, route[2], route[3], route[1]));
        Assert.Equal(0m, ReturnRouteRules.UnusedRatio(route, route[0], route[1], route[2]));
    }

    [Fact]
    public void UnusedRatio_WithoutKm_UsesPlannedTime()
    {
        var route = Route();
        route.ForEach(c => c.DistanceKmFromPrevious = null);
        Assert.Equal(0.4m, ReturnRouteRules.UnusedRatio(route, route[0], route[3], route[2])); // 4h / 10h
    }

    [Theory]
    [InlineData(FaultParties.Company, 3000)]      // hoàn 100% đã trả
    [InlineData(FaultParties.Customer, 0)]        // không hoàn
    [InlineData(FaultParties.ForceMajeure, 1000)] // 2000 cước × 50%
    public void RefundAmount_ByFaultParty(string fault, decimal expected)
    {
        Assert.Equal(expected, ReturnRouteRules.RefundAmount(fault, freight: 2000, refundable: 3000, unusedRatio: 0.5m));
    }

    [Fact]
    public void RefundAmount_NeverAboveRefundable()
    {
        Assert.Equal(500m, ReturnRouteRules.RefundAmount(FaultParties.ForceMajeure, 2000, 500, 1m));
    }

    [Theory]
    [InlineData(FaultParties.Company, 500)]       // 3000 cước / 3 ngựa × 50% chưa đi
    [InlineData(FaultParties.ForceMajeure, 500)]
    [InlineData(FaultParties.Customer, 0)]
    public void OffloadRefund_UnusedShareOfOneHorse(string fault, decimal expected)
    {
        Assert.Equal(expected, ReturnRouteRules.OffloadRefundAmount(fault, bookingFreight: 3000, horsesInBooking: 3, unusedRatio: 0.5m, refundable: 5000));
        Assert.Equal(Math.Min(expected, 100m), ReturnRouteRules.OffloadRefundAmount(fault, 3000, 3, 0.5m, refundable: 100));
    }

    [Theory]
    [InlineData(1, 1)]   // 1 giờ -> 1 ngày
    [InlineData(24, 1)]
    [InlineData(25, 2)]
    public void StableFee_RoundsUpDays(int hours, int days)
    {
        Assert.Equal(days * 2 * 50m, ReturnRouteRules.StableFee(T0, T0.AddHours(hours), horses: 2, feePerHorsePerDay: 50));
    }
}
