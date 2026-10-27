using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Implementations;
using Xunit;
using H = RacehorseTransport.Services.Implementations.QuoteRules.InsuredHorse;

namespace RacehorseTransport.Tests;

public class QuoteRulesTests
{
    private static readonly DateTimeOffset Quoted = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void InsuranceLines_OnlyFullHorses_UsesAgreedValueFirst()
    {
        var lines = QuoteRules.InsuranceLines(new[]
        {
            new H("A", InsurancePlans.Full, 100000, 80000),
            new H("B", InsurancePlans.None, 200000, 200000),
            new H("C", InsurancePlans.Full, null, 50000)
        }, 2.5m);

        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal(QuoteRules.InsuranceFullCode, l.Code));
        Assert.Equal(2500m, lines[0].Amount);
        Assert.Equal(1250m, lines[1].Amount);
    }

    [Fact]
    public void InsuranceLines_FullWithoutValue_Skipped()
    {
        Assert.Empty(QuoteRules.InsuranceLines(new[] { new H("A", InsurancePlans.Full, null, null) }, 2.5m));
    }

    [Fact]
    public void Expiry_ExactlyAtDeadline_IsExpired()
    {
        var expires = QuoteRules.ExpiresAt(Quoted, 3);
        Assert.Equal(Quoted.AddDays(3), expires);
        Assert.False(QuoteRules.IsExpired(expires, expires.AddSeconds(-1)));
        Assert.True(QuoteRules.IsExpired(expires, expires));
        Assert.False(QuoteRules.IsExpired(null, expires));
    }

    [Fact]
    public void Reminder_OnceWithinLastDay()
    {
        var expires = Quoted.AddDays(3);
        Assert.False(QuoteRules.NeedsReminder(expires, null, expires.AddDays(-2)));
        Assert.True(QuoteRules.NeedsReminder(expires, null, expires.AddHours(-23)));
        Assert.False(QuoteRules.NeedsReminder(expires, expires.AddHours(-23), expires.AddHours(-1)));
        Assert.False(QuoteRules.NeedsReminder(expires, null, expires));
    }

    [Fact]
    public void Deposit_PercentOfFinalQuote()
    {
        Assert.Equal(3000.33m, QuoteRules.DepositAmount(10001.1m, 30));
    }
}
