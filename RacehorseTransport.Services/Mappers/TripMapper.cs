using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class TripMapper
    {
        // Cần Include: Vehicle, Crew -> User, TripHorses
        public static TripSummaryModel ToSummaryModel(this Trip entity)
        {
            var model = new TripSummaryModel();
            FillSummary(model, entity);
            return model;
        }

        // Cần Include thêm: PlannedBy, ApprovedBy, ClosedBy, Checkpoints, Handovers,
        // TripHorses -> BookingHorse -> Horse / Dossier / Booking -> Customer
        public static TripDetailModel ToDetailModel(this Trip entity)
        {
            var model = new TripDetailModel
            {
                VehicleCapacity = entity.Vehicle?.CapacityHorses,
                PlannedCost = entity.PlannedCost,
                ActualCost = entity.ActualCost,
                PlannedByUserId = entity.PlannedByUserId,
                PlannedByName = entity.PlannedBy?.FullName,
                ApprovedByUserId = entity.ApprovedByUserId,
                ApprovedByName = entity.ApprovedBy?.FullName,
                ApprovedAt = entity.ApprovedAt,
                PlanRejectionCount = entity.PlanRejectionCount,
                PlanRejectionReason = entity.PlanRejectionReason,
                DelayMinutes = entity.DelayMinutes,
                OnTimeStatus = entity.OnTimeStatus,
                KpiScore = entity.KpiScore,
                ClosedByName = entity.ClosedBy?.FullName,
                ClosedAt = entity.ClosedAt,
                ExecutiveRemarks = entity.ExecutiveRemarks,
                CreatedAt = entity.CreatedAt,
                Horses = entity.TripHorses
                    .OrderBy(th => th.StallSlotNumber)
                    .Select(th => th.ToBusinessModel())
                    .ToList(),
                // Mốc đang hiệu lực đứng trước mốc đã bị thay thế có cùng thứ tự
                Checkpoints = entity.Checkpoints
                    .OrderBy(c => c.SequenceOrder)
                    .ThenByDescending(c => c.IsActive)
                    .ThenBy(c => c.CheckpointId)
                    .Select(c => c.ToBusinessModel())
                    .ToList(),
                Bookings = entity.TripHorses
                    .Where(th => th.BookingHorse?.Booking != null)
                    .GroupBy(th => th.BookingHorse!.BookingId)
                    .Select(g =>
                    {
                        var booking = g.First().BookingHorse!.Booking!;
                        return new TripBookingModel
                        {
                            BookingId = booking.BookingId,
                            BookingCode = booking.BookingCode,
                            CustomerName = booking.Customer?.FullName,
                            CustomerPhone = booking.Customer?.PhoneNumber,
                            PickupAddress = booking.PickupAddress,
                            DropoffAddress = booking.DropoffAddress,
                            HorseCountOnTrip = g.Count(),
                            HandoverCompleted = entity.Handovers.Any(h => h.BookingId == booking.BookingId)
                        };
                    })
                    .OrderBy(b => b.BookingId)
                    .ToList()
            };

            FillSummary(model, entity);
            return model;
        }

        public static TripHorseModel ToBusinessModel(this TripHorse entity)
        {
            var bookingHorse = entity.BookingHorse;
            var horse = bookingHorse?.Horse;

            return new TripHorseModel
            {
                TripHorseId = entity.TripHorseId,
                StallSlotNumber = entity.StallSlotNumber,
                Notes = entity.Notes,
                BookingHorseId = entity.BookingHorseId,
                BookingId = bookingHorse?.BookingId ?? 0,
                BookingCode = bookingHorse?.Booking?.BookingCode ?? string.Empty,
                HorseId = bookingHorse?.HorseId ?? 0,
                HorseName = horse?.Name ?? string.Empty,
                MicrochipNumber = horse?.MicrochipNumber ?? string.Empty,
                Gender = horse?.Gender ?? string.Empty,
                Color = horse?.Color ?? string.Empty,
                SpecialCareRequirements = horse?.SpecialCareRequirements,
                PhotoUrl = horse?.PhotoUrl,
                StallClass = bookingHorse?.StallClass ?? string.Empty,
                DossierStatus = bookingHorse?.Dossier?.Status
            };
        }

        public static TripCheckpointModel ToBusinessModel(this RouteCheckpoint entity)
        {
            return new TripCheckpointModel
            {
                CheckpointId = entity.CheckpointId,
                SequenceOrder = entity.SequenceOrder,
                CheckpointName = entity.CheckpointName,
                CheckpointType = entity.CheckpointType,
                Address = entity.Address,
                PlannedTime = entity.PlannedTime,
                MandatoryRestMinutes = entity.MandatoryRestMinutes,
                ActualArrivalTime = entity.ActualArrivalTime,
                ActualDepartureTime = entity.ActualDepartureTime,
                Status = entity.Status,
                IsActive = entity.IsActive
            };
        }

        private static void FillSummary(TripSummaryModel model, Trip entity)
        {
            model.TripId = entity.TripId;
            model.TripCode = entity.TripCode;
            model.OverallStatus = entity.OverallStatus;
            model.VehicleId = entity.VehicleId;
            model.VehicleCode = entity.Vehicle?.AssetCode;
            model.VehicleType = entity.Vehicle?.AssetType;
            model.PlannedStartDate = entity.PlannedStartDate;
            model.PlannedEndDate = entity.PlannedEndDate;
            model.ActualStartDate = entity.ActualStartDate;
            model.ActualEndDate = entity.ActualEndDate;
            model.HorseCount = entity.TripHorses.Count;
            model.Crew = entity.Crew
                .OrderBy(c => c.CrewRole)
                .ThenByDescending(c => c.IsLead)
                .Select(c => new TripCrewModel
                {
                    UserId = c.UserId,
                    FullName = c.User?.FullName ?? string.Empty,
                    PhoneNumber = c.User?.PhoneNumber,
                    CrewRole = c.CrewRole,
                    IsLead = c.IsLead,
                    Duty = c.Duty
                })
                .ToList();
        }
    }
}
