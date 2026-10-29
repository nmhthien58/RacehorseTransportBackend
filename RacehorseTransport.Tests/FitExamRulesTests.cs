using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class FitExamRulesTests
{
    private static readonly DateTimeOffset Departure = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ExamWindow_Is48HoursBeforePlannedDeparture()
    {
        var (start, end) = FitExamRules.ExamWindow(Departure, 48);

        Assert.Equal(new DateTimeOffset(2026, 11, 8, 8, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(Departure, end);
    }

    [Theory]
    [InlineData(2026, 11, 8, true)]
    [InlineData(2026, 11, 10, true)]
    [InlineData(2026, 11, 7, false)]  // khám trước khoảng 48 giờ
    [InlineData(2026, 11, 11, false)] // sau giờ xuất phát
    public void IssueDate_MustBeInsideWindow(int year, int month, int day, bool valid)
    {
        Assert.Equal(valid, FitExamRules.IsIssuedInWindow(new DateOnly(year, month, day), Departure, 48));
    }

    [Fact]
    public void IssueDate_FallsBackToUploadDay()
    {
        var uploadedAt = new DateTimeOffset(2026, 11, 9, 23, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 11, 9), FitExamRules.IssueDateOf(null, uploadedAt));
        Assert.Equal(new DateOnly(2026, 11, 8), FitExamRules.IssueDateOf(new DateOnly(2026, 11, 8), uploadedAt));
    }

    [Theory]
    [InlineData(DocumentStatuses.Approved, true)]
    [InlineData(DocumentStatuses.Pending, false)]
    [InlineData(DocumentStatuses.Rejected, false)]
    public void OnlyApprovedExamCountsAtDeparture(string status, bool counts)
    {
        Assert.Equal(counts, FitExamRules.CountsAtDeparture(status));
    }

    [Fact]
    public void FitExamRow_DoesNotBlockSubmitToAuthorities()
    {
        var checklist = new List<DossierChecklistItemModel>
        {
            new() { DocTypeCode = "HEALTH_CERT", IsMandatory = true, State = DocumentStatuses.Approved },
            new() { DocTypeCode = DocumentTypeCodes.FitExam, IsMandatory = true, PostClearance = true, State = ChecklistStates.Missing },
            new() { DocTypeCode = "OPTIONAL", IsMandatory = false, State = ChecklistStates.Missing }
        };

        var required = FitExamRules.RequiredForClearance(checklist).ToList();

        var item = Assert.Single(required);
        Assert.Equal("HEALTH_CERT", item.DocTypeCode);
        Assert.All(required, c => Assert.Equal(DocumentStatuses.Approved, c.State));
    }

    [Fact]
    public void PendingExam_BlocksWithOwnIssueCode()
    {
        var input = new DepartureCheckInput
        {
            DepartureAt = Departure,
            PlannedArrivalAt = Departure.AddDays(1),
            TripStatus = TripStatuses.Scheduled,
            FitExamValidHours = 48,
            FitExamTypeConfigured = true,
            Horses =
            {
                new DepartureHorseInput
                {
                    HorseName = "Silver Comet",
                    BookingCode = "BKG-2026-0012",
                    DossierStatus = DossierStatuses.Cleared,
                    HasPendingFitExam = true
                }
            }
        };

        var result = DepartureCheckRules.Evaluate(input);

        var issue = Assert.Single(result.Issues);
        Assert.Equal(DepartureIssueCodes.FitExamPendingReview, issue.Code);
        Assert.Equal(DepartureIssueSeverities.Error, issue.Severity);
        Assert.Equal(DepartureIssueCategories.Documents, issue.Category);
        Assert.False(result.IsReady);
    }

    [Fact]
    public void ApprovedExamInWindow_PassesEvenWithAnotherPending()
    {
        var input = new DepartureCheckInput
        {
            DepartureAt = Departure,
            PlannedArrivalAt = Departure.AddDays(1),
            TripStatus = TripStatuses.Scheduled,
            FitExamValidHours = 48,
            FitExamTypeConfigured = true,
            Horses =
            {
                new DepartureHorseInput
                {
                    HorseName = "Silver Comet",
                    BookingCode = "BKG-2026-0012",
                    DossierStatus = DossierStatuses.Cleared,
                    FitExamIssueDates = { new DateOnly(2026, 11, 9) },
                    HasPendingFitExam = true
                }
            }
        };

        Assert.True(DepartureCheckRules.Evaluate(input).IsReady);
    }
}
