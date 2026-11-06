using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Services.Implementations
{
    public static class IncidentSeverityRules
    {
        public static string EffectiveSeverityOnApproval(string currentSeverity, string? offloadOutcome)
        {
            if (offloadOutcome == OffloadOutcomes.Deceased)
            {
                return IncidentSeverities.Critical;
            }
            return currentSeverity;
        }
    }
}
