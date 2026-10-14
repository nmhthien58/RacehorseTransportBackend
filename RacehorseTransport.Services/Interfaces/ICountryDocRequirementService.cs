using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface ICountryDocRequirementService
    {
        Task<PagedBusinessResult<CountryDocRequirementModel>> GetCountryDocRequirementsAsync(
            CountryDocRequirementQueryOptions options);

        Task<CountryDocRequirementModel> GetCountryDocRequirementByIdAsync(int id);
        Task<CountryDocRequirementModel> CreateCountryDocRequirementAsync(CountryDocRequirementModel model);
        Task<CountryDocRequirementModel> UpdateCountryDocRequirementAsync(int id, CountryDocRequirementModel model);
        Task DeleteCountryDocRequirementAsync(int id);
    }
}
