using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class CustomerStageRulesTests
{
    private const int BookingId = 7;

    private static BookingRequest Booking(string status, string dossier = DossierStatuses.Cleared, TripHorse? tripHorse = null)
    {
        var horse = new BookingHorse
        {
            BookingId = BookingId,
            Status = BookingHorseStatuses.Approved,
            Dossier = new DigitalDossier { Status = dossier }
        };
        if (tripHorse != null)
        {
            horse.TripHorses.Add(tripHorse);
        }
        var booking = new BookingRequest { BookingId = BookingId, Status = status };
        booking.BookingHorses.Add(horse);
        return booking;
    }

    private static TripHorse OnTrip(string tripStatus, string horseStatus, bool arrivedAtDropoff = false)
    {
        var trip = new Trip { OverallStatus = tripStatus };
        trip.Checkpoints.Add(new RouteCheckpoint { CheckpointType = CheckpointTypes.Origin, IsActive = true });
        trip.Checkpoints.Add(new RouteCheckpoint
        {
            CheckpointType = CheckpointTypes.DropOff,
            BookingId = BookingId,
            IsActive = true,
            ActualArrivalTime = arrivedAtDropoff ? DateTimeOffset.UtcNow : null
        });
        trip.Checkpoints.Add(new RouteCheckpoint { CheckpointType = CheckpointTypes.Destination, IsActive = true });
        return new TripHorse { Trip = trip, Status = horseStatus };
    }

    private static BookingPaymentSummaryModel Paid() =>
        new() { PaymentRequired = true, BalanceAmount = 700, BalancePaid = 700 };

    private static BookingPaymentSummaryModel BalanceOpen() =>
        new() { PaymentRequired = true, BalanceAmount = 700, BalancePaid = 0 };

    [Theory]
    [InlineData(BookingStatuses.Submitted, CustomerStages.PendingReview)]
    [InlineData(BookingStatuses.AwaitingDeposit, CustomerStages.AwaitingDeposit)]
    [InlineData(BookingStatuses.Rejected, CustomerStages.Closed)]
    [InlineData(BookingStatuses.Cancelled, CustomerStages.Closed)]
    [InlineData(BookingStatuses.Expired, CustomerStages.Closed)]
    [InlineData(BookingStatuses.Quoted, CustomerStages.QuoteReady)]
    [InlineData(BookingStatuses.QuoteExpired, CustomerStages.Closed)]
    [InlineData(BookingStatuses.AwaitingFinalPayment, CustomerStages.AwaitingPayment)]
    [InlineData(BookingStatuses.Completed, CustomerStages.Completed)]
    public void Resolve_MapsBookingStatus(string status, string expected)
    {
        Assert.Equal(expected, CustomerStageRules.Resolve(Booking(status), Paid()));
    }

    [Fact]
    public void Resolve_CompletedWithUnpaidExtraIsAwaitingPayment()
    {
        var payment = new BookingPaymentSummaryModel { ExtraAmount = 120, ExtraPaid = 0 };
        Assert.Equal(CustomerStages.AwaitingPayment, CustomerStageRules.Resolve(Booking(BookingStatuses.Completed), payment));
    }

    [Fact]
    public void Resolve_ApprovedWithDossierPendingIsPreparingDossier()
    {
        var booking = Booking(BookingStatuses.Approved, DossierStatuses.Reviewing);
        Assert.Equal(CustomerStages.PreparingDossier, CustomerStageRules.Resolve(booking, Paid()));
    }

    [Fact]
    public void Resolve_ClearedButOnlyOnDraftTripIsAwaitingTrip()
    {
        var booking = Booking(BookingStatuses.Approved, tripHorse: OnTrip(TripStatuses.Draft, TripHorseStatuses.Planned));
        Assert.Equal(CustomerStages.AwaitingTrip, CustomerStageRules.Resolve(booking, Paid()));
    }

    [Fact]
    public void Resolve_ApprovedTripWithBalancePaidIsScheduled()
    {
        var booking = Booking(BookingStatuses.Approved, tripHorse: OnTrip(TripStatuses.Scheduled, TripHorseStatuses.Planned));
        Assert.Equal(CustomerStages.Scheduled, CustomerStageRules.Resolve(booking, Paid()));
    }

    [Fact]
    public void Resolve_ApprovedTripWithBalanceOpenIsAwaitingPayment()
    {
        var booking = Booking(BookingStatuses.Approved, tripHorse: OnTrip(TripStatuses.Scheduled, TripHorseStatuses.Planned));
        Assert.Equal(CustomerStages.AwaitingPayment, CustomerStageRules.Resolve(booking, BalanceOpen()));
    }

    [Fact]
    public void Resolve_HorseOnBoardIsInTransit()
    {
        var booking = Booking(BookingStatuses.Approved, tripHorse: OnTrip(TripStatuses.InTransit, TripHorseStatuses.InTransit));
        Assert.Equal(CustomerStages.InTransit, CustomerStageRules.Resolve(booking, Paid()));
    }

    [Fact]
    public void Resolve_VehicleAtDropoffIsAwaitingHandover()
    {
        var booking = Booking(BookingStatuses.Approved, tripHorse: OnTrip(TripStatuses.InTransit, TripHorseStatuses.InTransit, arrivedAtDropoff: true));
        Assert.Equal(CustomerStages.AwaitingHandover, CustomerStageRules.Resolve(booking, Paid()));
    }

    [Fact]
    public void Resolve_CancelledTripIsIgnored()
    {
        var booking = Booking(BookingStatuses.Approved, tripHorse: OnTrip(TripStatuses.Cancelled, TripHorseStatuses.Cancelled));
        Assert.Equal(CustomerStages.AwaitingTrip, CustomerStageRules.Resolve(booking, Paid()));
    }
}
