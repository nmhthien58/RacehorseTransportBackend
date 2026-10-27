using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class OffloadRulesTests
{
    private static TripHorse Horse(string status, string? outcome = null, int id = 1) =>
        new() { TripHorseId = id, Status = status, OffloadOutcome = outcome };

    private static TripIncident Incident(bool emergency = false, int? affectedTripHorseId = null) =>
        new() { EmergencyOffload = emergency, AffectedTripHorseId = affectedTripHorseId };

    // ---------- ValidatePlan ----------

    [Theory]
    [InlineData(OffloadOutcomes.WaitRecover)]
    [InlineData(OffloadOutcomes.ReturnToOrigin)]
    [InlineData(OffloadOutcomes.HandOverHere)]
    public void ValidatePlan_AcceptsNonFatalOutcomeOnAnySeverityWithoutReport(string outcome)
    {
        OffloadRules.ValidatePlan(outcome, IncidentSeverities.Minor, vetReportPath: null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Recover")]
    public void ValidatePlan_RejectsMissingOrUnknownOutcome(string? outcome)
    {
        Assert.Throws<BusinessException>(() => OffloadRules.ValidatePlan(outcome, IncidentSeverities.Major, null));
    }

    [Fact]
    public void ValidatePlan_Deceased_RequiresCriticalIncident()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            OffloadRules.ValidatePlan(OffloadOutcomes.Deceased, IncidentSeverities.Major, "documents/vet.pdf"));

        Assert.Contains("Critical", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void ValidatePlan_Deceased_RequiresVetReport(string? vetReportPath)
    {
        var ex = Assert.Throws<BusinessException>(() =>
            OffloadRules.ValidatePlan(OffloadOutcomes.Deceased, IncidentSeverities.Critical, vetReportPath));

        Assert.Contains("veterinary report", ex.Message);
    }

    [Fact]
    public void ValidatePlan_Deceased_OkOnCriticalWithReport()
    {
        OffloadRules.ValidatePlan(OffloadOutcomes.Deceased, IncidentSeverities.Critical, "documents/vet.pdf");
    }

    // ---------- CanBeOffloaded ----------

    [Theory]
    [InlineData(TripHorseStatuses.Planned)]
    [InlineData(TripHorseStatuses.InTransit)]
    public void CanBeOffloaded_HorseOnBoard(string status)
    {
        Assert.True(OffloadRules.CanBeOffloaded(Horse(status), Incident()));
    }

    [Theory]
    [InlineData(TripHorseStatuses.Delivered)]
    [InlineData(TripHorseStatuses.Deceased)]
    [InlineData(TripHorseStatuses.Cancelled)]
    public void CanBeOffloaded_NotWhenHorseIsAlreadyOff(string status)
    {
        Assert.False(OffloadRules.CanBeOffloaded(Horse(status), Incident()));
    }

    [Fact]
    public void CanBeOffloaded_EmergencyOffloadedHorseOfThisIncident()
    {
        var horse = Horse(TripHorseStatuses.Offloaded, outcome: null, id: 7);

        Assert.True(OffloadRules.CanBeOffloaded(horse, Incident(emergency: true, affectedTripHorseId: 7)));
    }

    [Fact]
    public void CanBeOffloaded_NotWhenOffloadedOnAnotherIncidentOrAlreadyDecided()
    {
        // Dỡ khẩn cấp ở sự cố khác
        Assert.False(OffloadRules.CanBeOffloaded(Horse(TripHorseStatuses.Offloaded, null, id: 7), Incident(emergency: true, affectedTripHorseId: 8)));
        // Sự cố không dỡ khẩn cấp
        Assert.False(OffloadRules.CanBeOffloaded(Horse(TripHorseStatuses.Offloaded, null, id: 7), Incident(emergency: false, affectedTripHorseId: 7)));
        // Đã có hướng xử lý được duyệt
        Assert.False(OffloadRules.CanBeOffloaded(Horse(TripHorseStatuses.Offloaded, OffloadOutcomes.WaitRecover, id: 7), Incident(emergency: true, affectedTripHorseId: 7)));
    }

    // ---------- StatusFor ----------

    [Theory]
    [InlineData(OffloadOutcomes.WaitRecover, TripHorseStatuses.Offloaded)]
    [InlineData(OffloadOutcomes.ReturnToOrigin, TripHorseStatuses.Offloaded)]
    [InlineData(OffloadOutcomes.HandOverHere, TripHorseStatuses.Offloaded)]
    [InlineData(OffloadOutcomes.Deceased, TripHorseStatuses.Deceased)]
    public void StatusFor_MapsOutcomeToTripHorseStatus(string outcome, string expected)
    {
        Assert.Equal(expected, OffloadRules.StatusFor(outcome));
    }

    // ---------- IsFinished / IsPendingOnTrip ----------

    [Theory]
    [InlineData(TripHorseStatuses.Delivered, null, true)]
    [InlineData(TripHorseStatuses.Deceased, OffloadOutcomes.Deceased, true)]
    [InlineData(TripHorseStatuses.Offloaded, OffloadOutcomes.ReturnToOrigin, true)]
    [InlineData(TripHorseStatuses.Offloaded, OffloadOutcomes.WaitRecover, false)]  // còn phải đi chuyến sau
    [InlineData(TripHorseStatuses.Offloaded, OffloadOutcomes.HandOverHere, false)] // chưa ký e-POD tại chỗ
    [InlineData(TripHorseStatuses.Offloaded, null, false)]                         // dỡ khẩn cấp, chưa có phương án
    [InlineData(TripHorseStatuses.InTransit, null, false)]
    public void IsFinished(string status, string? outcome, bool expected)
    {
        Assert.Equal(expected, OffloadRules.IsFinished(Horse(status, outcome)));
    }

    [Theory]
    [InlineData(TripHorseStatuses.Planned, null, true)]
    [InlineData(TripHorseStatuses.InTransit, null, true)]
    [InlineData(TripHorseStatuses.Offloaded, null, true)]
    [InlineData(TripHorseStatuses.Offloaded, OffloadOutcomes.HandOverHere, true)]
    [InlineData(TripHorseStatuses.Offloaded, OffloadOutcomes.WaitRecover, false)]
    [InlineData(TripHorseStatuses.Offloaded, OffloadOutcomes.ReturnToOrigin, false)]
    [InlineData(TripHorseStatuses.Deceased, OffloadOutcomes.Deceased, false)]
    [InlineData(TripHorseStatuses.Delivered, null, false)]
    [InlineData(TripHorseStatuses.Cancelled, null, false)]
    public void IsPendingOnTrip(string status, string? outcome, bool expected)
    {
        Assert.Equal(expected, OffloadRules.IsPendingOnTrip(Horse(status, outcome)));
    }
}
