using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;

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
            var activeRoute = entity.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
            var eta = LiveEtaCalculator.Calculate(activeRoute, entity.OverallStatus, DateTimeOffset.UtcNow);
            var bookingCodes = entity.TripHorses
                .Where(th => th.BookingHorse?.Booking != null)
                .Select(th => th.BookingHorse!.Booking!)
                .DistinctBy(b => b.BookingId)
                .ToDictionary(b => b.BookingId, b => b.BookingCode);

            var model = new TripDetailModel
            {
                VehicleCapacity = entity.Vehicle?.CapacityHorses,
                VehicleCompartmentCount = entity.Vehicle?.CompartmentCount,
                LiveDelayMinutes = eta.DelayMinutes,
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
                    .Select(th =>
                    {
                        var horse = th.ToBusinessModel();
                        horse.EscortName = entity.Crew.FirstOrDefault(c => c.UserId == th.EscortUserId)?.User?.FullName;
                        return horse;
                    })
                    .ToList(),
                // Mốc đang hiệu lực đứng trước mốc đã bị thay thế có cùng thứ tự
                Checkpoints = entity.Checkpoints
                    .OrderBy(c => c.SequenceOrder)
                    .ThenByDescending(c => c.IsActive)
                    .ThenBy(c => c.CheckpointId)
                    .Select(c =>
                    {
                        var checkpoint = c.ToBusinessModel();
                        checkpoint.BookingCode = c.BookingId.HasValue && bookingCodes.TryGetValue(c.BookingId.Value, out var code) ? code : null;
                        checkpoint.EstimatedArrivalTime = eta.EstimatedArrivals.TryGetValue(c.CheckpointId, out var at) ? at : null;
                        return checkpoint;
                    })
                    .ToList(),
                Bookings = entity.TripHorses
                    .Where(th => th.BookingHorse?.Booking != null)
                    .GroupBy(th => th.BookingHorse!.BookingId)
                    .Select(g =>
                    {
                        var booking = g.First().BookingHorse!.Booking!;
                        var pickup = activeRoute.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.PickUp && c.BookingId == booking.BookingId)
                            ?? activeRoute.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.Origin);
                        var dropoff = activeRoute.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.DropOff && c.BookingId == booking.BookingId)
                            ?? activeRoute.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.Destination);
                        return new TripBookingModel
                        {
                            PickupCheckpointId = pickup?.CheckpointId,
                            DropoffCheckpointId = dropoff?.CheckpointId,
                            EstimatedDeliveryAt = dropoff?.PlannedTime,
                            LiveEstimatedDeliveryAt = dropoff == null ? null : eta.EstimatedArrivals[dropoff.CheckpointId],
                            DeliveryDeadline = booking.DeliveryDeadline,
                            DeliveredHorseCount = g.Count(th => th.Status == TripHorseStatuses.Delivered),
                            OffloadedHorseCount = g.Count(th => th.Status == TripHorseStatuses.Offloaded),
                            BookingId = booking.BookingId,
                            BookingCode = booking.BookingCode,
                            CustomerName = booking.Customer?.FullName,
                            CustomerPhone = booking.Customer?.PhoneNumber,
                            PickupAddress = booking.PickupAddress,
                            DropoffAddress = booking.DropoffAddress,
                            HorseCountOnTrip = g.Count(),
                            // Biên bản ở điểm trả của đơn; biên bản giao tại chỗ (R-12) ký ở mốc khác nên không tính.
                            // Biên bản cũ chưa ghi mốc (checkpoint_id null) vẫn được tính.
                            HandoverCompleted = entity.Handovers.Any(h => h.BookingId == booking.BookingId
                                && (h.CheckpointId == null || h.CheckpointId == dropoff?.CheckpointId))
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
                CompartmentNo = entity.CompartmentNo,
                PositionNo = entity.PositionNo,
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
                DossierStatus = bookingHorse?.Dossier?.Status,
                EscortUserId = entity.EscortUserId,
                Status = entity.Status,
                PickedUpAt = entity.PickedUpAt,
                DeliveredAt = entity.DeliveredAt,
                OffloadedAt = entity.OffloadedAt,
                OffloadReason = entity.OffloadReason,
                OffloadOutcome = entity.OffloadOutcome,
                VetClinic = entity.ToVetClinic(),
                VetExpenseAmount = entity.VetExpenseAmount,
                VetExpenseBearer = entity.VetExpenseBearer,
                VetExpenseNote = entity.VetExpenseNote
            };
        }

        public static VetClinicModel? ToVetClinic(this TripHorse entity)
        {
            return entity.VetClinicName == null ? null : new VetClinicModel
            {
                Name = entity.VetClinicName,
                Address = entity.VetClinicAddress ?? string.Empty,
                Phone = entity.VetClinicPhone,
                AttendingVetName = entity.AttendingVetName,
                Latitude = entity.VetClinicLat,
                Longitude = entity.VetClinicLng
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
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                DistanceKmFromPrevious = entity.DistanceKmFromPrevious,
                PlannedTime = entity.PlannedTime,
                MandatoryRestMinutes = entity.MandatoryRestMinutes,
                ActualArrivalTime = entity.ActualArrivalTime,
                ActualDepartureTime = entity.ActualDepartureTime,
                Status = entity.Status,
                IsActive = entity.IsActive,
                BookingId = entity.BookingId
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
                    Duty = c.Duty,
                    AssignedHorseCount = c.CrewRole == CrewRoles.Escort
                        ? entity.TripHorses.Count(th => th.EscortUserId == c.UserId && TripHorseStatuses.Holding.Contains(th.Status))
                        : null
                })
                .ToList();
        }
    }
}
