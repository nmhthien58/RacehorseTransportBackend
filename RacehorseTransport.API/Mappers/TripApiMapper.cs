using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class TripApiMapper
    {
        public static TripFilterOptions ToBusinessOptions(this TripQueryRequest request)
        {
            return new TripFilterOptions
            {
                Search = request.Search,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size,
                Status = request.Status,
                StartFrom = request.StartFrom,
                StartTo = request.StartTo
            };
        }

        public static TripSummaryResponse ToResponse(this TripSummaryModel model)
        {
            var response = new TripSummaryResponse();
            FillSummary(response, model);
            return response;
        }

        public static TripDetailResponse ToResponse(this TripDetailModel model)
        {
            var response = new TripDetailResponse
            {
                VehicleCapacity = model.VehicleCapacity,
                VehicleCompartmentCount = model.VehicleCompartmentCount,
                PositionsPerCompartment = VehicleCompartments.PositionsPerCompartment,
                LiveDelayMinutes = model.LiveDelayMinutes,
                PlannedCost = model.PlannedCost,
                ActualCost = model.ActualCost,
                PlannedByUserId = model.PlannedByUserId,
                PlannedByName = model.PlannedByName,
                ApprovedByUserId = model.ApprovedByUserId,
                ApprovedByName = model.ApprovedByName,
                ApprovedAt = model.ApprovedAt,
                PlanRejectionCount = model.PlanRejectionCount,
                PlanRejectionReason = model.PlanRejectionReason,
                DelayMinutes = model.DelayMinutes,
                OnTimeStatus = model.OnTimeStatus,
                KpiScore = model.KpiScore,
                ClosedByName = model.ClosedByName,
                ClosedAt = model.ClosedAt,
                ExecutiveRemarks = model.ExecutiveRemarks,
                CreatedAt = model.CreatedAt,
                Horses = model.Horses.Select(h => h.ToResponse()).ToList(),
                Checkpoints = model.Checkpoints.Select(c => new TripCheckpointResponse
                {
                    CheckpointId = c.CheckpointId,
                    SequenceOrder = c.SequenceOrder,
                    CheckpointName = c.CheckpointName,
                    CheckpointType = c.CheckpointType,
                    Address = c.Address,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    DistanceKmFromPrevious = c.DistanceKmFromPrevious,
                    PlannedTime = c.PlannedTime,
                    MandatoryRestMinutes = c.MandatoryRestMinutes,
                    ActualArrivalTime = c.ActualArrivalTime,
                    ActualDepartureTime = c.ActualDepartureTime,
                    Status = c.Status,
                    IsActive = c.IsActive,
                    BookingId = c.BookingId,
                    BookingCode = c.BookingCode,
                    EstimatedArrivalTime = c.EstimatedArrivalTime
                }).ToList(),
                Bookings = model.Bookings.Select(b => new TripBookingResponse
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    CustomerName = b.CustomerName,
                    CustomerPhone = b.CustomerPhone,
                    PickupAddress = b.PickupAddress,
                    DropoffAddress = b.DropoffAddress,
                    HorseCountOnTrip = b.HorseCountOnTrip,
                    HandoverCompleted = b.HandoverCompleted,
                    PickupCheckpointId = b.PickupCheckpointId,
                    DropoffCheckpointId = b.DropoffCheckpointId,
                    EstimatedDeliveryAt = b.EstimatedDeliveryAt,
                    LiveEstimatedDeliveryAt = b.LiveEstimatedDeliveryAt,
                    DeliveryDeadline = b.DeliveryDeadline,
                    DeliveredHorseCount = b.DeliveredHorseCount,
                    OffloadedHorseCount = b.OffloadedHorseCount
                }).ToList()
            };

            FillSummary(response, model);
            return response;
        }

        private static void FillSummary(TripSummaryResponse response, TripSummaryModel model)
        {
            response.TripId = model.TripId;
            response.TripCode = model.TripCode;
            response.OverallStatus = model.OverallStatus;
            response.VehicleId = model.VehicleId;
            response.VehicleCode = model.VehicleCode;
            response.VehicleType = model.VehicleType;
            response.PlannedStartDate = model.PlannedStartDate;
            response.PlannedEndDate = model.PlannedEndDate;
            response.ActualStartDate = model.ActualStartDate;
            response.ActualEndDate = model.ActualEndDate;
            response.HorseCount = model.HorseCount;
            response.Crew = model.Crew.Select(c => new TripCrewResponse
            {
                UserId = c.UserId,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                CrewRole = c.CrewRole,
                IsLead = c.IsLead,
                Duty = c.Duty,
                AssignedHorseCount = c.AssignedHorseCount
            }).ToList();
        }

        public static TripHorseResponse ToResponse(this TripHorseModel h)
        {
            return new TripHorseResponse
            {
                TripHorseId = h.TripHorseId,
                CompartmentNo = h.CompartmentNo,
                PositionNo = h.PositionNo,
                StallSlotNumber = h.StallSlotNumber,
                Notes = h.Notes,
                BookingHorseId = h.BookingHorseId,
                BookingId = h.BookingId,
                BookingCode = h.BookingCode,
                HorseId = h.HorseId,
                HorseName = h.HorseName,
                MicrochipNumber = h.MicrochipNumber,
                Gender = h.Gender,
                Color = h.Color,
                SpecialCareRequirements = h.SpecialCareRequirements,
                PhotoUrl = h.PhotoUrl,
                StallClass = h.StallClass,
                DossierStatus = h.DossierStatus,
                EscortUserId = h.EscortUserId,
                EscortName = h.EscortName,
                Status = h.Status,
                PickedUpAt = h.PickedUpAt,
                DeliveredAt = h.DeliveredAt,
                OffloadedAt = h.OffloadedAt,
                OffloadReason = h.OffloadReason,
                OffloadOutcome = h.OffloadOutcome,
                VetClinic = h.VetClinic,
                VetExpenseAmount = h.VetExpenseAmount,
                VetExpenseBearer = h.VetExpenseBearer,
                VetExpenseNote = h.VetExpenseNote
            };
        }
    }
}
