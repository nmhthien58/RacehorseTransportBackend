using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class PriceItemMapper
    {
        public static PriceItemModel ToBusinessModel(this PriceItem entity)
        {
            return new PriceItemModel
            {
                PriceItemId = entity.PriceItemId,
                Category = entity.Category,
                ItemCode = entity.ItemCode,
                ItemName = entity.ItemName,
                TransportMode = entity.TransportMode,
                DestCountryCode = entity.DestCountryCode,
                CalcType = entity.CalcType,
                Value = entity.Value,
                MinHorses = entity.MinHorses,
                IsActive = entity.IsActive
            };
        }

        public static PriceItem ToEntity(this PriceItemModel model)
        {
            return new PriceItem
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
    }
}
