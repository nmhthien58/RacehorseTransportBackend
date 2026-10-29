using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

// Đơn đường bay: duyệt sơ bộ phát hành báo giá chốt ngay (không có tuyến trên bản đồ); đơn đường bộ giữ luồng lập tuyến.
public class AirFinalQuoteTests
{
    [Theory]
    [InlineData(TransportModes.Air, true)]
    [InlineData(TransportModes.Ground, false)]
    public void QuotesOnPreApproval_OnlyAir(string mode, bool expected)
    {
        Assert.Equal(expected, QuoteRules.QuotesOnPreApproval(mode));
    }

    [Theory]
    [InlineData(TransportModes.Air, BookingStatuses.Submitted, true)]
    [InlineData(TransportModes.Ground, BookingStatuses.Submitted, true)]
    // Đơn bay đã kẹt ở PreApproved duyệt lại được để ra báo giá
    [InlineData(TransportModes.Air, BookingStatuses.PreApproved, true)]
    [InlineData(TransportModes.Ground, BookingStatuses.PreApproved, false)]
    [InlineData(TransportModes.Air, BookingStatuses.Quoted, false)]
    [InlineData(TransportModes.Air, BookingStatuses.Rejected, false)]
    public void CanPreApprove_ByModeAndStatus(string mode, string status, bool expected)
    {
        Assert.Equal(expected, QuoteRules.CanPreApprove(mode, status));
    }

    [Fact]
    public void BuildQuoteInput_AirWithoutRoutePlan_UsesBookingData()
    {
        var booking = Booking(TransportModes.Air);
        booking.IsExpress = true;
        var horses = Horses(StallClasses.Shared, StallClasses.Private);

        var input = FinalQuoteIssuer.BuildQuoteInput(booking, horses, plan: null);

        Assert.Equal(TransportModes.Air, input.TransportMode);
        Assert.Equal("VN", input.PickupCountryCode);
        Assert.Equal("JP", input.DropoffCountryCode);
        Assert.Null(input.DistanceKm);
        Assert.Empty(input.TransitCountryCodes);
        Assert.Equal(new[] { StallClasses.Shared, StallClasses.Private }, input.StallClasses);
        Assert.True(input.SkipScheduleCheck);
        Assert.True(input.ForceExpress);
        Assert.Equal(booking.CreatedAt, input.QuotedAt);
        Assert.Null(input.DeclaredValue);
    }

    [Fact]
    public void BuildQuoteInput_GroundWithoutRoutePlan_Throws()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            FinalQuoteIssuer.BuildQuoteInput(Booking(TransportModes.Ground), Horses(StallClasses.Shared), plan: null));
        Assert.Contains("route plan", ex.Message);
    }

    [Fact]
    public void BuildQuoteInput_GroundWithRoutePlan_UsesPlanDistanceAndTransit()
    {
        var booking = Booking(TransportModes.Ground);
        booking.DistanceKm = 900;
        booking.TransitCountries.Add(new BookingTransitCountry { SequenceOrder = 1, CountryCode = "KH" });
        var plan = new RoutePlan { TotalDistanceKm = 1234.2m, TransitCountryCodes = "[\"LA\",\"TH\"]" };

        var input = FinalQuoteIssuer.BuildQuoteInput(booking, Horses(StallClasses.Shared), plan);

        Assert.Equal(1235, input.DistanceKm);
        Assert.Equal(new[] { "LA", "TH" }, input.TransitCountryCodes);
    }

    private static BookingRequest Booking(string mode)
    {
        var pickup = new DateTimeOffset(2026, 12, 10, 0, 0, 0, TimeSpan.Zero);
        return new BookingRequest
        {
            BookingCode = "BK-TEST",
            TransportMode = mode,
            PickupCountryCode = "VN",
            DropoffCountryCode = "JP",
            PickupWindowStart = pickup,
            PickupWindowEnd = pickup.AddDays(3),
            DeliveryDeadline = pickup.AddDays(10),
            CreatedAt = new DateTimeOffset(2026, 10, 29, 2, 0, 0, TimeSpan.Zero)
        };
    }

    private static List<BookingHorse> Horses(params string[] stallClasses) =>
        stallClasses.Select((s, i) => new BookingHorse { BookingHorseId = i + 1, StallClass = s }).ToList();
}
