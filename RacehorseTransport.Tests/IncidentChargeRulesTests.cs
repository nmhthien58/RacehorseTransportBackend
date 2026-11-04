using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

// Chính sách 9.1: phần khách trả của chi phí phát sinh khi đóng chuyến, theo nguyên nhân sự cố
public class IncidentChargeRulesTests
{
    private static TripIncident Detour(string faultParty) => new()
    {
        ResponseAction = IncidentResponseActions.Reroute,
        AdditionalCost = 180,       // chi phí xe + người của công ty
        ExtraCostForCustomer = 180, // phí đi vòng khách đã đồng ý
        FaultParty = faultParty
    };

    [Fact]
    public void ForceMajeureDetour_CustomerPaysOnlyTheAgreedDetourFee()
    {
        Assert.Equal(180, TripCompletionService.CustomerCharge(Detour(FaultParties.ForceMajeure)));
    }

    [Fact]
    public void CustomerFaultDetour_CustomerPaysEverything()
    {
        Assert.Equal(360, TripCompletionService.CustomerCharge(Detour(FaultParties.Customer)));
    }

    [Fact]
    public void CompanyFault_CustomerPaysNothing()
    {
        Assert.Equal(0, TripCompletionService.CustomerCharge(Detour(FaultParties.Company)));
    }

    [Fact]
    public void ForceMajeureHoldAtStable_CustomerPaysTheStable()
    {
        var incident = new TripIncident
        {
            ResponseAction = IncidentResponseActions.HoldAtStable,
            AdditionalCost = 150,
            FaultParty = FaultParties.ForceMajeure
        };

        Assert.Equal(150, TripCompletionService.CustomerCharge(incident));
    }

    [Fact]
    public void ForceMajeureWait_CompanyBearsTheCost()
    {
        var incident = new TripIncident
        {
            ResponseAction = IncidentResponseActions.WaitAndContinue,
            AdditionalCost = 200,
            FaultParty = FaultParties.ForceMajeure
        };

        Assert.Equal(0, TripCompletionService.CustomerCharge(incident));
    }
}
