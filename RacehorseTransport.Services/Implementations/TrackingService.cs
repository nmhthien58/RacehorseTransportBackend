using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    public class TrackingService : ITrackingService
    {
        // Client gọi lại định kỳ để "theo dõi real-time": cache ngắn để nhiều lần gọi liên tiếp không dội vào DB,
        // và được xóa ngay khi đoàn xe cập nhật hành trình.
        private static readonly TimeSpan TrackingCacheTtl = TimeSpan.FromSeconds(30);

        private static readonly string[] OpenTripStatuses =
        {
            TripStatuses.Scheduled, TripStatuses.InTransit, TripStatuses.EmergencyRerouting, TripStatuses.ArrivedDestination
        };

        private static readonly string[] OnTheRoadStatuses = { TripStatuses.InTransit, TripStatuses.EmergencyRerouting };

        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IRepository<HorseWelfareLog> _welfareLogRepo;
        private readonly ICacheService _cache;
        private readonly IFileStorageService _fileStorage;

        public TrackingService(
            IRepository<Trip> tripRepo,
            IRepository<BookingRequest> bookingRepo,
            IRepository<TripHorse> tripHorseRepo,
            IRepository<HorseWelfareLog> welfareLogRepo,
            ICacheService cache,
            IFileStorageService fileStorage)
        {
            _fileStorage = fileStorage;
            _tripRepo = tripRepo;
            _bookingRepo = bookingRepo;
            _tripHorseRepo = tripHorseRepo;
            _welfareLogRepo = welfareLogRepo;
            _cache = cache;
        }

        public async Task<IReadOnlyList<TripProgressModel>> GetTripProgressAsync(string? status)
        {
            var query = _tripRepo.GetQueryable().AsNoTracking().AsSplitQuery();

            if (string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => OpenTripStatuses.Contains(t.OverallStatus));
            }
            else
            {
                if (!TripStatuses.All.Contains(status))
                {
                    throw new BusinessException($"Status '{status}' is invalid. Allowed values: {string.Join(", ", TripStatuses.All)}.");
                }
                query = query.Where(t => t.OverallStatus == status);
            }

            var trips = await query
                .Include(t => t.Vehicle)
                .Include(t => t.Crew).ThenInclude(c => c.User)
                .Include(t => t.Checkpoints)
                .Include(t => t.TripHorses)
                .Include(t => t.Incidents)
                .OrderBy(t => t.PlannedStartDate)
                .ThenBy(t => t.TripId)
                .ToListAsync();

            // Số nhật ký cần chú ý (bỏ ăn / kích động) của từng chuyến
            var tripIds = trips.Select(t => t.TripId).ToList();
            var alertCounts = await _welfareLogRepo.GetQueryable()
                .Where(l => tripIds.Contains(l.TripHorse!.TripId)
                    && (l.FeedStatus == FeedStatuses.Refused || l.StressLevel == StressLevels.Agitated))
                .GroupBy(l => l.TripHorse!.TripId)
                .Select(g => new { TripId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TripId, x => x.Count);

            var now = DateTimeOffset.UtcNow;
            return trips.Select(t => ToProgress(t, alertCounts.GetValueOrDefault(t.TripId), now)).ToList();
        }

        public async Task<BookingTrackingModel> GetBookingTrackingAsync(int bookingId, CurrentUser currentUser)
        {
            // Kiểm tra quyền trên dữ liệu thật trước, rồi mới trả bản cache
            var access = await _bookingRepo.GetQueryable()
                .Where(b => b.BookingId == bookingId)
                .Select(b => new { b.CustomerUserId, b.AssignedSpecialistId })
                .FirstOrDefaultAsync();

            if (access == null)
            {
                throw new NotFoundException($"Booking with ID {bookingId} was not found.");
            }

            bool canView = currentUser.IsCustomer
                ? access.CustomerUserId == currentUser.UserId
                : currentUser.Role == UserRoles.TransportSpecialist
                    ? access.AssignedSpecialistId == currentUser.UserId
                    : currentUser.Role != UserRoles.DriverEscort;

            if (!canView)
            {
                throw new ForbiddenException("You do not have access to this booking.");
            }

            var tracking = await _cache.GetOrSetAsync(
                CacheKeys.BookingTracking(bookingId),
                () => BuildBookingTrackingAsync(bookingId),
                TrackingCacheTtl);

            // Bản cache giữ đường dẫn ảnh; link xem được gắn vào sau khi lấy ra
            var logs = tracking.Trips
                .SelectMany(t => t.Horses)
                .SelectMany(h => h.LatestLog == null ? h.Logs : h.Logs.Append(h.LatestLog))
                .ToList();
            await _fileStorage.ResolveViewUrlsAsync(logs, l => l.PhotoUrl, (l, url) => l.PhotoUrl = url);

            return tracking;
        }

        public async Task InvalidateTripAsync(int tripId)
        {
            var bookingIds = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId)
                .Select(th => th.BookingHorse!.BookingId)
                .Distinct()
                .ToListAsync();

            foreach (int bookingId in bookingIds)
            {
                await _cache.RemoveAsync(CacheKeys.BookingTracking(bookingId));
            }
        }

        private async Task<BookingTrackingModel> BuildBookingTrackingAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Horse)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.TripHorses).ThenInclude(th => th.WelfareLogs).ThenInclude(l => l.Checkpoint)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.TripHorses).ThenInclude(th => th.WelfareLogs).ThenInclude(l => l.RecordedBy)
                .FirstAsync(b => b.BookingId == bookingId);

            // Chỉ lấy các chuyến còn hiệu lực đang chở ngựa của đơn này
            var tripIds = booking.BookingHorses
                .SelectMany(bh => bh.TripHorses)
                .Select(th => th.TripId)
                .Distinct()
                .ToList();

            var trips = await _tripRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(t => tripIds.Contains(t.TripId) && t.OverallStatus != TripStatuses.Cancelled)
                .Include(t => t.Vehicle)
                .Include(t => t.Crew).ThenInclude(c => c.User)
                .Include(t => t.Checkpoints)
                .Include(t => t.Handovers)
                .OrderBy(t => t.PlannedStartDate)
                .ThenBy(t => t.TripId)
                .ToListAsync();

            var activeBookingHorses = booking.BookingHorses
                .Where(bh => bh.Status != BookingHorseStatuses.Cancelled)
                .OrderBy(bh => bh.BookingHorseId)
                .ToList();

            var liveTripIds = trips.Select(t => t.TripId).ToHashSet();

            return new BookingTrackingModel
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
                Status = booking.Status,
                PickupAddress = booking.PickupAddress,
                DropoffAddress = booking.DropoffAddress,
                DepartureDate = booking.DepartureDate,
                DeliveryDate = booking.DeliveryDate,
                GeneratedAt = DateTimeOffset.UtcNow,
                HorsesAwaitingPlanning = activeBookingHorses
                    .Where(bh => !bh.TripHorses.Any(th => liveTripIds.Contains(th.TripId)))
                    .Select(bh => bh.Horse!.Name)
                    .ToList(),
                Trips = trips.Select(trip =>
                {
                    var route = ActiveRoute(trip);
                    return new TrackingTripModel
                    {
                        TripId = trip.TripId,
                        TripCode = trip.TripCode,
                        OverallStatus = trip.OverallStatus,
                        VehicleCode = trip.Vehicle?.AssetCode,
                        LeadDriverName = LeadDriver(trip)?.User?.FullName,
                        PlannedStartDate = trip.PlannedStartDate,
                        PlannedEndDate = trip.PlannedEndDate,
                        ActualStartDate = trip.ActualStartDate,
                        ActualEndDate = trip.ActualEndDate,
                        DelayMinutes = CurrentDelay(route),
                        HandoverCompleted = trip.Handovers.Any(h => h.BookingId == bookingId),
                        Checkpoints = route.Select(c => new TrackingCheckpointModel
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
                            DelayMinutes = Delay(c)
                        }).ToList(),
                        // Khách chỉ thấy ngựa của đơn mình, không thấy ngựa của đơn khác đi chung chuyến
                        Horses = activeBookingHorses
                            .SelectMany(bh => bh.TripHorses.Where(th => th.TripId == trip.TripId).Select(th => new { BookingHorse = bh, TripHorse = th }))
                            .OrderBy(x => x.TripHorse.StallSlotNumber)
                            .Select(x =>
                            {
                                var logs = x.TripHorse.WelfareLogs
                                    .OrderByDescending(l => l.LoggedAt)
                                    .ThenByDescending(l => l.LogId)
                                    .Select(l => ToLogModel(l, x.BookingHorse.Horse!))
                                    .ToList();

                                return new TrackingHorseModel
                                {
                                    TripHorseId = x.TripHorse.TripHorseId,
                                    HorseId = x.BookingHorse.HorseId,
                                    HorseName = x.BookingHorse.Horse!.Name,
                                    StallSlotNumber = x.TripHorse.StallSlotNumber,
                                    LatestLog = logs.FirstOrDefault(),
                                    Logs = logs
                                };
                            })
                            .ToList()
                    };
                }).ToList()
            };
        }

        private static TripProgressModel ToProgress(Trip trip, int welfareAlertCount, DateTimeOffset now)
        {
            var route = ActiveRoute(trip);
            var lastReached = route.LastOrDefault(c => c.ActualArrivalTime != null);
            var next = route.FirstOrDefault(c => c.ActualArrivalTime == null);
            var leadDriver = LeadDriver(trip);
            int delay = CurrentDelay(route);

            return new TripProgressModel
            {
                TripId = trip.TripId,
                TripCode = trip.TripCode,
                OverallStatus = trip.OverallStatus,
                VehicleCode = trip.Vehicle?.AssetCode,
                LeadDriverName = leadDriver?.User?.FullName,
                LeadDriverPhone = leadDriver?.User?.PhoneNumber,
                PlannedStartDate = trip.PlannedStartDate,
                PlannedEndDate = trip.PlannedEndDate,
                ActualStartDate = trip.ActualStartDate,
                ActualEndDate = trip.ActualEndDate,
                HorseCount = trip.TripHorses.Count,
                CheckpointsTotal = route.Count,
                CheckpointsReached = route.Count(c => c.ActualArrivalTime != null),
                LastCheckpointName = lastReached?.CheckpointName,
                LastCheckpointStatus = lastReached?.Status,
                NextCheckpointName = next?.CheckpointName,
                NextCheckpointPlannedTime = next?.PlannedTime,
                DelayMinutes = delay,
                // Trễ nếu đã tới mốc gần nhất muộn hơn kế hoạch, hoặc đang trên đường mà đã quá giờ dự kiến của mốc kế tiếp
                IsBehindSchedule = delay > 0
                    || (OnTheRoadStatuses.Contains(trip.OverallStatus) && next != null && next.PlannedTime < now),
                OpenIncidentCount = trip.Incidents.Count(i => i.Status != IncidentStatuses.Resolved),
                WelfareAlertCount = welfareAlertCount
            };
        }

        private static List<RouteCheckpoint> ActiveRoute(Trip trip)
        {
            return trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
        }

        private static TripCrew? LeadDriver(Trip trip)
        {
            return trip.Crew
                .Where(c => c.CrewRole == CrewRoles.Driver)
                .OrderByDescending(c => c.IsLead)
                .FirstOrDefault();
        }

        // Số phút tới mốc muộn hơn kế hoạch (tới sớm tính là 0); null nếu chưa tới
        private static int? Delay(RouteCheckpoint checkpoint)
        {
            if (checkpoint.ActualArrivalTime == null)
            {
                return null;
            }
            return Math.Max(0, (int)Math.Round((checkpoint.ActualArrivalTime.Value - checkpoint.PlannedTime).TotalMinutes));
        }

        private static int CurrentDelay(List<RouteCheckpoint> route)
        {
            var lastReached = route.LastOrDefault(c => c.ActualArrivalTime != null);
            return lastReached == null ? 0 : Delay(lastReached) ?? 0;
        }

        private static WelfareLogModel ToLogModel(HorseWelfareLog log, Horse horse)
        {
            return new WelfareLogModel
            {
                LogId = log.LogId,
                TripHorseId = log.TripHorseId,
                HorseId = horse.HorseId,
                HorseName = horse.Name,
                CheckpointId = log.CheckpointId,
                CheckpointName = log.Checkpoint?.CheckpointName,
                CabinTemp = log.CabinTemp,
                WaterIntakeLiters = log.WaterIntakeLiters,
                FeedStatus = log.FeedStatus,
                StressLevel = log.StressLevel,
                NeedsAttention = WelfareLogMapper.NeedsAttention(log.FeedStatus, log.StressLevel),
                Notes = log.Notes,
                PhotoUrl = log.PhotoUrl,
                RecordedByUserId = log.RecordedByUserId,
                RecordedByName = log.RecordedBy?.FullName,
                LoggedAt = log.LoggedAt
            };
        }
    }
}
