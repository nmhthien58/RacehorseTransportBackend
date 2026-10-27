using RacehorseTransport.Services.Implementations;
using Xunit;
using V = RacehorseTransport.Services.Implementations.HorseValueRules.HorseValue;

namespace RacehorseTransport.Tests;

public class HorseValueRulesTests
{
    private const decimal Threshold = 50000; // Insurance.ProofThresholdUsd
    private const string ValidPath = "documents/2026/10/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.pdf";

    private static List<string> Validate(params V[] horses) =>
        HorseValueRules.Validate(horses, Threshold, p => p == ValidPath);

    [Fact]
    public void NoValues_IsValid()
    {
        Assert.Empty(Validate(new V("A", null, null, null), new V("B", null, null, null)));
    }

    [Fact]
    public void AllValuesBelowThreshold_NoProofNeeded()
    {
        Assert.Empty(Validate(new V("A", 49999.99m, null, "None"), new V("B", 1000, null, "Full")));
    }

    [Fact]
    public void ValueAtThreshold_NeedsProof()
    {
        var errors = Validate(new V("Midnight Crown", 50000, null, null));

        Assert.Contains(errors, e => e.StartsWith("Midnight Crown:") && e.Contains("proof document"));
    }

    [Fact]
    public void ValueAboveThreshold_WithProof_IsValid()
    {
        Assert.Empty(Validate(new V("A", 250000, ValidPath, "Full")));
    }

    [Fact]
    public void InvalidProofPath_IsRejected()
    {
        var errors = Validate(new V("A", 250000, "not/a/real/path.pdf", null));

        Assert.Contains(errors, e => e.Contains("ValueProofPath is not a valid"));
    }

    [Fact]
    public void SomeHorsesWithoutValue_IsRejected()
    {
        var errors = Validate(new V("A", 1000, null, null), new V("Sakura Wind", null, null, null));

        Assert.Contains(errors, e => e.Contains("Missing: Sakura Wind"));
    }

    [Fact]
    public void NegativeValue_IsRejected()
    {
        Assert.Contains(Validate(new V("A", -1, null, null)), e => e.Contains("cannot be negative"));
    }

    [Fact]
    public void UnknownInsurancePlan_IsRejected()
    {
        Assert.Contains(Validate(new V("A", 1000, null, "Gold")), e => e.Contains("InsurancePlan 'Gold' is invalid"));
    }

    [Fact]
    public void EffectiveValue_PrefersAgreedValue()
    {
        Assert.Equal(80000m, HorseValueRules.EffectiveValue(80000, 100000));
        Assert.Equal(100000m, HorseValueRules.EffectiveValue(null, 100000));
        Assert.Null(HorseValueRules.EffectiveValue(null, null));
    }
}
