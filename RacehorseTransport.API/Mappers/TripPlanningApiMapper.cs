using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class TripPlanningApiMapper
    {
        public static UnplannedHorseResponse ToResponse(this UnplannedHorseModel model)
        {
            return new UnplannedHorseResponse
            {
                BookingHorseId = model.BookingHorseId,
                BookingId = model.BookingId,
                BookingCode = model.BookingCode,
                CustomerName = model.CustomerName,
                TransportMode = model.TransportMode,
                PickupAddress = model.PickupAddress,
                PickupCountryCode = model.PickupCountryCode,
                DropoffAddress = model.DropoffAddress,
                DropoffCountryCode = model.DropoffCountryCode,
                DepartureDate = model.DepartureDate,
                DeliveryDate = model.DeliveryDate,
                RequiresClimateControl = model.RequiresClimateControl,
                HorseId = model.HorseId,
                HorseName = model.HorseName,
                MicrochipNumber = model.MicrochipNumber,
                SpecialCareRequirements = model.SpecialCareRequirements,
                StallClass = model.StallClass,
                DossierStatus = model.DossierStatus
            };
        }

        public static AvailableVehicleResponse ToResponse(this AvailableVehicleModel model)
        {
            return new AvailableVehicleResponse
            {
                AssetId = model.AssetId,
                AssetCode = model.AssetCode,
                AssetType = model.AssetType,
                CapacityHorses = model.CapacityHorses,
                HasClimateControl = model.HasClimateControl,
                HasGpsTracker = model.HasGpsTracker,
                LastSanitizationDate = model.LastSanitizationDate
            };
        }
    }
}
