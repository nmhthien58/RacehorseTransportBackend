using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;

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
                Horses = request.Horses.Select(h => h.ToBusinessModel()).ToList(),
                Checkpoints = request.Checkpoints?.Select(c => c.ToBusinessModel()).ToList()
            };
        }

        // Ưu tiên CompartmentNo + PositionNo; request cũ chỉ gửi StallSlotNumber thì quy đổi sang khoang.
        // Thiếu cả hai => 0, service báo lỗi rõ ràng.
        public static TripHorseInputModel ToBusinessModel(this TripHorseRequest request)
        {
            var (compartmentNo, positionNo) = request.CompartmentNo.HasValue
                ? (request.CompartmentNo.Value, request.PositionNo ?? 0)
                : request.StallSlotNumber.HasValue
                    ? CompartmentRules.FromStallSlot(request.StallSlotNumber.Value)
                    : (0, 0);

            return new TripHorseInputModel
            {
                BookingHorseId = request.BookingHorseId,
                CompartmentNo = compartmentNo,
                PositionNo = positionNo,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                EscortUserId = request.EscortUserId
            };
        }

        public static CheckpointInputModel ToBusinessModel(this CheckpointRequest request)
        {
            return new CheckpointInputModel
            {
                CheckpointName = request.CheckpointName.Trim(),
                CheckpointType = request.CheckpointType.Trim(),
                Address = request.Address.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                DistanceKmFromPrevious = request.DistanceKmFromPrevious,
                PlannedTime = request.PlannedTime!.Value,
                MandatoryRestMinutes = request.MandatoryRestMinutes,
                BookingId = request.BookingId,
                Position = request.Position
            };
        }

        public static RouteSuggestionRequestModel ToBusinessModel(this SuggestRouteRequest request)
        {
            return new RouteSuggestionRequestModel
            {
                BookingHorseIds = request.BookingHorseIds,
                PlannedStartDate = request.PlannedStartDate!.Value,
                DriverCount = request.DriverCount
            };
        }

        public static RouteSuggestionResponse ToResponse(this RouteSuggestionModel model)
        {
            return new RouteSuggestionResponse
            {
                TransportMode = model.TransportMode,
                PlannedStartDate = model.PlannedStartDate,
                SuggestedEndDate = model.SuggestedEndDate,
                TotalDistanceKm = model.TotalDistanceKm,
                TotalHours = model.TotalHours,
                Checkpoints = model.Checkpoints.Select(c => new SuggestedCheckpointResponse
                {
                    CheckpointName = c.CheckpointName,
                    CheckpointType = c.CheckpointType,
                    Address = c.Address,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    PlannedTime = c.PlannedTime,
                    MandatoryRestMinutes = c.MandatoryRestMinutes,
                    BookingId = c.BookingId,
                    BookingCode = c.BookingCode,
                    LegDistanceKm = c.LegDistanceKm
                }).ToList(),
                Bookings = model.Bookings.Select(b => new RouteBookingEtaResponse
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    PickupAt = b.PickupAt,
                    EstimatedDeliveryAt = b.EstimatedDeliveryAt,
                    DeliveryDeadline = b.DeliveryDeadline,
                    OnTime = b.OnTime,
                    RouteDistanceKm = b.RouteDistanceKm,
                    DirectDistanceKm = b.DirectDistanceKm,
                    DetourRatio = b.DetourRatio
                }).ToList(),
                Warnings = model.Warnings.Select(w => new PlanWarningResponse
                {
                    Code = w.Code,
                    Severity = w.Severity,
                    Message = w.Message,
                    BookingCode = w.BookingCode,
                    Params = w.Params
                }).ToList()
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
