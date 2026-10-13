using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface ITransportAssetService
    {
        Task<PagedBusinessResult<TransportAssetModel>> GetTransportAssetsAsync(TransportAssetQueryOptions options);
        Task<TransportAssetModel> GetTransportAssetByIdAsync(int id);
        Task<TransportAssetModel> CreateTransportAssetAsync(TransportAssetModel model);
        Task<TransportAssetModel> UpdateTransportAssetAsync(int id, TransportAssetModel model);
        Task DeleteTransportAssetAsync(int id);
    }
}
