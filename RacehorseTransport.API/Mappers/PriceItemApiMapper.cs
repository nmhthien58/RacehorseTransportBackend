using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class PriceItemApiMapper
    {
        public static PriceItemQueryOptions ToBusinessOptions(this PriceItemQueryRequest request)
        {
            return new PriceItemQueryOptions
            {
                Search = request.Search,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size,
                Category = NormalizeOptional(request.Category),
                TransportMode = NormalizeOptional(request.TransportMode),
                IsActive = request.IsActive,
                DestCountryCode = NormalizeOptional(request.DestCountryCode)?.ToUpperInvariant(),
                GlobalOnly = request.GlobalOnly
            };
        }

        public static PriceItemModel ToBusinessModel(this CreatePriceItemRequest request)
        {
            return MapRequest(request);
        }

        public static PriceItemModel ToBusinessModel(this UpdatePriceItemRequest request, int id)
        {
            var model = MapRequest(request);
            model.PriceItemId = id;
            return model;
        }

        public static PriceItemResponse ToResponse(this PriceItemModel model)
        {
            return new PriceItemResponse
            {
                PriceItemId = model.PriceItemId,
                Category = model.Category,
                ItemCode = model.ItemCode,
                ItemName = model.ItemName,
                TransportMode = model.TransportMode,
                DestCountryCode = model.DestCountryCode,
                CalcType = model.CalcType,
                Value = model.Value,
                MinHorses = model.MinHorses,
                IsActive = model.IsActive
            };
        }

        private static PriceItemModel MapRequest(CreatePriceItemRequest request)
        {
            return new PriceItemModel
            {
                Category = request.Category.Trim(),
                ItemCode = request.ItemCode.Trim().ToUpperInvariant(),
                ItemName = request.ItemName.Trim(),
                TransportMode = NormalizeOptional(request.TransportMode),
                DestCountryCode = NormalizeOptional(request.DestCountryCode)?.ToUpperInvariant(),
                CalcType = request.CalcType.Trim(),
                Value = request.Value,
                MinHorses = request.MinHorses,
                IsActive = true
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
