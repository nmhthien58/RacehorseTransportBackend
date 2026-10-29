using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    public class TripExecutionService : ITripExecutionService
    {
        // Chuyến đang trên đường (kể cả lúc đang nắn tuyến vì sự cố)
        private static readonly string[] OnTheRoadStatuses = { TripStatuses.InTransit, TripStatuses.EmergencyRerouting };

        // Ngựa đã rời chuyến không còn tính vào kiểm tra xuất phát
        private static readonly string[] LeftTripStatuses = { TripHorseStatuses.Offloaded, TripHorseStatuses.Deceased, TripHorseStatuses.Cancelled };

        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IRepository<RouteCheckpoint> _checkpointRepo;
        private readonly IRepository<TransportAsset> _assetRepo;
        private readonly IRepository<HorseWelfareLog> _welfareLogRepo;
        private readonly ITripService _tripService;
        private readonly ITrackingService _trackingService;
        private readonly INotificationService _notificationService;
        private readonly IRecipientService _recipientService;
        private readonly IRepository<DeliveryFailure> _deliveryFailureRepo;
        private readonly IFileStorageService _fileStorage;
        private readonly IRepository<HorseDocument> _horseDocumentRepo;
        private readonly IRepository<DocumentType> _docTypeRepo;
        private readonly IRepository<RoutePlan> _routePlanRepo;
        private readonly ITransitTimeEstimator _transitEstimator;
        private readonly BookingOptions _bookingOptions;
        private readonly TransitOptions _transitOptions;
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
            IRecipientService recipientService,
            IRepository<DeliveryFailure> deliveryFailureRepo,
            IFileStorageService fileStorage,
            IRepository<HorseDocument> horseDocumentRepo,
            IRepository<DocumentType> docTypeRepo,
            IRepository<RoutePlan> routePlanRepo,
            ITransitTimeEstimator transitEstimator,
            IOptions<BookingOptions> bookingOptions,
            IOptions<TransitOptions> transitOptions,
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
            _recipientService = recipientService;
            _deliveryFailureRepo = deliveryFailureRepo;
            _fileStorage = fileStorage;
            _horseDocumentRepo = horseDocumentRepo;
            _docTypeRepo = docTypeRepo;
            _routePlanRepo = routePlanRepo;
            _transitEstimator = transitEstimator;
            _bookingOptions = bookingOptions.Value;
            _transitOptions = transitOptions.Value;
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

            // BE-10: kiểm mọi điều kiện tại giờ xuất phát thực tế, báo hết lỗi một lần
            var now = DateTimeOffset.UtcNow;
            var check = await BuildDepartureCheckAsync(trip, now);
            if (!check.IsReady)
            {
                throw new BusinessException(DepartureCheckRules.ToBlockingMessage(check));
            }

            var origin = ActiveRoute(trip).FirstOrDefault();
            if (origin == null || origin.CheckpointType != CheckpointTypes.Origin)
            {
                throw new BusinessException("The trip has no origin checkpoint.");
            }

            var customerIds = await GetCustomerIdsAsync(tripId);

            // R-05: ngựa của các đơn không có mốc PickUp riêng lên xe ở điểm xuất phát
            var bookingIdsWithPickup = ActiveRoute(trip)
                .Where(c => c.CheckpointType == CheckpointTypes.PickUp)
                .Select(c => c.BookingId!.Value)
                .ToHashSet();
            var horseBookingIds = await GetHorseBookingIdsAsync(tripId);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                trip.OverallStatus = TripStatuses.InTransit;
                trip.ActualStartDate = now;

                origin.ActualArrivalTime ??= now;
                origin.ActualDepartureTime = now;
                origin.Status = CheckpointStatuses.Departed;

                foreach (var tripHorse in trip.TripHorses.Where(th => th.Status == TripHorseStatuses.Planned
                    && !bookingIdsWithPickup.Contains(horseBookingIds[th.TripHorseId])))
                {
                    tripHorse.Status = TripHorseStatuses.InTransit;
                    tripHorse.PickedUpAt = now;
                }

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

            // R-16: email người nhận kèm giờ dự kiến tới (ghi khung giờ đã báo)
            await _recipientService.OnTripStartedAsync(tripId);

            await _trackingService.InvalidateTripAsync(tripId);
            await _trackingService.NotifyLateDeliveriesAsync(tripId);
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
            bool isDropOff = checkpoint.CheckpointType == CheckpointTypes.DropOff;
            bool isBookingStop = CheckpointTypes.BookingStops.Contains(checkpoint.CheckpointType);

            // Báo tin: mốc đón / trả của một đơn chỉ báo cho khách của đơn đó (không lộ điểm của khách khác);
            // mốc chung báo cho khách còn ngựa trên xe. Email "ngựa đã tới" gửi cho đơn được trả ở mốc này.
            var onBoardBookingIds = await GetOnBoardBookingIdsAsync(trip.TripId);
            var routeNow = ActiveRoute(trip);
            var recipientsAll = await _tripHorseRepo.GetQueryable().GetBookingRecipientsAsync(trip.TripId);
            var dropHere = recipientsAll
                .Where(r => (isDropOff && checkpoint.BookingId == r.BookingId)
                    || (isDestination && !routeNow.Any(c => c.CheckpointType == CheckpointTypes.DropOff && c.BookingId == r.BookingId)))
                .Where(r => onBoardBookingIds.Contains(r.BookingId))
                .ToList();
            var customerIds = isBookingStop
                ? recipientsAll.Where(r => r.BookingId == checkpoint.BookingId).Select(r => r.CustomerUserId).Distinct().ToList()
                : recipientsAll.Where(r => onBoardBookingIds.Contains(r.BookingId)).Select(r => r.CustomerUserId).Distinct().ToList();
            var bookingRecipients = dropHere;
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

                bool isHandoverStop = isDestination || isDropOff;
                await _notificationService.SendToUsersAsync(
                    isHandoverStop ? WithPlanner(customerIds, trip) : customerIds,
                    isHandoverStop ? $"Trip {trip.TripCode} has reached {checkpoint.CheckpointName}" : $"Trip {trip.TripCode} arrived at {checkpoint.CheckpointName}",
                    isHandoverStop
                        ? $"The horses have arrived at {checkpoint.CheckpointName} and are ready for handover."
                        : $"The transport reached {checkpoint.CheckpointName} ({checkpoint.CheckpointType}).",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);

                // Chỉ tới điểm giao (DropOff / Destination) mới gửi email cho các đơn được trả ở đây
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
            await _trackingService.NotifyLateDeliveriesAsync(trip.TripId);
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

            var horseBookingIds = await GetHorseBookingIdsAsync(trip.TripId);
            var bookingHorses = trip.TripHorses
                .Where(th => checkpoint.BookingId != null && horseBookingIds[th.TripHorseId] == checkpoint.BookingId)
                .ToList();

            // R-04: chưa ký biên bản bàn giao cho đơn được trả ở mốc này thì xe chưa được đi tiếp
            // R-16: đã ghi "không giao được" ở mốc này thì được đi tiếp (ngựa xử lý theo quyết định của khách)
            if (checkpoint.CheckpointType == CheckpointTypes.DropOff
                && bookingHorses.Any(th => TripHorseStatuses.OnBoard.Contains(th.Status))
                && !await _deliveryFailureRepo.ExistsAsync(f => f.CheckpointId == checkpoint.CheckpointId && f.BookingId == checkpoint.BookingId))
            {
                throw new BusinessException($"Record the handover (e-POD) of the horses dropped off at '{checkpoint.CheckpointName}' before leaving.");
            }

            checkpoint.Status = CheckpointStatuses.Departed;
            checkpoint.ActualDepartureTime = now;

            // R-05: rời mốc đón của một đơn = ngựa của đơn đó đã lên xe
            if (checkpoint.CheckpointType == CheckpointTypes.PickUp)
            {
                foreach (var tripHorse in bookingHorses.Where(th => th.Status == TripHorseStatuses.Planned))
                {
                    tripHorse.Status = TripHorseStatuses.InTransit;
                    tripHorse.PickedUpAt = now;
                }
            }

            await _checkpointRepo.UpdateAsync(checkpoint);

            // R-16: rời mốc ngay trước điểm giao của đơn nào thì báo người nhận đơn đó xe sắp tới
            await _recipientService.OnCheckpointDepartedAsync(trip.TripId, checkpoint.CheckpointId);

            await _trackingService.InvalidateTripAsync(trip.TripId);
            await _trackingService.NotifyLateDeliveriesAsync(trip.TripId);
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

            var loggedHorse = trip.TripHorses.FirstOrDefault(th => th.TripHorseId == model.TripHorseId);
            if (loggedHorse == null)
            {
                throw new BusinessException($"Trip horse with ID {model.TripHorseId} is not on trip {trip.TripCode}.");
            }
            // R-06: escort chỉ ghi nhật ký cho ngựa mình phụ trách; trưởng nhóm escort (và Admin) ghi được cho mọi con
            bool isLeadEscort = trip.Crew.Any(c => c.UserId == currentUser.UserId && c.CrewRole == CrewRoles.Escort && c.IsLead);
            if (currentUser.Role != UserRoles.Admin && loggedHorse.EscortUserId.HasValue
                && loggedHorse.EscortUserId != currentUser.UserId && !isLeadEscort)
            {
                throw new ForbiddenException("Only the escort in charge of this horse (or the lead escort) can record its welfare log.");
            }
            if (loggedHorse.Status != TripHorseStatuses.InTransit)
            {
                throw new BusinessException($"Welfare logs are recorded while the horse is on board (horse status: {loggedHorse.Status}).");
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
        // Kiểm tra trước khi xuất phát (BE-10)
        // =============================================================================================

        public async Task<DepartureCheckModel> GetDepartureCheckAsync(int tripId, CurrentUser currentUser)
        {
            // Dùng lại quy tắc "ai được xem chuyến" của TripService (404 / 403)
            await _tripService.GetTripByIdAsync(tripId, currentUser);

            var trip = await _tripRepo.GetQueryable()
                .AsNoTracking()
                .Include(t => t.Crew)
                .Include(t => t.Checkpoints)
                .FirstAsync(t => t.TripId == tripId);

            // Xem trước giờ dự kiến thì tính tại giờ dự kiến: không báo "sẵn sàng" cho giấy sẽ hết hạn trước lúc đi.
            // Bấm xuất phát luôn tính tại giờ bấm, nên kết quả ở đây không bao giờ dễ hơn lúc bấm.
            var now = DateTimeOffset.UtcNow;
            var departureAt = trip.PlannedStartDate > now ? trip.PlannedStartDate : now;
            return await BuildDepartureCheckAsync(trip, departureAt);
        }

        private async Task<DepartureCheckModel> BuildDepartureCheckAsync(Trip trip, DateTimeOffset departureAt)
        {
            int? fitExamTypeId = await _docTypeRepo.GetQueryable()
                .Where(t => t.Code == DocumentTypeCodes.FitExam)
                .Select(t => (int?)t.DocTypeId)
                .FirstOrDefaultAsync();

            var horses = await _tripHorseRepo.GetQueryable()
                .AsNoTracking()
                .Where(th => th.TripId == trip.TripId && !LeftTripStatuses.Contains(th.Status))
                .Select(th => new
                {
                    th.BookingHorse!.HorseId,
                    HorseName = th.BookingHorse.Horse!.Name,
                    th.BookingHorse.Booking!.BookingCode,
                    DossierStatus = th.BookingHorse.Dossier == null ? null : th.BookingHorse.Dossier.Status,
                    Documents = th.BookingHorse.Dossier!.Documents
                        .Where(d => d.Status != DocumentStatuses.Rejected)
                        .Select(d => new { d.DocTypeId, d.DocumentType!.Name, d.Status, d.ExpiryDate, d.UploadedAt })
                        .ToList()
                })
                .ToListAsync();

            // Giấy khám thường có sau khi hồ sơ đã thông quan (hồ sơ đã khóa), nên khách nộp vào hồ sơ thú y của ngựa
            var horseIds = horses.Select(h => h.HorseId).Distinct().ToList();
            var fitExams = fitExamTypeId == null
                ? new List<(int HorseId, DateOnly IssuedOn)>()
                : (await _horseDocumentRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(d => horseIds.Contains(d.HorseId) && d.DocTypeId == fitExamTypeId.Value)
                    .Select(d => new { d.HorseId, d.IssueDate, d.UploadedAt })
                    .ToListAsync())
                    .Select(d => (d.HorseId, IssuedOn: d.IssueDate ?? DateOnly.FromDateTime(d.UploadedAt.UtcDateTime)))
                    .ToList();

            // R-02: đơn tạo trước khi có thanh toán (DepositPercent = null) không bắt trả tiền
            var bookings = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == trip.TripId && !LeftTripStatuses.Contains(th.Status))
                .Select(th => th.BookingHorse!.Booking!)
                .Where(b => b.DepositPercent != null)
                .Distinct()
                .Select(b => new DepartureBookingInput
                {
                    BookingCode = b.BookingCode,
                    EstimatedCost = b.EstimatedCost,
                    CurrencyCode = b.CurrencyCode,
                    Paid = b.Payments
                        .Where(p => p.Status == PaymentStatuses.Succeeded
                            && (p.PaymentType == PaymentTypes.Deposit || p.PaymentType == PaymentTypes.Balance))
                        .Sum(p => (decimal?)p.Amount) ?? 0
                })
                .ToListAsync();

            var modes = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == trip.TripId)
                .Select(th => th.BookingHorse!.Booking!.TransportMode)
                .Distinct()
                .ToListAsync();
            bool isGround = modes.Contains(TransportModes.Ground);
            int driverCount = trip.Crew.Count(c => c.CrewRole == CrewRoles.Driver);

            return DepartureCheckRules.Evaluate(new DepartureCheckInput
            {
                DepartureAt = departureAt,
                PlannedArrivalAt = departureAt + (trip.PlannedEndDate - trip.PlannedStartDate),
                TripStatus = trip.OverallStatus,
                FitExamValidHours = _bookingOptions.FitExamValidHours,
                FitExamTypeConfigured = fitExamTypeId != null,
                Horses = horses.Select(h => new DepartureHorseInput
                {
                    HorseName = h.HorseName,
                    BookingCode = h.BookingCode,
                    DossierStatus = h.DossierStatus,
                    Documents = h.Documents
                        .Where(d => d.Status == DocumentStatuses.Approved && d.DocTypeId != fitExamTypeId)
                        .Select(d => new DepartureDocumentInput { DocTypeName = d.Name, ExpiryDate = d.ExpiryDate })
                        .ToList(),
                    // Specialist có thể nộp hộ giấy khám vào hồ sơ của đơn: tính theo ngày tải lên
                    FitExamIssueDates = fitExams.Where(f => f.HorseId == h.HorseId).Select(f => f.IssuedOn)
                        .Concat(h.Documents.Where(d => d.DocTypeId == fitExamTypeId)
                            .Select(d => DateOnly.FromDateTime(d.UploadedAt.UtcDateTime)))
                        .ToList()
                }).ToList(),
                Bookings = bookings,
                IsGround = isGround,
                DrivingHours = isGround ? await EstimateDrivingHoursAsync(trip, driverCount) : null,
                DriverCount = driverCount,
                SingleDriverLimitHours = _transitOptions.MaxDrivingHoursPerDaySingleDriver
            });
        }

        // Giờ lái ước tính như lúc duyệt chuyến (BE-07), nhưng không gọi bản đồ: km từng chặng đã lưu,
        // thiếu thì lấy tuyến đã duyệt dài nhất của các đơn. Không có số liệu thì bỏ qua quy tắc.
        private async Task<decimal?> EstimateDrivingHoursAsync(Trip trip, int driverCount)
        {
            var route = ActiveRoute(trip);
            decimal distance;
            if (route.Count > 1 && route.Skip(1).All(c => c.DistanceKmFromPrevious.HasValue))
            {
                distance = route.Skip(1).Sum(c => c.DistanceKmFromPrevious!.Value);
            }
            else
            {
                var bookingIds = await _tripHorseRepo.GetQueryable()
                    .Where(th => th.TripId == trip.TripId)
                    .Select(th => th.BookingHorse!.BookingId)
                    .Distinct()
                    .ToListAsync();
                distance = await _routePlanRepo.GetQueryable()
                    .Where(p => bookingIds.Contains(p.BookingId) && p.Status == RoutePlanStatuses.Approved)
                    .MaxAsync(p => (decimal?)p.TotalDistanceKm) ?? 0;
            }

            if (distance <= 0)
            {
                return null;
            }

            var estimate = _transitEstimator.Estimate(new TransitEstimateInputModel
            {
                TransportMode = TransportModes.Ground,
                DistanceKm = (int)Math.Ceiling(distance),
                DriverCount = driverCount,
                Stops = route.Count(c => c.CheckpointType == CheckpointTypes.PickUp || c.CheckpointType == CheckpointTypes.DropOff) + 2
            });
            return estimate.DrivingHours;
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

        // TripHorseId -> BookingId của các con ngựa trên chuyến
        private async Task<Dictionary<int, int>> GetHorseBookingIdsAsync(int tripId)
        {
            return await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId)
                .Select(th => new { th.TripHorseId, th.BookingHorse!.BookingId })
                .ToDictionaryAsync(x => x.TripHorseId, x => x.BookingId);
        }

        // Đơn còn ngựa chưa giao trên chuyến (đơn đã giao xong hoặc ngựa đã rớt lại thì không báo tin hành trình nữa)
        private async Task<HashSet<int>> GetOnBoardBookingIdsAsync(int tripId)
        {
            var ids = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId && TripHorseStatuses.OnBoard.Contains(th.Status))
                .Select(th => th.BookingHorse!.BookingId)
                .Distinct()
                .ToListAsync();
            return ids.ToHashSet();
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
