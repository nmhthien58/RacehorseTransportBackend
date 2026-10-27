using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class ClaimRulesTests
{
    private static readonly PolicySettingsModel Policy = new();
    private static readonly DateTimeOffset Delivered = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ClaimTypes.Death, 5000)]
    [InlineData(ClaimTypes.Loss, 5000)]
    [InlineData(ClaimTypes.Injury, 1000)]
    public void NoInsurance_CompanyFault_UsesBaseCaps(string type, decimal cap)
    {
        Assert.Equal(cap, ClaimRules.Cap(InsurancePlans.None, type, 200000m, FaultParties.Company, Policy));
    }

    [Theory]
    [InlineData(FaultParties.Customer)]
    [InlineData(FaultParties.ForceMajeure)]
    [InlineData(null)]
    public void NoInsurance_NotCompanyFault_PaysNothing(string? fault)
    {
        Assert.Equal(0m, ClaimRules.Cap(InsurancePlans.None, ClaimTypes.Death, 200000m, fault, Policy));
        Assert.Throws<BusinessException>(() =>
            ClaimRules.EnsureApprovable(InsurancePlans.None, ClaimTypes.Death, 200000m, fault, 100m, Policy));
    }

    [Theory]
    [InlineData(ClaimTypes.Death, 200000)]
    [InlineData(ClaimTypes.Loss, 200000)]
    [InlineData(ClaimTypes.Injury, 40000)]
    public void FullInsurance_AnyCause_UsesAgreedValue(string type, decimal cap)
    {
        Assert.Equal(cap, ClaimRules.Cap(InsurancePlans.Full, type, 200000m, FaultParties.ForceMajeure, Policy));
    }

    [Fact]
    public void AmountAboveCap_IsRejected()
    {
        Assert.Throws<BusinessException>(() =>
            ClaimRules.EnsureApprovable(InsurancePlans.Full, ClaimTypes.Injury, 200000m, null, 40000.01m, Policy));
        ClaimRules.EnsureApprovable(InsurancePlans.Full, ClaimTypes.Injury, 200000m, null, 40000m, Policy);
    }

    [Fact]
    public void ZeroAmount_IsRejected()
    {
        Assert.Throws<BusinessException>(() =>
            ClaimRules.EnsureApprovable(InsurancePlans.Full, ClaimTypes.Death, 200000m, null, 0m, Policy));
    }

    [Fact]
    public void CustomerWindow_Is24HoursAfterDelivery()
    {
        Assert.True(ClaimRules.WithinCustomerWindow(Delivered, Delivered.AddHours(24)));
        Assert.False(ClaimRules.WithinCustomerWindow(Delivered, Delivered.AddHours(24).AddMinutes(1)));
        Assert.False(ClaimRules.WithinCustomerWindow(null, Delivered));
    }
}
