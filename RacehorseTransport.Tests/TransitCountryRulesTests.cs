using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Helpers;
using Xunit;

namespace RacehorseTransport.Tests;

public class TransitCountryRulesTests
{
    [Fact]
    public void Normalize_UppercasesAndKeepsOrder()
    {
        var result = TransitCountryRules.Normalize(new[] { " cn", "kz " }, "VN", "RU");

        Assert.Equal(new[] { "CN", "KZ" }, result);
    }

    [Fact]
    public void Normalize_NullMeansNoTransit()
    {
        Assert.Empty(TransitCountryRules.Normalize(null, "VN", "TH"));
    }

    [Theory]
    [InlineData("CHN")]   // không phải 2 chữ cái
    [InlineData("1A")]
    [InlineData("VN")]    // trùng nước đi
    [InlineData("RU")]    // trùng nước đến
    public void Normalize_RejectsInvalidCodes(string code)
    {
        Assert.Throws<BusinessException>(() => TransitCountryRules.Normalize(new[] { code }, "VN", "RU"));
    }

    [Fact]
    public void Normalize_RejectsDuplicates()
    {
        Assert.Throws<BusinessException>(() => TransitCountryRules.Normalize(new[] { "CN", "cn" }, "VN", "RU"));
    }

    [Fact]
    public void Normalize_RejectsTooManyCountries()
    {
        var codes = new[] { "CN", "MN", "KZ", "UZ", "KG", "TJ" };
        Assert.Throws<BusinessException>(() => TransitCountryRules.Normalize(codes, "VN", "RU"));
    }
}
