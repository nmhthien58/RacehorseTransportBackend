using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IPriceItemService
    {
        Task<PagedBusinessResult<PriceItemModel>> GetPriceItemsAsync(PriceItemQueryOptions options);
        Task<PriceItemModel> GetPriceItemByIdAsync(int id);
        Task<PriceItemModel> CreatePriceItemAsync(PriceItemModel model);
        Task<PriceItemModel> UpdatePriceItemAsync(int id, PriceItemModel model);
        Task<PriceItemModel> SetActiveAsync(int id, bool isActive);
    }
}
