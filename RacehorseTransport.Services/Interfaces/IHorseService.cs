using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IHorseService
    {
        Task<PagedBusinessResult<HorseModel>> GetHorsesAsync(HorseFilterOptions options, CurrentUser currentUser);
        Task<HorseModel> GetHorseByIdAsync(int id, CurrentUser currentUser);
        Task<HorseModel> CreateHorseAsync(HorseModel model, CurrentUser currentUser);
        Task<HorseModel> UpdateHorseAsync(int id, HorseModel model, CurrentUser currentUser);
        Task DeleteHorseAsync(int id, CurrentUser currentUser);
    }
}
