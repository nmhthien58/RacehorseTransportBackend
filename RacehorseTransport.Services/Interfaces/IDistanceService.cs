using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IDistanceService
    {
        /// <summary>
        /// Calculates or estimates ground transportation distance in kilometers and transit duration.
        /// Uses configured map API (e.g., Goong) if available; otherwise falls back to intelligent multi-hub route matrix.
        /// </summary>
        Task<DistanceEstimateResultModel> EstimateGroundDistanceAsync(DistanceEstimateInputModel input, CancellationToken cancellationToken = default);
    }
}
