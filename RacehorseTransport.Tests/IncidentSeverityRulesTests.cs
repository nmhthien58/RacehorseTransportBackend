using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests
{
    public class IncidentSeverityRulesTests
    {
        [Fact]
        public void PreservesInitialSeverity_WhenUpgradedToCritical()
        {
            string initialSeverity = IncidentSeverities.Moderate;
            string upgraded = IncidentSeverityRules.EffectiveSeverityOnApproval(initialSeverity, OffloadOutcomes.Deceased);

            Assert.Equal(IncidentSeverities.Critical, upgraded);
            Assert.Equal(IncidentSeverities.Moderate, initialSeverity);
        }

        [Theory]
        [InlineData(OffloadOutcomes.WaitRecover)]
        [InlineData(OffloadOutcomes.ReturnToOrigin)]
        [InlineData(OffloadOutcomes.HandOverHere)]
        [InlineData(null)]
        public void DoesNotUpgradeSeverity_WhenOutcomeIsNotDeceased(string? outcome)
        {
            string initialSeverity = IncidentSeverities.Moderate;
            string result = IncidentSeverityRules.EffectiveSeverityOnApproval(initialSeverity, outcome);

            Assert.Equal(IncidentSeverities.Moderate, result);
        }
    }
}
