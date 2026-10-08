using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class WelfareLogMapper
    {
        // Cần Include: TripHorse -> BookingHorse -> Horse, Checkpoint, RecordedBy
        public static WelfareLogModel ToBusinessModel(this HorseWelfareLog entity)
        {
            var horse = entity.TripHorse?.BookingHorse?.Horse;

            return new WelfareLogModel
            {
                LogId = entity.LogId,
                TripHorseId = entity.TripHorseId,
                HorseId = horse?.HorseId ?? 0,
                HorseName = horse?.Name ?? string.Empty,
                CheckpointId = entity.CheckpointId,
                CheckpointName = entity.Checkpoint?.CheckpointName,
                CabinTemp = entity.CabinTemp,
                WaterIntakeLiters = entity.WaterIntakeLiters,
                FeedStatus = entity.FeedStatus,
                StressLevel = entity.StressLevel,
                NeedsAttention = NeedsAttention(entity.FeedStatus, entity.StressLevel),
                Notes = entity.Notes,
                PhotoUrl = entity.PhotoUrl,
                RecordedByUserId = entity.RecordedByUserId,
                RecordedByName = entity.RecordedBy?.FullName,
                LoggedAt = entity.LoggedAt
            };
        }

        // Ngựa bỏ ăn hoặc kích động là dấu hiệu cần Coordinator xem ngay
        public static bool NeedsAttention(string feedStatus, string stressLevel)
        {
            return feedStatus == FeedStatuses.Refused || stressLevel == StressLevels.Agitated;
        }
    }
}
