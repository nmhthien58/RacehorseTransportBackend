using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class DelayCompensationRulesTests
{
    // Mặc định theo chính sách: ân hạn 6 giờ, 5% mỗi 12 giờ, tối đa 25%
    private static readonly PolicySettingsModel Settings = new();

    private static decimal Percent(double hours) => DelayCompensationRules.Percent(hours * 60, Settings);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5.99, 0)]
    [InlineData(6, 0)]           // đúng 6 giờ: trong ân hạn
    [InlineData(6 + 1 / 60.0, 5)] // 6h01: bắt đầu tính, chưa đủ 12 giờ tính tròn lên
    [InlineData(18, 5)]          // 6 + 12 giờ: vẫn 1 bậc
    [InlineData(18 + 1 / 60.0, 10)]
    [InlineData(30, 10)]
    [InlineData(54, 20)]
    [InlineData(66, 25)]         // 5 bậc = trần
    [InlineData(200, 25)]        // vượt trần vẫn 25%
    public void Percent_FollowsPolicy(double lateHours, decimal expected)
    {
        Assert.Equal(expected, Percent(lateHours));
    }

    [Fact]
    public void Percent_UsesConfiguredValues()
    {
        var settings = new PolicySettingsModel { DelayGraceHours = 2, DelayPercentPer12Hours = 3, DelayMaxPercent = 10 };

        Assert.Equal(3m, DelayCompensationRules.Percent(3 * 60, settings));
        Assert.Equal(10m, DelayCompensationRules.Percent(100 * 60, settings));
    }

    [Fact]
    public void FreightAmount_ExcludesInsuranceAndProceduralFees()
    {
        var lines = new List<QuoteLineModel>
        {
            new() { Code = "FREIGHT_SHARED", Amount = 1800 },
            new() { Code = "DISC_VOL_2", Amount = -90 },
            new() { Code = "SUR_FUEL", Amount = 136.8m },
            new() { Code = "SUR_EXPRESS", Amount = 342 },
            new() { Code = "FEE_PICKUP", Amount = 100 },   // phí điều xe: là cước
            new() { Code = "ESCORT", Amount = 240 },
            new() { Code = "FEE_DOCS", Amount = 700 },     // thủ tục: bỏ
            new() { Code = "FEE_CLEARANCE", Amount = 500 },
            new() { Code = "FEE_QUARANTINE", Amount = 1400 },
            new() { Code = "FEE_TRANSIT", Amount = 300 },
            new() { Code = "INS_TRANSIT", Amount = 1500 }  // bảo hiểm: bỏ
        };

        Assert.Equal(2528.8m, DelayCompensationRules.FreightAmount(lines));
    }

    [Fact]
    public void FreightAmount_WithoutBreakdown_UsesQuoteTotal()
    {
        Assert.Equal(7500m, DelayCompensationRules.FreightAmount(new List<QuoteLineModel>(), 7500));
        Assert.Equal(100m, DelayCompensationRules.FreightAmount(new List<QuoteLineModel> { new() { Code = "FREIGHT", Amount = 100 } }, 7500));
    }

    [Fact]
    public void Amount_IsRoundedPercentOfFreight()
    {
        Assert.Equal(126.44m, DelayCompensationRules.Amount(2528.8m, 5));
        Assert.Equal(632.2m, DelayCompensationRules.Amount(2528.8m, 25));
    }

    [Fact]
    public void DelayHours_IsPositiveHoursAfterDeadline()
    {
        var deadline = new DateTimeOffset(2026, 12, 6, 0, 0, 0, TimeSpan.FromHours(7));

        Assert.Equal(18.5m, DelayCompensationRules.DelayHours(deadline.AddHours(18.5), deadline));
        Assert.Equal(0m, DelayCompensationRules.DelayHours(deadline.AddHours(-3), deadline));
    }

    [Fact]
    public void CompanyFault_WhenNoIncidents()
    {
        Assert.True(DelayCompensationRules.IsCompanyFault(Array.Empty<string?>()));
    }

    [Fact]
    public void CompanyFault_WhenAnyIncidentIsCompany()
    {
        Assert.True(DelayCompensationRules.IsCompanyFault(new string?[] { FaultParties.ForceMajeure, FaultParties.Company }));
    }

    [Fact]
    public void NotCompanyFault_WhenOnlyForceMajeureOrCustomer()
    {
        Assert.False(DelayCompensationRules.IsCompanyFault(new string?[] { FaultParties.ForceMajeure }));
        Assert.False(DelayCompensationRules.IsCompanyFault(new string?[] { FaultParties.Customer, FaultParties.ForceMajeure }));
    }

    [Fact]
    public void ForceMajeureTrip_GetsNoCompensation()
    {
        // Bất khả kháng: không bồi thường dù trễ vượt trần
        bool companyFault = DelayCompensationRules.IsCompanyFault(new string?[] { FaultParties.ForceMajeure });
        decimal percent = companyFault ? Percent(100) : 0;

        Assert.Equal(0m, percent);
    }
}
