using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using RacehorseTransport.Services.Options;
using Xunit;

namespace RacehorseTransport.Tests;

// R-04: gợi ý lộ trình khi ghép nhiều đơn. Quãng đường lấy từ bảng giả, tham số thời gian mặc định
// (xe 55 km/h, xếp / dỡ 1 giờ mỗi điểm, qua cửa khẩu 6 giờ).
public class RouteSuggestionBuilderTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 10, 6, 0, 0, TimeSpan.Zero);
    private static readonly TransitOptions Options = new();

    private static readonly Dictionary<(string, string), int> Km = new()
    {
        [("Hà Nội", "Lạng Sơn")] = 160,
        [("Hà Nội", "Nam Ninh")] = 380,
        [("Lạng Sơn", "Nam Ninh")] = 220,
        [("Hà Nội", "Bắc Ninh")] = 30,
        [("Bắc Ninh", "Nam Ninh")] = 360,
        [("Bắc Ninh", "Lạng Sơn")] = 130
    };

    private static Task<int> FakeDistance(RoutePlace from, RoutePlace to)
    {
        if (Km.TryGetValue((from.Address, to.Address), out int km) || Km.TryGetValue((to.Address, from.Address), out km))
        {
            return Task.FromResult(km);
        }
        throw new InvalidOperationException($"No test distance for {from.Address} -> {to.Address}");
    }

    private static RouteSuggestionBuilder Builder() =>
        new(Options, new TransitTimeEstimator(Microsoft.Extensions.Options.Options.Create(Options)), FakeDistance);

    private static RouteBookingInput Booking(int id, string pickup, string pickupCountry, string dropoff, string dropoffCountry,
        int? directKm, DateTimeOffset? deadline = null, int windowStartOffsetDays = 0) => new()
    {
        BookingId = id,
        BookingCode = $"BKG-{id}",
        PickupAddress = pickup,
        PickupCountryCode = pickupCountry,
        DropoffAddress = dropoff,
        DropoffCountryCode = dropoffCountry,
        PickupWindowStart = Start.Date.AddDays(windowStartOffsetDays),
        PickupWindowEnd = Start.Date.AddDays(3),
        DeliveryDeadline = deadline ?? Start.AddDays(5),
        DirectDistanceKm = directKm
    };

    [Fact]
    public async Task SamePickup_NearestDropFirst_BorderBeforeForeignDrop()
    {
        var a = Booking(1, "Hà Nội", "VN", "Lạng Sơn", "VN", 160);
        var b = Booking(2, "Hà Nội", "VN", "Nam Ninh", "CN", 380);

        var route = await Builder().BuildAsync(new[] { a, b }, TransportModes.Ground, Start, 1);

        Assert.Equal(
            new[] { CheckpointTypes.Origin, CheckpointTypes.DropOff, CheckpointTypes.BorderGate, CheckpointTypes.Destination },
            route.Checkpoints.Select(c => c.CheckpointType));
        Assert.Equal(1, route.Checkpoints[1].BookingId);   // Lạng Sơn gần hơn nên trả đơn 1 trước
        Assert.Null(route.Checkpoints[3].BookingId);       // đơn 2 giao ở mốc cuối

        // Xếp ngựa 1 giờ + 160 km / 55 km/h = 2,9 giờ
        var etaA = route.Bookings.Single(x => x.BookingId == 1);
        Assert.Equal(Start.AddHours(1 + 2.9), etaA.EstimatedDeliveryAt);
        Assert.Equal(160, etaA.RouteDistanceKm);

        var etaB = route.Bookings.Single(x => x.BookingId == 2);
        Assert.Equal(380, etaB.RouteDistanceKm);
        Assert.Equal(1.0m, etaB.DetourRatio);
        Assert.True(etaB.OnTime);
        Assert.Equal(380, route.TotalDistanceKm);

        // Thời gian tăng dần để lộ trình trình duyệt được
        for (int i = 1; i < route.Checkpoints.Count; i++)
        {
            Assert.True(route.Checkpoints[i].PlannedTime > route.Checkpoints[i - 1].PlannedTime);
        }
    }

    [Fact]
    public async Task DifferentPickups_AddsPickUpStop_AndSharedDestination()
    {
        var a = Booking(1, "Hà Nội", "VN", "Nam Ninh", "CN", 380);
        var b = Booking(2, "Bắc Ninh", "VN", "Nam Ninh", "CN", 360, windowStartOffsetDays: 0);
        b.PickupWindowStart = a.PickupWindowStart.AddHours(1); // đơn 1 sẵn sàng sớm hơn -> xuất phát ở Hà Nội

        var route = await Builder().BuildAsync(new[] { b, a }, TransportModes.Ground, Start, 1);

        Assert.Equal(CheckpointTypes.Origin, route.Checkpoints[0].CheckpointType);
        Assert.Equal("Hà Nội", route.Checkpoints[0].Address);
        Assert.Equal(CheckpointTypes.PickUp, route.Checkpoints[1].CheckpointType);
        Assert.Equal(2, route.Checkpoints[1].BookingId);
        Assert.DoesNotContain(route.Checkpoints, c => c.CheckpointType == CheckpointTypes.DropOff);
        Assert.Equal(CheckpointTypes.Destination, route.Checkpoints[^1].CheckpointType);
    }

    [Fact]
    public async Task LateDelivery_IsReportedAsError()
    {
        var a = Booking(1, "Hà Nội", "VN", "Lạng Sơn", "VN", 160);
        var b = Booking(2, "Hà Nội", "VN", "Nam Ninh", "CN", 380, deadline: Start.AddHours(10));

        var route = await Builder().BuildAsync(new[] { a, b }, TransportModes.Ground, Start, 1);

        var warning = Assert.Single(route.Warnings, w => w.Code == PlanWarningCodes.DeadlineMissed);
        Assert.Equal(PlanWarningSeverities.Error, warning.Severity);
        Assert.Equal("BKG-2", warning.BookingCode);
    }

    [Fact]
    public async Task LongDetour_IsWarned()
    {
        var a = Booking(1, "Hà Nội", "VN", "Lạng Sơn", "VN", 160);
        var b = Booking(2, "Hà Nội", "VN", "Nam Ninh", "CN", directKm: 200); // tuyến ghép 380 km = 1,9 lần

        var route = await Builder().BuildAsync(new[] { a, b }, TransportModes.Ground, Start, 1);

        var warning = Assert.Single(route.Warnings, w => w.Code == PlanWarningCodes.Detour);
        Assert.Equal("1.9", warning.Params["ratio"]);
    }
}

