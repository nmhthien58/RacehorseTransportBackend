using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class TrackingApiMapper
    {
        // Stream của ảnh do controller giữ và giải phóng (request.Photo.ToUploadModel())
        public static CreateWelfareLogModel ToBusinessModel(this CreateWelfareLogRequest request, FileUploadModel? photo)
        {
            return new CreateWelfareLogModel
            {
                TripHorseId = request.TripHorseId,
                CheckpointId = request.CheckpointId,
                CabinTemp = request.CabinTemp!.Value,
                WaterIntakeLiters = request.WaterIntakeLiters!.Value,
                FeedStatus = request.FeedStatus.Trim(),
                StressLevel = request.StressLevel.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                Photo = photo
            };
        }

        public static WelfareLogResponse ToResponse(this WelfareLogModel model)
        {
            return new WelfareLogResponse
            {
                LogId = model.LogId,
                TripHorseId = model.TripHorseId,
                HorseId = model.HorseId,
                HorseName = model.HorseName,
                CheckpointId = model.CheckpointId,
                CheckpointName = model.CheckpointName,
                CabinTemp = model.CabinTemp,
                WaterIntakeLiters = model.WaterIntakeLiters,
                FeedStatus = model.FeedStatus,
                StressLevel = model.StressLevel,
                NeedsAttention = model.NeedsAttention,
                Notes = model.Notes,
                PhotoUrl = model.PhotoUrl,
                RecordedByUserId = model.RecordedByUserId,
                RecordedByName = model.RecordedByName,
                LoggedAt = model.LoggedAt
            };
        }

        public static TripProgressResponse ToResponse(this TripProgressModel model)
        {
            return new TripProgressResponse
            {
                TripId = model.TripId,
                TripCode = model.TripCode,
                OverallStatus = model.OverallStatus,
                VehicleCode = model.VehicleCode,
                LeadDriverName = model.LeadDriverName,
                LeadDriverPhone = model.LeadDriverPhone,
                PlannedStartDate = model.PlannedStartDate,
                PlannedEndDate = model.PlannedEndDate,
                ActualStartDate = model.ActualStartDate,
                ActualEndDate = model.ActualEndDate,
                HorseCount = model.HorseCount,
                CheckpointsTotal = model.CheckpointsTotal,
                CheckpointsReached = model.CheckpointsReached,
                LastCheckpointName = model.LastCheckpointName,
                LastCheckpointStatus = model.LastCheckpointStatus,
                NextCheckpointName = model.NextCheckpointName,
                NextCheckpointPlannedTime = model.NextCheckpointPlannedTime,
                NextCheckpointEstimatedTime = model.NextCheckpointEstimatedTime,
                EstimatedEndAt = model.EstimatedEndAt,
                DelayMinutes = model.DelayMinutes,
                IsBehindSchedule = model.IsBehindSchedule,
                OpenIncidentCount = model.OpenIncidentCount,
                WelfareAlertCount = model.WelfareAlertCount
            };
        }

        public static BookingTrackingResponse ToResponse(this BookingTrackingModel model)
        {
            return new BookingTrackingResponse
            {
                BookingId = model.BookingId,
                BookingCode = model.BookingCode,
                Status = model.Status,
                PickupAddress = model.PickupAddress,
                DropoffAddress = model.DropoffAddress,
                PickupWindowStart = model.PickupWindowStart,
                PickupWindowEnd = model.PickupWindowEnd,
                DeliveryDeadline = model.DeliveryDeadline,
                HorsesAwaitingPlanning = model.HorsesAwaitingPlanning,
                GeneratedAt = model.GeneratedAt,
                Trips = model.Trips.Select(t => new TrackingTripResponse
                {
                    TripId = t.TripId,
                    TripCode = t.TripCode,
                    OverallStatus = t.OverallStatus,
                    VehicleCode = t.VehicleCode,
                    LeadDriverName = t.LeadDriverName,
                    PlannedStartDate = t.PlannedStartDate,
                    PlannedEndDate = t.PlannedEndDate,
                    ActualStartDate = t.ActualStartDate,
                    ActualEndDate = t.ActualEndDate,
                    DelayMinutes = t.DelayMinutes,
                    HandoverCompleted = t.HandoverCompleted,
                    PickupCheckpointId = t.PickupCheckpointId,
                    DropoffCheckpointId = t.DropoffCheckpointId,
                    EstimatedDeliveryAt = t.EstimatedDeliveryAt,
                    LiveEstimatedDeliveryAt = t.LiveEstimatedDeliveryAt,
                    DeliveryAtRisk = t.DeliveryAtRisk,
                    Checkpoints = t.Checkpoints.Select(c => new TrackingCheckpointResponse
                    {
                        CheckpointId = c.CheckpointId,
                        SequenceOrder = c.SequenceOrder,
                        CheckpointName = c.CheckpointName,
                        CheckpointType = c.CheckpointType,
                        Address = c.Address,
                        PlannedTime = c.PlannedTime,
                        ActualArrivalTime = c.ActualArrivalTime,
                        ActualDepartureTime = c.ActualDepartureTime,
                        Status = c.Status,
                        DelayMinutes = c.DelayMinutes,
                        EstimatedArrivalTime = c.EstimatedArrivalTime,
                        IsOtherBookingStop = c.IsOtherBookingStop,
                        IsMyPickup = c.IsMyPickup,
                        IsMyDropoff = c.IsMyDropoff
                    }).ToList(),
                    Horses = t.Horses.Select(h => new TrackingHorseResponse
                    {
                        TripHorseId = h.TripHorseId,
                        HorseId = h.HorseId,
                        HorseName = h.HorseName,
                        StallSlotNumber = h.StallSlotNumber,
                        Status = h.Status,
                        PickedUpAt = h.PickedUpAt,
                        DeliveredAt = h.DeliveredAt,
                        OffloadedAt = h.OffloadedAt,
                        OffloadReason = h.OffloadReason,
                        LatestLog = h.LatestLog?.ToResponse(),
                        Logs = h.Logs.Select(l => l.ToResponse()).ToList()
                    }).ToList()
                }).ToList()
            };
        }
    }
}
