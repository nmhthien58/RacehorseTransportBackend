using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface ITripService
    {
        Task<PagedBusinessResult<TripSummaryModel>> GetTripsAsync(TripFilterOptions options, CurrentUser currentUser);
        Task<TripDetailModel> GetTripByIdAsync(int id, CurrentUser currentUser);
    }
}