// R-04: quy tắc lộ trình khi trình / duyệt chuyến ghép nhiều đơn
public class TripRouteRulesTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 10, 6, 0, 0, TimeSpan.Zero);

    private static RouteBookingRule Rule(int id, DateTimeOffset? deadline = null) => new()
    {
        BookingId = id,
        BookingCode = $"BKG-{id}",
        PickupWindowStart = Start.Date,
        PickupWindowEnd = Start.Date.AddDays(1),
        DeliveryDeadline = deadline ?? Start.AddDays(3)
    };

    private static RouteCheckpoint Cp(string type, double hours, int? bookingId = null) => new()
    {
        CheckpointName = $"{type} +{hours}h",
        CheckpointType = type,
        PlannedTime = Start.AddHours(hours),
        BookingId = bookingId,
        IsActive = true
    };

    private static void Validate(List<RouteCheckpoint> route, params RouteBookingRule[] bookings) =>
        TripRouteRules.Validate(route, bookings, Start, Start.AddDays(2));

    [Fact]
    public void ValidCombinedRoute_Passes()
    {
        var route = new List<RouteCheckpoint>
        {
            Cp(CheckpointTypes.Origin, 0),
            Cp(CheckpointTypes.PickUp, 2, bookingId: 2),
            Cp(CheckpointTypes.DropOff, 6, bookingId: 1),
            Cp(CheckpointTypes.BorderGate, 8),
            Cp(CheckpointTypes.Destination, 20)
        };

        Validate(route, Rule(1), Rule(2));
    }

    [Fact]
    public void DropOffBeforePickUp_IsRejected()
    {
        var route = new List<RouteCheckpoint>
        {
            Cp(CheckpointTypes.Origin, 0),
            Cp(CheckpointTypes.DropOff, 2, bookingId: 2),
            Cp(CheckpointTypes.PickUp, 4, bookingId: 2),
            Cp(CheckpointTypes.Destination, 20)
        };

        var ex = Assert.Throws<BusinessException>(() => Validate(route, Rule(1), Rule(2)));
        Assert.Contains("picked up before", ex.Message);
    }

    [Fact]
    public void DeadlineIsCheckedAtTheBookingsOwnDropOff()
    {
        // Đơn 1 trả ở DropOff lúc +6h, hạn giao +5h -> chặn; dù mốc cuối của chuyến không liên quan đơn 1
        var route = new List<RouteCheckpoint>
        {
            Cp(CheckpointTypes.Origin, 0),
            Cp(CheckpointTypes.DropOff, 6, bookingId: 1),
            Cp(CheckpointTypes.Destination, 20)
        };

        var ex = Assert.Throws<BusinessException>(() => Validate(route, Rule(1, Start.AddHours(5)), Rule(2)));
        Assert.Contains("BKG-1", ex.Message);
    }

    [Fact]
    public void BookingDroppedMidRoute_IsNotBoundByTripEnd()
    {
        // Đơn 1 hạn giao +8h, trả lúc +6h; chuyến kết thúc +20h vẫn hợp lệ
        var route = new List<RouteCheckpoint>
        {
            Cp(CheckpointTypes.Origin, 0),
            Cp(CheckpointTypes.DropOff, 6, bookingId: 1),
            Cp(CheckpointTypes.Destination, 20)
        };

        Validate(route, Rule(1, Start.AddHours(8)), Rule(2));
    }

    [Fact]
    public void PickUpWithoutBooking_IsRejected()
    {
        var route = new List<RouteCheckpoint>
        {
            Cp(CheckpointTypes.Origin, 0),
            Cp(CheckpointTypes.PickUp, 2),
            Cp(CheckpointTypes.Destination, 20)
        };

        Assert.Throws<BusinessException>(() => Validate(route, Rule(1)));
    }

    [Fact]
    public void PickUpAfterPickupWindow_IsRejected()
    {
        // Khoảng pickup của đơn 2 hết vào cuối ngày Start + 1; mốc PickUp lúc +50h là quá muộn
        var route = new List<RouteCheckpoint>
        {
            Cp(CheckpointTypes.Origin, 0),
            Cp(CheckpointTypes.PickUp, 50, bookingId: 2),
            Cp(CheckpointTypes.Destination, 60)
        };

        var ex = Assert.Throws<BusinessException>(() =>
            TripRouteRules.Validate(route, new[] { Rule(1), Rule(2) }, Start, Start.AddDays(3)));
        Assert.Contains("BKG-2", ex.Message);
    }
}

