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
        private readonly IRepository<SystemNotification> _notificationRepo;
        private readonly INotificationService _notificationService;

        public TrackingService(
            IRepository<Trip> tripRepo,
            IRepository<BookingRequest> bookingRepo,
            IRepository<TripHorse> tripHorseRepo,
            IRepository<HorseWelfareLog> welfareLogRepo,
            ICacheService cache,
            IFileStorageService fileStorage,
            IRepository<SystemNotification> notificationRepo,
            INotificationService notificationService)
        {
            _notificationRepo = notificationRepo;
            _notificationService = notificationService;
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
                PickupWindowStart = booking.PickupWindowStart,
                PickupWindowEnd = booking.PickupWindowEnd,
                DeliveryDeadline = booking.DeliveryDeadline,
                GeneratedAt = DateTimeOffset.UtcNow,
                HorsesAwaitingPlanning = activeBookingHorses
                    .Where(bh => !bh.TripHorses.Any(th => liveTripIds.Contains(th.TripId)))
                    .Select(bh => bh.Horse!.Name)
                    .ToList(),
                Trips = trips.Select(trip =>
                {
                    var route = ActiveRoute(trip);
                    var myPickup = TripRouteRules.PickupCheckpoint(route, bookingId);
                    var myDropoff = TripRouteRules.DropoffCheckpoint(route, bookingId);
                    var eta = LiveEtaCalculator.Calculate(route, trip.OverallStatus, DateTimeOffset.UtcNow);
                    DateTimeOffset? liveDelivery = myDropoff == null ? null : eta.EstimatedArrivals[myDropoff.CheckpointId];
                    // Biên bản ở điểm trả của đơn (biên bản giao tại chỗ của con rời chuyến không tính, R-12)
                    bool handedOver = trip.Handovers.Any(h => h.BookingId == bookingId
                        && (h.CheckpointId == null || h.CheckpointId == myDropoff?.CheckpointId));
                    return new TrackingTripModel
                    {
                        PickupCheckpointId = myPickup?.CheckpointId,
                        DropoffCheckpointId = myDropoff?.CheckpointId,
                        EstimatedDeliveryAt = myDropoff?.PlannedTime,
                        LiveEstimatedDeliveryAt = liveDelivery,
                        DeliveryAtRisk = liveDelivery.HasValue && liveDelivery.Value > booking.DeliveryDeadline
                            && !handedOver,
                        TripId = trip.TripId,
                        TripCode = trip.TripCode,
                        OverallStatus = trip.OverallStatus,
                        VehicleCode = trip.Vehicle?.AssetCode,
                        LeadDriverName = LeadDriver(trip)?.User?.FullName,
                        PlannedStartDate = trip.PlannedStartDate,
                        PlannedEndDate = trip.PlannedEndDate,
                        ActualStartDate = trip.ActualStartDate,
                        ActualEndDate = trip.ActualEndDate,
                        DelayMinutes = eta.DelayMinutes,
                        HandoverCompleted = handedOver,
                        Checkpoints = route.Select(c =>
                        {
                            // Khách không được thấy điểm đón / trả của khách khác đi chung chuyến
                            bool otherStop = c.BookingId != null && c.BookingId != bookingId;
                            return new TrackingCheckpointModel
                            {
                                CheckpointId = c.CheckpointId,
                                SequenceOrder = c.SequenceOrder,
                                CheckpointName = otherStop ? string.Empty : c.CheckpointName,
                                CheckpointType = c.CheckpointType,
                                Address = otherStop ? string.Empty : c.Address,
                                PlannedTime = c.PlannedTime,
                                ActualArrivalTime = c.ActualArrivalTime,
                                ActualDepartureTime = c.ActualDepartureTime,
                                Status = c.Status,
                                DelayMinutes = Delay(c),
                                EstimatedArrivalTime = eta.EstimatedArrivals[c.CheckpointId],
                                IsOtherBookingStop = otherStop,
                                IsMyPickup = c.CheckpointId == myPickup?.CheckpointId,
                                IsMyDropoff = c.CheckpointId == myDropoff?.CheckpointId
                            };
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
                                    Status = x.TripHorse.Status,
                                    PickedUpAt = x.TripHorse.PickedUpAt,
                                    DeliveredAt = x.TripHorse.DeliveredAt,
                                    OffloadedAt = x.TripHorse.OffloadedAt,
                                    OffloadReason = x.TripHorse.OffloadReason,
                                    OffloadOutcome = x.TripHorse.OffloadOutcome,
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
            var eta = LiveEtaCalculator.Calculate(route, trip.OverallStatus, now);
            int delay = eta.DelayMinutes;

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
                NextCheckpointEstimatedTime = next == null ? null : eta.EstimatedArrivals[next.CheckpointId],
                EstimatedEndAt = route.Count == 0 ? null : eta.EstimatedArrivals[route[^1].CheckpointId],
                DelayMinutes = delay,
                // Độ trễ theo check-in thực tế (LiveEtaCalculator): đã tính cả trường hợp đang trên đường mà quá giờ mốc kế tiếp
                IsBehindSchedule = delay > 0,
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

        // Sau mỗi lần check-in: đơn nào còn ngựa trên xe mà giờ tới điểm trả dự kiến đã vượt hạn giao thì báo khách
        // (thông báo + email) và điều phối viên. Mỗi đơn chỉ báo một lần: đã có thông báo cùng tiêu đề thì bỏ qua.
        public async Task NotifyLateDeliveriesAsync(int tripId)
        {
            var trip = await _tripRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Checkpoints)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Booking)
                .FirstOrDefaultAsync(t => t.TripId == tripId);
            if (trip == null)
            {
                return;
            }

            var route = ActiveRoute(trip);
            var eta = LiveEtaCalculator.Calculate(route, trip.OverallStatus, DateTimeOffset.UtcNow);
            var bookings = trip.TripHorses
                .Where(th => TripHorseStatuses.OnBoard.Contains(th.Status))
                .Select(th => th.BookingHorse!.Booking!)
                .DistinctBy(b => b.BookingId)
                .ToList();

            foreach (var booking in bookings)
            {
                var dropoff = TripRouteRules.DropoffCheckpoint(route, booking.BookingId);
                if (dropoff == null || dropoff.ActualArrivalTime != null)
                {
                    continue;
                }
                var expected = eta.EstimatedArrivals[dropoff.CheckpointId];
                if (expected <= booking.DeliveryDeadline)
                {
                    continue;
                }

                string title = $"Booking {booking.BookingCode} may arrive late";
                bool alreadySent = await _notificationRepo.GetQueryable()
                    .AnyAsync(n => n.RecipientUserId == booking.CustomerUserId
                        && n.ReferenceType == NotificationReferenceTypes.Booking
                        && n.ReferenceId == booking.BookingId
                        && n.Title == title);
                if (alreadySent)
                {
                    continue;
                }

                string message = $"Trip {trip.TripCode} is running {eta.DelayMinutes} minute(s) behind schedule. "
                    + $"Expected arrival at {dropoff.CheckpointName}: {expected:yyyy-MM-dd HH:mm} UTC, "
                    + $"later than the delivery deadline {booking.DeliveryDeadline:yyyy-MM-dd HH:mm} UTC. Our team will keep you updated.";
                await _notificationService.SendAsync(
                    booking.CustomerUserId, title, message, NotificationReferenceTypes.Booking, booking.BookingId, emailCustomers: true);
                if (trip.PlannedByUserId.HasValue)
                {
                    await _notificationService.SendAsync(
                        trip.PlannedByUserId.Value, title, message, NotificationReferenceTypes.Trip, trip.TripId);
                }
            }
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
