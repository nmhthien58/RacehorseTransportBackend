using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class VetExpenseRulesTests
{
    [Theory]
    [InlineData(FaultParties.Company, VetExpenseBearers.Company)]
    [InlineData(FaultParties.Customer, VetExpenseBearers.Customer)]
    [InlineData(FaultParties.ForceMajeure, VetExpenseBearers.Customer)] // ngựa tự phát bệnh
    public void Bearer_FollowsTheCause(string fault, string expected)
    {
        Assert.Equal(expected, VetExpenseRules.Bearer(fault, euthanasiaDeclined: false));
    }

    [Fact]
    public void FinalBearer_IsCustomer_WhenCompanyFault_ButEuthanasiaDeclined()
    {
        Assert.Equal(VetExpenseBearers.Customer, VetExpenseRules.Bearer(FaultParties.Company, euthanasiaDeclined: true));
    }

    [Fact]
    public void ValidateVetClinic_RequiredForWaitRecover()
    {
        Assert.Throws<BusinessException>(() => OffloadRules.ValidateVetClinic(OffloadOutcomes.WaitRecover, null));
        Assert.Throws<BusinessException>(() => OffloadRules.ValidateVetClinic(OffloadOutcomes.WaitRecover, new VetClinicModel { Name = "Clinic" }));
        OffloadRules.ValidateVetClinic(OffloadOutcomes.WaitRecover, new VetClinicModel { Name = "Phòng khám Bằng Tường", Address = "Bằng Tường, Quảng Tây" });
    }

    [Fact]
    public void ValidateVetClinic_RejectedForOtherOutcomes()
    {
        Assert.Throws<BusinessException>(() =>
            OffloadRules.ValidateVetClinic(OffloadOutcomes.HandOverHere, new VetClinicModel { Name = "X", Address = "Y" }));
        OffloadRules.ValidateVetClinic(OffloadOutcomes.Deceased, null);
        OffloadRules.ValidateVetClinic(null, null);
    }
}