// R-04: chi phí phát sinh chỉ chia cho các đơn còn ngựa trên xe lúc xảy ra sự cố
public class ExtraChargeAllocationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 10, 6, 0, 0, TimeSpan.Zero);

    private static TripHorse Horse(int id, int bookingId, DateTimeOffset? deliveredAt = null) => new()
    {
        TripHorseId = id,
        Status = deliveredAt.HasValue ? TripHorseStatuses.Delivered : TripHorseStatuses.InTransit,
        PickedUpAt = T0,
        DeliveredAt = deliveredAt,
        BookingHorse = new BookingHorse { BookingId = bookingId }
    };

    private static Trip TwoBookingTrip() => new()
    {
        TripHorses =
        {
            Horse(1, bookingId: 1, deliveredAt: T0.AddHours(5)), // đơn 1 giao giữa đường lúc +5h
            Horse(2, bookingId: 2),
            Horse(3, bookingId: 2)
        }
    };

    [Fact]
    public void IncidentAfterADropOff_IsChargedOnlyToBookingsStillOnBoard()
    {
        var incident = new TripIncident { AdditionalCost = 300, ReportedAt = T0.AddHours(8) };

        var extras = TripCompletionService.AllocateExtraCharges(TwoBookingTrip(), new List<TripIncident> { incident });

        Assert.False(extras.ContainsKey(1));
        Assert.Equal(300, extras[2]);
    }

    [Fact]
    public void IncidentBeforeTheDropOff_IsSplitByHorses()
    {
        var incident = new TripIncident { AdditionalCost = 300, ReportedAt = T0.AddHours(2) };

        var extras = TripCompletionService.AllocateExtraCharges(TwoBookingTrip(), new List<TripIncident> { incident });

        Assert.Equal(100, extras[1]);
        Assert.Equal(200, extras[2]);
    }
}
