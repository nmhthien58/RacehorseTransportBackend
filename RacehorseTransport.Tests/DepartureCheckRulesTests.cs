using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class DepartureCheckRulesTests
{
    private static readonly DateTimeOffset Departure = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private static DepartureHorseInput Horse(params DateOnly[] fitExams) => new()
    {
        HorseName = "Silver Comet",
        BookingCode = "BKG-2026-0012",
        DossierStatus = DossierStatuses.Cleared,
        FitExamIssueDates = fitExams.ToList()
    };

    private static DepartureCheckInput Ready(DepartureHorseInput? horse = null) => new()
    {
        DepartureAt = Departure,
        PlannedArrivalAt = Departure.AddDays(2),
        TripStatus = TripStatuses.Scheduled,
        FitExamValidHours = 48,
        FitExamTypeConfigured = true,
        Horses = { horse ?? Horse(new DateOnly(2026, 11, 9)) },
        Bookings = { new DepartureBookingInput { BookingCode = "BKG-2026-0012", EstimatedCost = 1000, Paid = 1000, CurrencyCode = "USD" } },
        IsGround = true,
        DrivingHours = 8,
        DriverCount = 1,
        SingleDriverLimitHours = 9
    };

    [Fact]
    public void AllConditionsMet_IsReady()
    {
        var result = DepartureCheckRules.Evaluate(Ready());

        Assert.True(result.IsReady);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void DocumentExpiredBeforeDepartureDay_Blocks()
    {
        var horse = Horse(new DateOnly(2026, 11, 9));
        horse.Documents.Add(new DepartureDocumentInput { DocTypeName = "Coggins", ExpiryDate = new DateOnly(2026, 11, 9) });

        var result = DepartureCheckRules.Evaluate(Ready(horse));

        var issue = Assert.Single(result.Issues);
        Assert.Equal(DepartureIssueCodes.DocumentExpired, issue.Code);
        Assert.Equal(DepartureIssueSeverities.Error, issue.Severity);
        Assert.Equal("Silver Comet", issue.HorseName);
        Assert.False(result.IsReady);
    }

    [Fact]
    public void DocumentExpiringOnDepartureDay_IsStillValid_ButWarnsBeforeArrival()
    {
        var horse = Horse(new DateOnly(2026, 11, 9));
        horse.Documents.Add(new DepartureDocumentInput { DocTypeName = "Coggins", ExpiryDate = new DateOnly(2026, 11, 10) });

        var result = DepartureCheckRules.Evaluate(Ready(horse));

        var issue = Assert.Single(result.Issues);
        Assert.Equal(DepartureIssueCodes.DocumentExpiresBeforeArrival, issue.Code);
        Assert.Equal(DepartureIssueSeverities.Warning, issue.Severity);
        Assert.True(result.IsReady);
    }

    [Fact]
    public void DocumentValidAtPlannedTime_ExpiredAtLateActualDeparture_Blocks()
    {
        // Chuyến lùi 2 ngày: giấy còn hạn ở giờ dự kiến nhưng hết hạn ở giờ xuất phát thực tế
        var horse = Horse(new DateOnly(2026, 11, 11));
        horse.Documents.Add(new DepartureDocumentInput { DocTypeName = "Health certificate", ExpiryDate = new DateOnly(2026, 11, 11) });
        var input = Ready(horse);
        input.DepartureAt = Departure.AddDays(2);

        var result = DepartureCheckRules.Evaluate(input);

        Assert.Contains(result.Issues, i => i.Code == DepartureIssueCodes.DocumentExpired);
        Assert.False(result.IsReady);
    }

    [Theory]
    [InlineData(2026, 11, 8, true)]   // 08:00 ngày 10 trừ 48 giờ = ngày 8
    [InlineData(2026, 11, 10, true)]
    [InlineData(2026, 11, 7, false)]
    [InlineData(2026, 11, 11, false)] // ngày cấp sau giờ xuất phát
    public void FitExam_MustBeIssuedWithin48Hours(int year, int month, int day, bool valid)
    {
        Assert.Equal(valid, DepartureCheckRules.HasValidFitExam(new[] { new DateOnly(year, month, day) }, Departure, 48));
    }

    [Fact]
    public void FitExamMissing_Blocks_WithFrontendShape()
    {
        var result = DepartureCheckRules.Evaluate(Ready(Horse(new DateOnly(2026, 11, 1))));

        var issue = Assert.Single(result.Issues);
        Assert.Equal(DepartureIssueCodes.FitExamMissing, issue.Code);
        Assert.Equal("Silver Comet: no fit-to-travel exam within 48 hours", issue.Message);
        Assert.Equal(DepartureIssueCategories.Documents, issue.Category);
        Assert.Equal("BKG-2026-0012", issue.BookingCode);
        Assert.False(result.IsReady);
    }

    [Fact]
    public void FitExamTypeNotInDatabase_OnlyWarns()
    {
        var input = Ready(Horse());
        input.FitExamTypeConfigured = false;

        var result = DepartureCheckRules.Evaluate(input);

        var issue = Assert.Single(result.Issues);
        Assert.Equal(DepartureIssueCodes.FitExamTypeNotConfigured, issue.Code);
        Assert.Equal(DepartureIssueSeverities.Warning, issue.Severity);
        Assert.True(result.IsReady);
    }

    [Fact]
    public void UnpaidBalance_Blocks()
    {
        var input = Ready();
        input.Bookings[0].Paid = 300;

        var result = DepartureCheckRules.Evaluate(input);

        var issue = Assert.Single(result.Issues);
        Assert.Equal(DepartureIssueCodes.BalanceUnpaid, issue.Code);
        Assert.Equal(DepartureIssueCategories.Payment, issue.Category);
        Assert.Contains("700 USD", issue.Message);
    }

    [Fact]
    public void LongGroundTripWithOneDriver_Blocks()
    {
        var input = Ready();
        input.DrivingHours = 12;

        var result = DepartureCheckRules.Evaluate(input);

        var issue = Assert.Single(result.Issues);
        Assert.Equal(DepartureIssueCodes.NotEnoughDrivers, issue.Code);
        Assert.Equal(DepartureIssueCategories.Crew, issue.Category);
    }

    [Fact]
    public void DriverRule_DoesNotApplyToAirTrips()
    {
        var input = Ready();
        input.IsGround = false;
        input.DrivingHours = 12;

        Assert.True(DepartureCheckRules.Evaluate(input).IsReady);
    }

    [Fact]
    public void BlockingMessage_ListsEveryError()
    {
        var horse = Horse();
        horse.DossierStatus = DossierStatuses.Reviewing;
        var input = Ready(horse);
        input.Bookings[0].Paid = 0;
        input.DrivingHours = 12;

        var result = DepartureCheckRules.Evaluate(input);
        string message = DepartureCheckRules.ToBlockingMessage(result);

        Assert.Equal(4, result.Issues.Count);
        Assert.StartsWith("The trip cannot depart: ", message);
        Assert.Contains("clearance is not complete", message);
        Assert.Contains("fit-to-travel exam", message);
        Assert.Contains("remaining balance", message);
        Assert.Contains("at least 2 drivers", message);
    }
}
