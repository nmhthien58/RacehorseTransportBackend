using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class TransportAssetApiMapper
    {
        public static TransportAssetQueryOptions ToBusinessOptions(this TransportAssetQueryRequest request)
        {
            return new TransportAssetQueryOptions
            {
                Search = request.Search,
                Status = request.Status,
                AssetType = request.AssetType,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size
            };
        }

        public static TransportAssetModel ToBusinessModel(this CreateTransportAssetRequest request)
        {
            return new TransportAssetModel
            {
                AssetCode = request.AssetCode.Trim().ToUpperInvariant(),
                AssetType = request.AssetType.Trim(),
                CapacityHorses = request.CapacityHorses,
                CompartmentCount = request.CompartmentCount ?? 0,
                HasClimateControl = request.HasClimateControl,
                HasGpsTracker = request.HasGpsTracker,
                Status = request.Status.Trim(),
                LastSanitizationDate = request.LastSanitizationDate
            };
        }

        public static TransportAssetModel ToBusinessModel(this UpdateTransportAssetRequest request, int id)
        {
            return new TransportAssetModel
            {
                AssetId = id,
                AssetCode = request.AssetCode.Trim().ToUpperInvariant(),
                AssetType = request.AssetType.Trim(),
                CapacityHorses = request.CapacityHorses,
                CompartmentCount = request.CompartmentCount ?? 0,
                HasClimateControl = request.HasClimateControl,
                HasGpsTracker = request.HasGpsTracker,
                Status = request.Status.Trim(),
                LastSanitizationDate = request.LastSanitizationDate
            };
        }

        public static TransportAssetResponse ToResponse(this TransportAssetModel model)
        {
            return new TransportAssetResponse
            {
                AssetId = model.AssetId,
                AssetCode = model.AssetCode,
                AssetType = model.AssetType,
                CapacityHorses = model.CapacityHorses,
                CompartmentCount = model.CompartmentCount,
                PositionsPerCompartment = VehicleCompartments.PositionsPerCompartment,
                HasClimateControl = model.HasClimateControl,
                HasGpsTracker = model.HasGpsTracker,
                Status = model.Status,
                LastSanitizationDate = model.LastSanitizationDate
            };
        }
    }
}
