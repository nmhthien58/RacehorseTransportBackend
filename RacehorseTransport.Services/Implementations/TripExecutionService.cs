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
    public class TripExecutionService : ITripExecutionService
    {
        // Chuyến đang trên đường (kể cả lúc đang nắn tuyến vì sự cố)
        private static readonly string[] OnTheRoadStatuses = { TripStatuses.InTransit, TripStatuses.EmergencyRerouting };

        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IRepository<RouteCheckpoint> _checkpointRepo;
        private readonly IRepository<TransportAsset> _assetRepo;
        private readonly IRepository<HorseWelfareLog> _welfareLogRepo;
        private readonly ITripService _tripService;
        private readonly ITrackingService _trackingService;
        private readonly INotificationService _notificationService;
        private readonly IFileStorageService _fileStorage;
        private readonly IUnitOfWork _unitOfWork;

        public TripExecutionService(
            IRepository<Trip> tripRepo,
            IRepository<TripHorse> tripHorseRepo,
            IRepository<RouteCheckpoint> checkpointRepo,
            IRepository<TransportAsset> assetRepo,
            IRepository<HorseWelfareLog> welfareLogRepo,
            ITripService tripService,
            ITrackingService trackingService,
            INotificationService notificationService,
            IFileStorageService fileStorage,
            IUnitOfWork unitOfWork)
        {
            _tripRepo = tripRepo;
            _tripHorseRepo = tripHorseRepo;
            _checkpointRepo = checkpointRepo;
            _assetRepo = assetRepo;
            _welfareLogRepo = welfareLogRepo;
            _tripService = tripService;
            _trackingService = trackingService;
            _notificationService = notificationService;
            _fileStorage = fileStorage;
            _unitOfWork = unitOfWork;
        }

        // =============================================================================================
        // Xuất phát
        // =============================================================================================

        public async Task<TripDetailModel> StartTripAsync(int tripId, CurrentUser currentUser)
        {
            var trip = await GetTripForCrewAsync(tripId, currentUser);

            if (trip.OverallStatus != TripStatuses.Scheduled)
            {
                throw new BusinessException($"Only a scheduled trip can depart (current status: {trip.OverallStatus}).");
            }

            // Chặn cứng: mọi con ngựa trên chuyến phải có hồ sơ đã thông quan
            var notCleared = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId
                    && (th.BookingHorse!.Dossier == null || th.BookingHorse.Dossier.Status != DossierStatuses.Cleared))
                .Select(th => th.BookingHorse!.Horse!.Name)
                .ToListAsync();

            if (notCleared.Count > 0)
            {
                throw new BusinessException($"The trip cannot depart: clearance is not complete for {string.Join(", ", notCleared)}.");
            }

            // R-02: phần còn lại của giá báo phải trả đủ trước khi xuất phát (đơn tạo trước khi có thanh toán thì bỏ qua)
            var unpaidBookings = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId)
                .Select(th => th.BookingHorse!.Booking!)
                .Where(b => b.DepositPercent != null)
                .Distinct()
                .Select(b => new
                {
                    b.BookingCode,
                    b.EstimatedCost,
                    Paid = b.Payments
                        .Where(p => p.Status == PaymentStatuses.Succeeded
                            && (p.PaymentType == PaymentTypes.Deposit || p.PaymentType == PaymentTypes.Balance))
                        .Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            var notPaid = unpaidBookings.Where(b => b.Paid < b.EstimatedCost).Select(b => b.BookingCode).ToList();
            if (notPaid.Count > 0)
            {
                throw new BusinessException($"The trip cannot depart: the remaining balance is not paid for {string.Join(", ", notPaid)}.");
            }

            var origin = ActiveRoute(trip).FirstOrDefault();
            if (origin == null || origin.CheckpointType != CheckpointTypes.Origin)
            {
                throw new BusinessException("The trip has no origin checkpoint.");
            }

            var customerIds = await GetCustomerIdsAsync(tripId);
            var now = DateTimeOffset.UtcNow;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                trip.OverallStatus = TripStatuses.InTransit;
                trip.ActualStartDate = now;

                origin.ActualArrivalTime ??= now;
                origin.ActualDepartureTime = now;
                origin.Status = CheckpointStatuses.Departed;

                var vehicle = await _assetRepo.GetByIdAsync(trip.VehicleId);
                if (vehicle != null)
                {
                    vehicle.Status = AssetStatuses.InTransit;
                }

                await _tripRepo.UpdateAsync(trip);

                await _notificationService.SendToUsersAsync(
                    WithPlanner(customerIds, trip),
                    $"Trip {trip.TripCode} has departed",
                    $"The transport left {origin.CheckpointName}. You can follow each checkpoint and the horses' condition from the tracking screen.",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);
            });

            await _trackingService.InvalidateTripAsync(tripId);
            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        // =============================================================================================
        // Check-in từng mốc: Pending -> Arrived -> (Cleared, chỉ cửa khẩu) -> Departed
        // =============================================================================================

        public async Task<TripDetailModel> ArriveAtCheckpointAsync(int checkpointId, CurrentUser currentUser)
        {
            var (trip, checkpoint) = await GetCheckpointForCrewAsync(checkpointId, currentUser);

            if (checkpoint.Status != CheckpointStatuses.Pending)
            {
                throw new BusinessException($"Arrival at '{checkpoint.CheckpointName}' has already been recorded (status: {checkpoint.Status}).");
            }

            // Phải đi đúng thứ tự: mọi mốc phía trước đã rời đi
            var notLeft = ActiveRoute(trip)
                .Where(c => c.SequenceOrder < checkpoint.SequenceOrder && c.Status != CheckpointStatuses.Departed)
                .FirstOrDefault();
            if (notLeft != null)
            {
                throw new BusinessException($"Record the departure from '{notLeft.CheckpointName}' before arriving at '{checkpoint.CheckpointName}'.");
            }

            bool isDestination = checkpoint.CheckpointType == CheckpointTypes.Destination;
            var customerIds = await GetCustomerIdsAsync(trip.TripId);
            var bookingRecipients = isDestination
                ? await _tripHorseRepo.GetQueryable().GetBookingRecipientsAsync(trip.TripId)
                : new List<TripBookingRecipient>();
            var now = DateTimeOffset.UtcNow;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                checkpoint.Status = CheckpointStatuses.Arrived;
                checkpoint.ActualArrivalTime = now;

                if (isDestination)
                {
                    trip.OverallStatus = TripStatuses.ArrivedDestination;
                    trip.ActualEndDate = now;
                }

                await _tripRepo.UpdateAsync(trip);

                await _notificationService.SendToUsersAsync(
                    isDestination ? WithPlanner(customerIds, trip) : customerIds,
                    isDestination ? $"Trip {trip.TripCode} has reached the destination" : $"Trip {trip.TripCode} arrived at {checkpoint.CheckpointName}",
                    isDestination
                        ? $"The horses have arrived at {checkpoint.CheckpointName} and are ready for handover."
                        : $"The transport reached {checkpoint.CheckpointName} ({checkpoint.CheckpointType}).",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);

                // Chỉ tới điểm giao mới gửi email (bookingRecipients rỗng với mốc khác)
                foreach (var recipient in bookingRecipients)
                {
                    await _notificationService.EmailCustomerAsync(
                        recipient.CustomerUserId,
                        EmailTemplateNames.HorseArrived,
                        CustomerPages.Trip(trip.TripId),
                        new Dictionary<string, string>
                        {
                            ["BookingCode"] = recipient.BookingCode,
                            ["HorseName"] = recipient.HorseNames,
                            ["Destination"] = checkpoint.CheckpointName
                        });
                }
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await _tripService.GetTripByIdAsync(trip.TripId, currentUser);
        }

        // Chỉ cửa khẩu mới có bước "đã thông quan"
        public async Task<TripDetailModel> ClearCheckpointAsync(int checkpointId, CurrentUser currentUser)
        {
            var (trip, checkpoint) = await GetCheckpointForCrewAsync(checkpointId, currentUser);

            if (checkpoint.CheckpointType != CheckpointTypes.BorderGate)
            {
                throw new BusinessException("Only a border gate checkpoint can be marked as cleared.");
            }
            if (checkpoint.Status != CheckpointStatuses.Arrived)
            {
                throw new BusinessException($"'{checkpoint.CheckpointName}' can only be cleared after arrival (status: {checkpoint.Status}).");
            }

            var customerIds = await GetCustomerIdsAsync(trip.TripId);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                checkpoint.Status = CheckpointStatuses.Cleared;
                await _checkpointRepo.UpdateAsync(checkpoint);

                await _notificationService.SendToUsersAsync(
                    customerIds,
                    $"Trip {trip.TripCode} cleared {checkpoint.CheckpointName}",
                    "Border and quarantine clearance is complete at this gate.",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await _tripService.GetTripByIdAsync(trip.TripId, currentUser);
        }

        public async Task<TripDetailModel> DepartCheckpointAsync(int checkpointId, CurrentUser currentUser)
        {
            var (trip, checkpoint) = await GetCheckpointForCrewAsync(checkpointId, currentUser);

            if (checkpoint.CheckpointType == CheckpointTypes.Destination)
            {
                throw new BusinessException("The destination is the last checkpoint; there is no departure from it.");
            }

            bool isBorderGate = checkpoint.CheckpointType == CheckpointTypes.BorderGate;
            if (isBorderGate && checkpoint.Status == CheckpointStatuses.Arrived)
            {
                throw new BusinessException($"'{checkpoint.CheckpointName}' must be cleared before departure.");
            }

            string requiredStatus = isBorderGate ? CheckpointStatuses.Cleared : CheckpointStatuses.Arrived;
            if (checkpoint.Status != requiredStatus)
            {
                throw new BusinessException($"Departure from '{checkpoint.CheckpointName}' cannot be recorded in status {checkpoint.Status}.");
            }

            // Thời gian nghỉ bắt buộc để ngựa hồi sức
            var now = DateTimeOffset.UtcNow;
            double restedMinutes = (now - checkpoint.ActualArrivalTime!.Value).TotalMinutes;
            if (restedMinutes < checkpoint.MandatoryRestMinutes)
            {
                int remaining = (int)Math.Ceiling(checkpoint.MandatoryRestMinutes - restedMinutes);
                throw new BusinessException(
                    $"The horses must rest {checkpoint.MandatoryRestMinutes} minutes at '{checkpoint.CheckpointName}'; {remaining} minute(s) remaining.");
            }

            checkpoint.Status = CheckpointStatuses.Departed;
            checkpoint.ActualDepartureTime = now;
            await _checkpointRepo.UpdateAsync(checkpoint);

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await _tripService.GetTripByIdAsync(trip.TripId, currentUser);
        }

        // =============================================================================================
        // Nhật ký thể trạng ngựa
        // =============================================================================================

        public async Task<WelfareLogModel> AddWelfareLogAsync(int tripId, CreateWelfareLogModel model, CurrentUser currentUser)
        {
            var trip = await GetTripForCrewAsync(tripId, currentUser);

            if (!OnTheRoadStatuses.Contains(trip.OverallStatus) && trip.OverallStatus != TripStatuses.ArrivedDestination)
            {
                throw new BusinessException($"Welfare logs are recorded from departure until handover (current status: {trip.OverallStatus}).");
            }

            if (!FeedStatuses.All.Contains(model.FeedStatus))
            {
                throw new BusinessException($"FeedStatus '{model.FeedStatus}' is invalid. Allowed values: {string.Join(", ", FeedStatuses.All)}.");
            }
            if (!StressLevels.All.Contains(model.StressLevel))
            {
                throw new BusinessException($"StressLevel '{model.StressLevel}' is invalid. Allowed values: {string.Join(", ", StressLevels.All)}.");
            }

            if (!trip.TripHorses.Any(th => th.TripHorseId == model.TripHorseId))
            {
                throw new BusinessException($"Trip horse with ID {model.TripHorseId} is not on trip {trip.TripCode}.");
            }
            if (model.CheckpointId.HasValue && !trip.Checkpoints.Any(c => c.CheckpointId == model.CheckpointId.Value))
            {
                throw new BusinessException($"Checkpoint with ID {model.CheckpointId} does not belong to trip {trip.TripCode}.");
            }

            string? photoPath = null;
            if (model.Photo != null)
            {
                var stored = await _fileStorage.UploadAsync(
                    model.Photo.Content, model.Photo.FileName, model.Photo.SizeBytes, FileCategories.Welfare);
                photoPath = stored.Path;
            }

            var log = new HorseWelfareLog
            {
                TripHorseId = model.TripHorseId,
                CheckpointId = model.CheckpointId,
                RecordedByUserId = currentUser.UserId,
                CabinTemp = model.CabinTemp,
                WaterIntakeLiters = model.WaterIntakeLiters,
                FeedStatus = model.FeedStatus,
                StressLevel = model.StressLevel,
                Notes = model.Notes,
                PhotoUrl = photoPath
            };

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _welfareLogRepo.AddAsync(log);

                if (WelfareLogMapper.NeedsAttention(log.FeedStatus, log.StressLevel) && trip.PlannedByUserId.HasValue)
                {
                    string horseName = await _tripHorseRepo.GetQueryable()
                        .Where(th => th.TripHorseId == log.TripHorseId)
                        .Select(th => th.BookingHorse!.Horse!.Name)
                        .FirstAsync();

                    await _notificationService.SendAsync(
                        trip.PlannedByUserId.Value,
                        $"Welfare alert on trip {trip.TripCode}",
                        $"{horseName}: feed {log.FeedStatus}, stress {log.StressLevel}. {log.Notes}".Trim(),
                        NotificationReferenceTypes.Trip,
                        trip.TripId);
                }
            });

            await _trackingService.InvalidateTripAsync(tripId);

            var saved = await QueryLogs().FirstAsync(l => l.LogId == log.LogId);
            var result = saved.ToBusinessModel();
            await _fileStorage.ResolveViewUrlsAsync(new[] { result }, l => l.PhotoUrl, (l, url) => l.PhotoUrl = url);
            return result;
        }

        // Đoàn xe của chuyến và các role vận hành xem được; khách hàng xem qua màn hình theo dõi đơn
        public async Task<IReadOnlyList<WelfareLogModel>> GetWelfareLogsAsync(int tripId, int? tripHorseId, CurrentUser currentUser)
        {
            // Dùng lại quy tắc "ai được xem chuyến" của TripService (404 / 403)
            await _tripService.GetTripByIdAsync(tripId, currentUser);

            var query = QueryLogs().Where(l => l.TripHorse!.TripId == tripId);
            if (tripHorseId.HasValue)
            {
                query = query.Where(l => l.TripHorseId == tripHorseId.Value);
            }

            var logs = await query.OrderByDescending(l => l.LoggedAt).ThenByDescending(l => l.LogId).ToListAsync();
            var result = logs.Select(l => l.ToBusinessModel()).ToList();
            await _fileStorage.ResolveViewUrlsAsync(result, l => l.PhotoUrl, (l, url) => l.PhotoUrl = url);
            return result;
        }

        // =============================================================================================
        // Hàm phụ
        // =============================================================================================

        private IQueryable<HorseWelfareLog> QueryLogs()
        {
            return _welfareLogRepo.GetQueryable()
                .AsNoTracking()
                .Include(l => l.TripHorse!).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(l => l.Checkpoint)
                .Include(l => l.RecordedBy);
        }

        // Chỉ người có tên trong đoàn xe của chuyến (hoặc Admin) mới được cập nhật hành trình
        private async Task<Trip> GetTripForCrewAsync(int tripId, CurrentUser currentUser)
        {
            var trip = await _tripRepo.GetQueryable()
                .Include(t => t.Crew)
                .Include(t => t.TripHorses)
                .Include(t => t.Checkpoints)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null)
            {
                throw new NotFoundException($"Trip with ID {tripId} was not found.");
            }

            if (currentUser.Role != UserRoles.Admin && !trip.Crew.Any(c => c.UserId == currentUser.UserId))
            {
                throw new ForbiddenException("Only the crew assigned to this trip can update its progress.");
            }

            return trip;
        }

        private async Task<(Trip Trip, RouteCheckpoint Checkpoint)> GetCheckpointForCrewAsync(int checkpointId, CurrentUser currentUser)
        {
            int? tripId = await _checkpointRepo.GetQueryable()
                .Where(c => c.CheckpointId == checkpointId)
                .Select(c => (int?)c.TripId)
                .FirstOrDefaultAsync();

            if (tripId == null)
            {
                throw new NotFoundException($"Checkpoint with ID {checkpointId} was not found.");
            }

            var trip = await GetTripForCrewAsync(tripId.Value, currentUser);
            var checkpoint = trip.Checkpoints.First(c => c.CheckpointId == checkpointId);

            if (!OnTheRoadStatuses.Contains(trip.OverallStatus))
            {
                throw new BusinessException($"Checkpoints can only be updated while the trip is on the road (current status: {trip.OverallStatus}).");
            }
            if (!checkpoint.IsActive)
            {
                throw new BusinessException($"'{checkpoint.CheckpointName}' is no longer part of the route.");
            }

            return (trip, checkpoint);
        }

        private static List<RouteCheckpoint> ActiveRoute(Trip trip)
        {
            return trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
        }

        private async Task<List<int>> GetCustomerIdsAsync(int tripId)
        {
            return await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId)
                .Select(th => th.BookingHorse!.Booking!.CustomerUserId)
                .Distinct()
                .ToListAsync();
        }

        private static List<int> WithPlanner(List<int> userIds, Trip trip)
        {
            var result = new List<int>(userIds);
            if (trip.PlannedByUserId.HasValue)
            {
                result.Add(trip.PlannedByUserId.Value);
            }
            return result;
        }
    }
}
