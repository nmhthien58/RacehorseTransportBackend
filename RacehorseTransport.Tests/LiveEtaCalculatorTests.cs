using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class LiveEtaCalculatorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 10, 6, 0, 0, TimeSpan.Zero);

    // Origin 06:00 -> RestStop 10:00 (nghỉ 30') -> Destination 16:00
    private static List<RouteCheckpoint> Route() => new()
    {
        new() { CheckpointId = 1, SequenceOrder = 1, CheckpointType = CheckpointTypes.Origin, PlannedTime = T0, IsActive = true },
        new() { CheckpointId = 2, SequenceOrder = 2, CheckpointType = CheckpointTypes.RestStop, PlannedTime = T0.AddHours(4), MandatoryRestMinutes = 30, IsActive = true },
        new() { CheckpointId = 3, SequenceOrder = 3, CheckpointType = CheckpointTypes.Destination, PlannedTime = T0.AddHours(10), IsActive = true },
    };

    [Fact]
    public void BeforeDeparture_KeepsPlan()
    {
        var result = LiveEtaCalculator.Calculate(Route(), TripStatuses.Scheduled, T0.AddHours(-2));

        Assert.Equal(0, result.DelayMinutes);
        Assert.Equal(T0.AddHours(10), result.EstimatedArrivals[3]);
    }

    [Fact]
    public void LateDeparture_ShiftsLaterStops()
    {
        var route = Route();
        route[0].ActualArrivalTime = T0;
        route[0].ActualDepartureTime = T0.AddMinutes(45);

        var result = LiveEtaCalculator.Calculate(route, TripStatuses.InTransit, T0.AddHours(1));

        Assert.Equal(45, result.DelayMinutes);
        Assert.Equal(T0.AddHours(4).AddMinutes(45), result.EstimatedArrivals[2]);
        Assert.Equal(T0.AddHours(10).AddMinutes(45), result.EstimatedArrivals[3]);
    }

    [Fact]
    public void StoppedAtCheckpoint_CountsMandatoryRest()
    {
        var route = Route();
        route[0].ActualArrivalTime = T0;
        route[0].ActualDepartureTime = T0;
        route[1].ActualArrivalTime = T0.AddHours(5); // tới trễ 60'

        // Vừa tới: còn phải nghỉ 30' -> rời sớm nhất 11:30, kế hoạch rời 10:30 -> trễ 60'
        var result = LiveEtaCalculator.Calculate(route, TripStatuses.InTransit, T0.AddHours(5));

        Assert.Equal(60, result.DelayMinutes);
        Assert.Equal(T0.AddHours(5), result.EstimatedArrivals[2]); // mốc đã tới giữ giờ thực tế
        Assert.Equal(T0.AddHours(11), result.EstimatedArrivals[3]);
    }

    [Fact]
    public void StayingLongerThanPlanned_KeepsPushingEta()
    {
        var route = Route();
        route[0].ActualArrivalTime = T0;
        route[0].ActualDepartureTime = T0;
        route[1].ActualArrivalTime = T0.AddHours(4);

        // Đúng giờ tới nhưng 2 tiếng sau vẫn chưa rời -> trễ 90' (kế hoạch rời 10:30)
        var result = LiveEtaCalculator.Calculate(route, TripStatuses.InTransit, T0.AddHours(6));

        Assert.Equal(90, result.DelayMinutes);
    }

    [Fact]
    public void OverdueOnTheRoad_AssumesArrivingNow()
    {
        var route = Route();
        route[0].ActualArrivalTime = T0;
        route[0].ActualDepartureTime = T0;

        // Đã 11:00 mà chưa tới mốc dự kiến 10:00
        var result = LiveEtaCalculator.Calculate(route, TripStatuses.InTransit, T0.AddHours(5));

        Assert.Equal(60, result.DelayMinutes);
        Assert.Equal(T0.AddHours(11), result.EstimatedArrivals[3]);
    }

    [Fact]
    public void EarlyArrival_DoesNotPullEtaForward()
    {
        var route = Route();
        route[0].ActualArrivalTime = T0;
        route[0].ActualDepartureTime = T0;
        route[1].ActualArrivalTime = T0.AddHours(3);
        route[1].ActualDepartureTime = T0.AddHours(3).AddMinutes(30);

        var result = LiveEtaCalculator.Calculate(route, TripStatuses.InTransit, T0.AddHours(4));

        Assert.Equal(0, result.DelayMinutes);
        Assert.Equal(T0.AddHours(10), result.EstimatedArrivals[3]);
    }

    [Fact]
    public void InactiveCheckpointsAreIgnored()
    {
        var route = Route();
        route[1].IsActive = false;
        route[0].ActualArrivalTime = T0;
        route[0].ActualDepartureTime = T0.AddMinutes(20);

        var result = LiveEtaCalculator.Calculate(route, TripStatuses.InTransit, T0.AddMinutes(30));

        Assert.False(result.EstimatedArrivals.ContainsKey(2));
        Assert.Equal(T0.AddHours(10).AddMinutes(20), result.EstimatedArrivals[3]);
    }
}
