using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class TripPlanningApiMapper
    {
        public static TripPlanModel ToBusinessModel(this TripPlanRequest request)
        {
            return new TripPlanModel
            {
                VehicleId = request.VehicleId,
                PlannedStartDate = request.PlannedStartDate!.Value,
                PlannedEndDate = request.PlannedEndDate!.Value,
                PlannedCost = request.PlannedCost,
                Crew = request.Crew.Select(c => new TripCrewInputModel
                {
                    UserId = c.UserId,
                    CrewRole = c.CrewRole.Trim(),
                    IsLead = c.IsLead,
                    Duty = string.IsNullOrWhiteSpace(c.Duty) ? null : c.Duty.Trim()
                }).ToList(),
                Horses = request.Horses.Select(h => new TripHorseInputModel
                {
                    BookingHorseId = h.BookingHorseId,
                    StallSlotNumber = h.StallSlotNumber,
                    Notes = string.IsNullOrWhiteSpace(h.Notes) ? null : h.Notes.Trim()
                }).ToList(),
                Checkpoints = request.Checkpoints?.Select(c => c.ToBusinessModel()).ToList()
            };
        }

        public static CheckpointInputModel ToBusinessModel(this CheckpointRequest request)
        {
            return new CheckpointInputModel
            {
                CheckpointName = request.CheckpointName.Trim(),
                CheckpointType = request.CheckpointType.Trim(),
                Address = request.Address.Trim(),
                PlannedTime = request.PlannedTime!.Value,
                MandatoryRestMinutes = request.MandatoryRestMinutes,
                Position = request.Position
            };
        }

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
                TransitCountryCodes = model.TransitCountryCodes,
                PickupWindowStart = model.PickupWindowStart,
                PickupWindowEnd = model.PickupWindowEnd,
                DeliveryDeadline = model.DeliveryDeadline,
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
