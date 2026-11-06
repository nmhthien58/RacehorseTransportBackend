using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class EuthanasiaConsentRulesTests
{
    private static readonly DateTimeOffset Requested = new(2026, 11, 6, 8, 0, 0, TimeSpan.Zero);

    private static TripIncident Waiting(params string[] documentTypes) => new()
    {
        IncidentCode = "INC-2026-0099",
        Status = IncidentStatuses.AwaitingConsent,
        PendingConsentType = ConsentTypes.Euthanasia,
        ConsentRequestedAt = Requested,
        Documents = documentTypes.Select(t => new IncidentDocument { DocumentType = t }).ToList()
    };

    // ---------- ValidatePlan ----------

    [Fact]
    public void ValidatePlan_AllowsConsentOnDeceasedOffload()
    {
        EuthanasiaConsentRules.ValidatePlan(IncidentResponseActions.OffloadHorse, OffloadOutcomes.Deceased, true);
    }

    [Theory]
    [InlineData(IncidentResponseActions.OffloadHorse, OffloadOutcomes.WaitRecover)]
    [InlineData(IncidentResponseActions.VeterinaryCare, null)]
    public void ValidatePlan_RejectsConsentOutsideDeceasedOffload(string action, string? outcome)
    {
        Assert.Throws<BusinessException>(() => EuthanasiaConsentRules.ValidatePlan(action, outcome, true));
    }

    [Fact]
    public void ValidatePlan_NoConsentFlag_AlwaysOk()
    {
        EuthanasiaConsentRules.ValidatePlan(IncidentResponseActions.Reroute, null, false);
    }

    // ---------- PendingType ----------

    [Fact]
    public void PendingType_OldIncidentsWaitForDetour()
    {
        Assert.Equal(ConsentTypes.Detour, EuthanasiaConsentRules.PendingType(new TripIncident()));
        Assert.Equal(ConsentTypes.Euthanasia, EuthanasiaConsentRules.PendingType(Waiting()));
    }

    // ---------- MinutesLeft / Deadline ----------

    [Theory]
    [InlineData(0, 60)]
    [InlineData(30, 30)]
    [InlineData(59.5, 1)]
    [InlineData(60, 0)]
    [InlineData(90, 0)]
    public void MinutesLeft_CountsDownToZero(double elapsedMinutes, int expected)
    {
        Assert.Equal(expected, EuthanasiaConsentRules.MinutesLeft(Requested, Requested.AddMinutes(elapsedMinutes), 60));
    }

    [Fact]
    public void Deadline_IsRequestPlusPolicyMinutes()
    {
        Assert.Equal(Requested.AddMinutes(45), EuthanasiaConsentRules.Deadline(Requested, 45));
    }

    // ---------- EnsureCanEmergencyAuthorize ----------

    [Fact]
    public void EmergencyAuthorize_Blocked_WhenWindowNotExpired()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            EuthanasiaConsentRules.EnsureCanEmergencyAuthorize(Waiting(IncidentDocumentTypes.VetReport), Requested.AddMinutes(20), 60));

        Assert.Contains("40 minute", ex.Message);
    }

    [Fact]
    public void EmergencyAuthorize_Allowed_AfterWindowWithVetReport()
    {
        EuthanasiaConsentRules.EnsureCanEmergencyAuthorize(Waiting(IncidentDocumentTypes.VetReport), Requested.AddMinutes(61), 60);
    }

    [Fact]
    public void EmergencyAuthorize_Blocked_WithoutVetReport()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            EuthanasiaConsentRules.EnsureCanEmergencyAuthorize(Waiting(IncidentDocumentTypes.ScenePhoto), Requested.AddMinutes(61), 60));

        Assert.Contains("veterinarian's report", ex.Message);
    }

    [Fact]
    public void EmergencyAuthorize_Blocked_WhenWaitingForDetour()
    {
        var incident = Waiting(IncidentDocumentTypes.VetReport);
        incident.PendingConsentType = ConsentTypes.Detour;

        Assert.Throws<BusinessException>(() => EuthanasiaConsentRules.EnsureCanEmergencyAuthorize(incident, Requested.AddMinutes(61), 60));
    }

    [Fact]
    public void EmergencyAuthorize_Blocked_WhenAlreadyDecided()
    {
        var incident = Waiting(IncidentDocumentTypes.VetReport);
        incident.Status = IncidentStatuses.Approved;

        Assert.Throws<BusinessException>(() => EuthanasiaConsentRules.EnsureCanEmergencyAuthorize(incident, Requested.AddMinutes(61), 60));
    }
}
