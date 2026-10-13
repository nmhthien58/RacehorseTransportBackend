using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class TransportAssetMapper
    {
        public static TransportAssetModel ToBusinessModel(this TransportAsset entity)
        {
            return new TransportAssetModel
            {
                AssetId = entity.AssetId,
                AssetCode = entity.AssetCode,
                AssetType = entity.AssetType,
                CapacityHorses = entity.CapacityHorses,
                HasClimateControl = entity.HasClimateControl,
                HasGpsTracker = entity.HasGpsTracker,
                Status = entity.Status,
                LastSanitizationDate = entity.LastSanitizationDate
            };
        }

        public static TransportAsset ToEntity(this TransportAssetModel model)
        {
            return new TransportAsset
            {
                AssetId = model.AssetId,
                AssetCode = model.AssetCode,
                AssetType = model.AssetType,
                CapacityHorses = model.CapacityHorses,
                HasClimateControl = model.HasClimateControl,
                HasGpsTracker = model.HasGpsTracker,
                Status = model.Status,
                LastSanitizationDate = model.LastSanitizationDate
            };
        }
    }
}
