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

    // ---------- Hồ sơ tử vong bổ sung ----------

    [Fact]
    public void SupplementaryDeathClaim_NonePackage_DoesNotExceed5000Total()
    {
        // Không mua bảo hiểm, lỗi công ty: thương tích đã duyệt 1.000 -> tử vong bổ sung tối đa 4.000
        decimal cap = ClaimRules.RemainingCap(ClaimRules.Cap(InsurancePlans.None, ClaimTypes.Death, 200000m, FaultParties.Company, Policy), ClaimTypes.Death, 1000m);
        Assert.Equal(4000m, cap);

        ClaimRules.EnsureApprovable(InsurancePlans.None, ClaimTypes.Death, 200000m, FaultParties.Company, 4000m, Policy, 1000m);
        var ex = Assert.Throws<BusinessException>(() =>
            ClaimRules.EnsureApprovable(InsurancePlans.None, ClaimTypes.Death, 200000m, FaultParties.Company, 4000.01m, Policy, 1000m));
        Assert.Contains("already approved", ex.Message);
    }

    [Fact]
    public void SupplementaryDeathClaim_FullPackage_PaysAgreedValueMinusInjury()
    {
        Assert.Equal(190000m, ClaimRules.RemainingCap(200000m, ClaimTypes.Death, 10000m));
    }

    [Fact]
    public void RemainingCap_InjuryKeepsItsOwnCap_AndNeverNegative()
    {
        Assert.Equal(1000m, ClaimRules.RemainingCap(1000m, ClaimTypes.Injury, 5000m));
        Assert.Equal(0m, ClaimRules.RemainingCap(5000m, ClaimTypes.Death, 6000m));
    }

    [Theory]
    [InlineData(ClaimTypes.Death, ClaimTypes.Injury, ClaimStatuses.Approved, true)]
    [InlineData(ClaimTypes.Death, ClaimTypes.Injury, ClaimStatuses.Paid, true)]
    [InlineData(ClaimTypes.Death, ClaimTypes.Injury, ClaimStatuses.Open, false)]       // còn chờ: nâng hồ sơ cũ thành tử vong
    [InlineData(ClaimTypes.Death, ClaimTypes.Injury, ClaimStatuses.UnderReview, false)]
    [InlineData(ClaimTypes.Injury, ClaimTypes.Injury, ClaimStatuses.Paid, false)]
    public void NeedsSupplementaryDeathClaim_OnlyAfterInjuryDecided(string newType, string existingType, string status, bool expected)
    {
        Assert.Equal(expected, ClaimRules.NeedsSupplementaryDeathClaim(newType, existingType, status));
    }
}
