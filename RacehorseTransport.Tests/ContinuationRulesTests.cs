using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class ContinuationRulesTests
{
    private static TripHorse AtClinic() => new() { TripHorseId = 4, Status = TripHorseStatuses.Offloaded, OffloadOutcome = OffloadOutcomes.WaitRecover };

    private static TripIncident Resolved(string fault, params string[] documentTypes) => new()
    {
        IncidentCode = "INC-2026-0010",
        Status = IncidentStatuses.Resolved,
        FaultParty = fault,
        Documents = documentTypes.Select(t => new IncidentDocument { DocumentType = t }).ToList()
    };

    [Fact]
    public void Allowed_WhenResolvedAndFitToTravel()
    {
        ContinuationRules.EnsureCanContinue(Resolved(FaultParties.ForceMajeure, IncidentDocumentTypes.FitToTravel), AtClinic());
    }

    [Fact]
    public void Blocked_WithoutFitToTravel()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ContinuationRules.EnsureCanContinue(Resolved(FaultParties.Company, IncidentDocumentTypes.VetReport), AtClinic()));
        Assert.Contains("FitToTravel", ex.Message);
    }

    [Fact]
    public void Blocked_WhenIncidentNotResolved()
    {
        var incident = Resolved(FaultParties.Company, IncidentDocumentTypes.FitToTravel);
        incident.Status = IncidentStatuses.Approved;
        incident.FaultParty = null;
        Assert.Throws<BusinessException>(() => ContinuationRules.EnsureCanContinue(incident, AtClinic()));
    }

    [Fact]
    public void Blocked_WhenHorseDiedAtClinic()
    {
        var horse = AtClinic();
        horse.Status = TripHorseStatuses.Deceased;
        Assert.Throws<BusinessException>(() =>
            ContinuationRules.EnsureCanContinue(Resolved(FaultParties.Company, IncidentDocumentTypes.FitToTravel), horse));
    }

    [Theory]
    [InlineData(FaultParties.Company, true)]
    [InlineData(FaultParties.ForceMajeure, false)]
    [InlineData(FaultParties.Customer, false)]
    public void IsFree_OnlyWhenCompanyFault(string fault, bool expected)
    {
        Assert.Equal(expected, ContinuationRules.IsFree(fault));
    }
}
