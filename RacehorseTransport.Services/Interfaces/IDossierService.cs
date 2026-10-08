using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IDossierService
    {
        Task<PagedBusinessResult<DossierSummaryModel>> GetDossiersAsync(DossierFilterOptions options, CurrentUser currentUser);
        Task<DossierDetailModel> GetDossierByIdAsync(int id, CurrentUser currentUser);
    }
}
