using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class PolicySettingsTests
{
    [Fact]
    public void Defaults_MatchServicePolicyDocument()
    {
        // Giá trị trong ngoặc vuông của docs/CHINH_SACH_DICH_VU.md
        var m = new PolicySettingsModel();

        Assert.Equal("1.0", m.PolicyVersion);
        Assert.Equal(3, m.QuoteAcceptDays);
        Assert.Equal(6, m.DelayGraceHours);
        Assert.Equal(5, m.DelayPercentPer12Hours);
        Assert.Equal(25, m.DelayMaxPercent);
        Assert.Equal(1, m.InsurancePremiumPercent);
        Assert.Equal(5000, m.InsuranceBaseLiabilityUsd);
        Assert.Equal(1000, m.InsuranceBaseVetCapUsd);
        Assert.Equal(20, m.InsuranceVetCapPercent);
        Assert.Equal(50000, m.InsuranceProofThresholdUsd);
        Assert.Equal(6, m.HorseMinAgeMonths);
        Assert.Equal(2, m.DeliveryFreeWaitingHours);
        Assert.Equal(40, m.DeliveryWaitingFeePerHourUsd);
        Assert.Equal(24, m.DeliveryMaxWaitingHours);
        Assert.Equal(24, m.DeliveryDecisionHours);
    }

    [Fact]
    public void Validate_AcceptsDefaults()
    {
        SystemSettingService.ValidatePolicySettings(new PolicySettingsModel());
    }

    [Fact]
    public void Validate_TrimsVersion()
    {
        var m = new PolicySettingsModel { PolicyVersion = "  1.1 " };

        SystemSettingService.ValidatePolicySettings(m);

        Assert.Equal("1.1", m.PolicyVersion);
    }

    public static IEnumerable<object[]> InvalidSettings()
    {
        yield return Case("Policy.Version", m => m.PolicyVersion = " ");
        yield return Case("Policy.Version", m => m.PolicyVersion = new string('1', 21));
        yield return Case("Booking.QuoteAcceptDays", m => m.QuoteAcceptDays = 0);
        yield return Case("Delay.GraceHours", m => m.DelayGraceHours = -1);
        yield return Case("Delay.PercentPer12Hours", m => m.DelayPercentPer12Hours = 100.01m);
        yield return Case("Delay.MaxPercent", m => m.DelayMaxPercent = -5);
        yield return Case("Insurance.PremiumPercent", m => m.InsurancePremiumPercent = 101);
        yield return Case("Insurance.BaseLiabilityUsd", m => m.InsuranceBaseLiabilityUsd = -1);
        yield return Case("Insurance.BaseVetCapUsd", m => m.InsuranceBaseVetCapUsd = -0.01m);
        yield return Case("Insurance.VetCapPercent", m => m.InsuranceVetCapPercent = 150);
        yield return Case("Insurance.ProofThresholdUsd", m => m.InsuranceProofThresholdUsd = -100);
        yield return Case("Horse.MinAgeMonths", m => m.HorseMinAgeMonths = -1);
        yield return Case("Delivery.FreeWaitingHours", m => m.DeliveryFreeWaitingHours = -2);
        yield return Case("Delivery.WaitingFeePerHourUsd", m => m.DeliveryWaitingFeePerHourUsd = -40);
        yield return Case("Delivery.MaxWaitingHours", m => m.DeliveryMaxWaitingHours = 0);
        yield return Case("Delivery.DecisionHours", m => m.DeliveryDecisionHours = 169);
    }

    private static object[] Case(string key, Action<PolicySettingsModel> change) => new object[] { key, change };

    [Theory]
    [MemberData(nameof(InvalidSettings))]
    public void Validate_RejectsInvalidValue_NamingTheKey(string key, Action<PolicySettingsModel> change)
    {
        var m = new PolicySettingsModel();
        change(m);

        var ex = Assert.Throws<BusinessException>(() => SystemSettingService.ValidatePolicySettings(m));

        Assert.Contains(key, ex.Message);
    }

    [Fact]
    public void Validate_ListsEveryProblemInOneError()
    {
        var m = new PolicySettingsModel { DelayMaxPercent = 200, InsurancePremiumPercent = -1 };

        var ex = Assert.Throws<BusinessException>(() => SystemSettingService.ValidatePolicySettings(m));

        Assert.Contains("Delay.MaxPercent", ex.Message);
        Assert.Contains("Insurance.PremiumPercent", ex.Message);
    }

    [Fact]
    public void Validate_AcceptsBoundaryValues()
    {
        var m = new PolicySettingsModel
        {
            QuoteAcceptDays = 1,
            DelayGraceHours = 0,
            DelayPercentPer12Hours = 0,
            DelayMaxPercent = 100,
            InsurancePremiumPercent = 0,
            InsuranceVetCapPercent = 100,
            HorseMinAgeMonths = 0,
            DeliveryFreeWaitingHours = 0,
            DeliveryWaitingFeePerHourUsd = 0,
            DeliveryMaxWaitingHours = 168,
            DeliveryDecisionHours = 1
        };

        SystemSettingService.ValidatePolicySettings(m);
    }
}
