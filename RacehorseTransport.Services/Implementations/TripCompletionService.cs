using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // Flow 6 (có ghép nhiều đơn, R-04 / R-05):
    //   Mỗi đơn ký một biên bản e-POD ở mốc trả của mình (DropOff của đơn, không có thì Destination).
    //   Ký xong: ngựa của đơn -> Delivered; mọi ngựa của đơn đã xong -> đơn Completed ngay (không chờ đóng chuyến).
    //   R-12: con ngựa rời chuyến với hướng xử lý HandOverHere ký e-POD riêng tại nơi dỡ (TripHorseId), nên một đơn
    //   có thể có nhiều biên bản trên cùng chuyến. Ngựa chết / đưa về nơi đi cũng được tính là đã xong với đơn.
    //   Tới Destination + đủ biên bản -> Manager đóng chuyến, chốt KPI, chia chi phí phát sinh cho các đơn.
    //   R-09: mọi sự cố đã duyệt phải đóng (có nguyên nhân) trước; chuyến trễ do lỗi công ty thì tính bồi thường giao trễ
    //   cho từng đơn, chờ Manager duyệt (DelayCompensationService).
    public class TripCompletionService : ITripCompletionService
    {
        // Chuyến đang chạy: ký e-POD ở mốc DropOff giữa đường được
        private static readonly string[] HandoverTripStatuses =
        {
            TripStatuses.InTransit, TripStatuses.EmergencyRerouting, TripStatuses.ArrivedDestination
        };

        // Sự cố ở các trạng thái này được tính là bất khả kháng và chi phí phát sinh được cộng vào chuyến
        private static readonly string[] AcceptedIncidentStatuses = { IncidentStatuses.Approved, IncidentStatuses.Resolved };

        // Đơn còn đang chạy (Assigned là trạng thái cũ, không còn được tạo mới)
        private static readonly string[] OpenBookingStatuses = { BookingStatuses.Approved, BookingStatuses.Assigned };

        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<HandoverAcceptance> _handoverRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<TransportAsset> _assetRepo;
        private readonly IRepository<DelayCompensation> _compensationRepo;
        private readonly IPricingService _pricingService;
        private readonly ITripService _tripService;
        private readonly IBookingService _bookingService;
        private readonly ITrackingService _trackingService;
        private readonly IFileStorageService _fileStorage;
        private readonly INotificationService _notificationService;
        private readonly IRecipientService _recipientService;
        private readonly IDeliveryService _deliveryService;
        private readonly ISystemSettingService _settingService;
        private readonly IUnitOfWork _unitOfWork;

        public TripCompletionService(
            IRepository<Trip> tripRepo,
            IRepository<HandoverAcceptance> handoverRepo,
            IRepository<BookingRequest> bookingRepo,
            IRepository<TransportAsset> assetRepo,
            IRepository<DelayCompensation> compensationRepo,
            IPricingService pricingService,
            ITripService tripService,
            IBookingService bookingService,
            ITrackingService trackingService,
            IFileStorageService fileStorage,
            INotificationService notificationService,
            IRecipientService recipientService,
            IDeliveryService deliveryService,
            ISystemSettingService settingService,
            IUnitOfWork unitOfWork)
        {
            _tripRepo = tripRepo;
            _handoverRepo = handoverRepo;
            _bookingRepo = bookingRepo;
            _assetRepo = assetRepo;
            _compensationRepo = compensationRepo;
            _pricingService = pricingService;
            _tripService = tripService;
            _bookingService = bookingService;
            _trackingService = trackingService;
            _fileStorage = fileStorage;
            _notificationService = notificationService;
            _recipientService = recipientService;
            _deliveryService = deliveryService;
            _settingService = settingService;
            _unitOfWork = unitOfWork;
        }

        // =============================================================================================
        // Bàn giao e-POD
        // =============================================================================================

        public async Task<HandoverModel> CreateHandoverAsync(int tripId, CreateHandoverModel model, CurrentUser currentUser)
        {
            var trip = await GetTripWithCargoAsync(tripId);

            if (currentUser.Role != UserRoles.Admin && !trip.Crew.Any(c => c.UserId == currentUser.UserId))
            {
                throw new ForbiddenException("Only the crew assigned to this trip can record a handover.");
            }

            if (!HandoverTripStatuses.Contains(trip.OverallStatus))
            {
                throw new BusinessException($"Horses can only be handed over while the trip is under way or has arrived (current status: {trip.OverallStatus}).");
            }

            if (!HandoverConditions.All.Contains(model.OverallCondition))
            {
                throw new BusinessException($"OverallCondition '{model.OverallCondition}' is invalid. Allowed values: {string.Join(", ", HandoverConditions.All)}.");
            }

            // Ngựa của đơn này trên chuyến
            var bookingHorsesOnTrip = trip.TripHorses.Where(th => th.BookingHorse!.BookingId == model.BookingId).ToList();
            if (bookingHorsesOnTrip.Count == 0)
            {
                throw new BusinessException($"Booking with ID {model.BookingId} has no horses on trip {trip.TripCode}.");
            }
            var booking = bookingHorsesOnTrip[0].BookingHorse!.Booking!;
            var route = trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();

            RouteCheckpoint? signedAt;
            List<TripHorse> horses;
            if (model.TripHorseId.HasValue)
            {
                // R-12: giao tại chỗ con ngựa đã rời chuyến (HandOverHere), ghi nhận ở mốc gần nhất đoàn xe đã tới lúc dỡ ngựa
                var offloaded = bookingHorsesOnTrip.FirstOrDefault(th => th.TripHorseId == model.TripHorseId.Value)
                    ?? throw new BusinessException($"Trip horse with ID {model.TripHorseId} does not belong to booking {booking.BookingCode} on trip {trip.TripCode}.");
                if (offloaded.Status != TripHorseStatuses.Offloaded || offloaded.OffloadOutcome != OffloadOutcomes.HandOverHere)
                {
                    throw new BusinessException($"{offloaded.BookingHorse!.Horse!.Name} can only be handed over on site after an OffloadHorse plan with {OffloadOutcomes.HandOverHere} is approved (status: {offloaded.Status}).");
                }
                signedAt = route.LastOrDefault(c => c.ActualArrivalTime != null && c.ActualArrivalTime <= offloaded.OffloadedAt);
                horses = new List<TripHorse> { offloaded };
            }
            else
            {
                // R-04: chỉ ký được khi xe đang đứng ở mốc trả của đơn (DropOff riêng, hoặc Destination)
                var dropoff = TripRouteRules.DropoffCheckpoint(route, model.BookingId);
                if (dropoff == null || dropoff.Status != CheckpointStatuses.Arrived)
                {
                    throw new BusinessException(dropoff == null
                        ? $"Trip {trip.TripCode} has no drop-off point for booking {booking.BookingCode}."
                        : $"Booking {booking.BookingCode} is handed over at '{dropoff.CheckpointName}'; record the arrival there first (status: {dropoff.Status}).");
                }
                if (trip.Handovers.Any(h => h.BookingId == model.BookingId && h.CheckpointId == dropoff.CheckpointId))
                {
                    throw new BusinessException($"Booking {booking.BookingCode} has already been handed over at '{dropoff.CheckpointName}' on trip {trip.TripCode}.");
                }

                // Ngựa còn trên xe phải giao (con đã rời chuyến giữa đường thì không giao ở đây)
                horses = bookingHorsesOnTrip.Where(th => th.Status == TripHorseStatuses.InTransit).ToList();
                if (horses.Count == 0)
                {
                    throw new BusinessException($"Booking {booking.BookingCode} has no horses on board to hand over.");
                }
                signedAt = dropoff;
            }

            // Đối chiếu mã vi mạch quét được với các con ngựa phải giao
            var expected = horses.ToDictionary(th => th.BookingHorse!.Horse!.MicrochipNumber.ToUpperInvariant(), th => th.BookingHorse!.Horse!.Name);
            var scanned = model.MicrochipNumbers
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim().ToUpperInvariant())
                .ToHashSet();

            var notScanned = expected.Where(e => !scanned.Contains(e.Key)).Select(e => $"{e.Value} ({e.Key})").ToList();
            if (notScanned.Count > 0)
            {
                throw new BusinessException($"Microchip check failed. Not scanned: {string.Join(", ", notScanned)}.");
            }

            var unknown = scanned.Where(m => !expected.ContainsKey(m)).ToList();
            if (unknown.Count > 0)
            {
                throw new BusinessException($"Microchip check failed. These microchips do not belong to booking {booking.BookingCode} on this trip: {string.Join(", ", unknown)}.");
            }

            // R-16: người ký phải là người nhận trong đơn, hoặc người nhận thay khách đã xác nhận trên app
            await _recipientService.EnsureHandoverSignerAsync(model.BookingId, model.RecipientName);

            // Mọi kiểm tra nghiệp vụ đã qua mới đẩy ảnh chữ ký lên storage
            var signature = await _fileStorage.UploadAsync(
                model.Signature.Content, model.Signature.FileName, model.Signature.SizeBytes, FileCategories.Signatures);

            // R-16: phí chờ tại điểm giao của đơn (giao tại chỗ con ngựa rời chuyến thì không tính)
            int waitingHours = 0;
            decimal waitingFee = 0;
            if (!model.TripHorseId.HasValue && signedAt?.ActualArrivalTime != null)
            {
                var policy = await _settingService.GetPolicySettingsAsync();
                waitingHours = DeliveryRules.WaitingHours(signedAt.ActualArrivalTime.Value, DateTimeOffset.UtcNow,
                    booking.RecipientNotifiedWindowStart, policy.DeliveryFreeWaitingHours, policy.DeliveryMaxWaitingHours);
                waitingFee = DeliveryRules.WaitingFee(waitingHours, policy.DeliveryWaitingFeePerHourUsd);
            }

            var handover = new HandoverAcceptance
            {
                WaitingHours = waitingHours,
                WaitingFee = waitingFee,
                TripId = tripId,
                BookingId = model.BookingId,
                RecipientName = model.RecipientName,
                RecipientPhone = model.RecipientPhone,
                RecipientIdCard = model.RecipientIdCard,
                DigitalSignatureUrl = signature.Path,
                OverallCondition = model.OverallCondition,
                Remarks = model.Remarks,
                DriverUserId = currentUser.UserId,
                CheckpointId = signedAt?.CheckpointId
            };

            // Đây có phải biên bản cuối cùng của chuyến không: ngoài các con giao lần này, không con nào còn việc trên chuyến
            var handedNow = horses.Select(th => th.TripHorseId).ToHashSet();
            bool allHandedOver = trip.OverallStatus == TripStatuses.ArrivedDestination
                && trip.TripHorses.Where(th => !handedNow.Contains(th.TripHorseId)).All(th => !OffloadRules.IsPendingOnTrip(th));

            var now = DateTimeOffset.UtcNow;
            BookingRequest? completedBooking = null;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _handoverRepo.AddAsync(handover);

                foreach (var tripHorse in horses)
                {
                    tripHorse.Status = TripHorseStatuses.Delivered;
                    tripHorse.DeliveredAt = now;
                    tripHorse.HandoverId = handover.HandoverId;
                }
                await _tripRepo.SaveChangesAsync();

                completedBooking = await CompleteIfDeliveredAsync(booking.BookingId);

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Booking {booking.BookingCode} has been delivered",
                    $"{horses.Count} horse(s) were handed over to {handover.RecipientName} (condition: {handover.OverallCondition}).",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);

                if (completedBooking != null)
                {
                    await _notificationService.SendAsync(
                        completedBooking.CustomerUserId,
                        $"Booking {completedBooking.BookingCode} is completed",
                        "All horses of this booking have been delivered. Thank you for travelling with us.",
                        NotificationReferenceTypes.Booking,
                        completedBooking.BookingId);
                }

                if (allHandedOver)
                {
                    await _notificationService.SendToRoleAsync(
                        UserRoles.LogisticsManager,
                        $"Trip {trip.TripCode} is ready to be closed",
                        "Every booking on this trip has been handed over and signed for.",
                        NotificationReferenceTypes.Trip,
                        trip.TripId);
                }
            });

            await _trackingService.InvalidateTripAsync(tripId);

            // R-16: báo khách đã ký nhận (email HandoverCompleted)
            await _recipientService.OnHandoverSignedAsync(booking.BookingId, handover.RecipientName, now, handover.OverallCondition);

            var saved = await QueryHandovers().FirstAsync(h => h.HandoverId == handover.HandoverId);
            return (await ToModelsAsync(new[] { saved }))[0];
        }

        public async Task<IReadOnlyList<HandoverModel>> GetTripHandoversAsync(int tripId, CurrentUser currentUser)
        {
            // Dùng lại quy tắc "ai được xem chuyến" của TripService (404 / 403)
            await _tripService.GetTripByIdAsync(tripId, currentUser);

            var handovers = await QueryHandovers().Where(h => h.TripId == tripId).OrderBy(h => h.HandoverDateTime).ToListAsync();
            return await ToModelsAsync(handovers);
        }

        public async Task<IReadOnlyList<HandoverModel>> GetBookingHandoversAsync(int bookingId, CurrentUser currentUser)
        {
            // Dùng lại quy tắc "ai được xem đơn" của BookingService (404 / 403)
            await _bookingService.GetBookingByIdAsync(bookingId, currentUser);

            var handovers = await QueryHandovers().Where(h => h.BookingId == bookingId).OrderBy(h => h.HandoverDateTime).ToListAsync();
            return await ToModelsAsync(handovers);
        }

        // =============================================================================================
        // Đóng chuyến và chốt KPI
        // =============================================================================================

        public async Task<TripDetailModel> CloseTripAsync(int tripId, string? executiveRemarks, CurrentUser currentUser)
        {
            var trip = await GetTripWithCargoAsync(tripId);

            if (trip.OverallStatus != TripStatuses.ArrivedDestination)
            {
                throw new BusinessException($"Only a trip that has arrived at the destination can be closed (current status: {trip.OverallStatus}).");
            }

            // Mọi con ngựa phải xong việc trên chuyến: đã ký e-POD, hoặc đã rời chuyến theo phương án được duyệt (R-12)
            var pending = trip.TripHorses
                .Where(OffloadRules.IsPendingOnTrip)
                .Select(th => $"{th.BookingHorse!.Horse!.Name} ({th.BookingHorse.Booking!.BookingCode}): {PendingReason(th)}")
                .ToList();

            if (pending.Count > 0)
            {
                throw new BusinessException($"The trip cannot be closed: {string.Join("; ", pending)}.");
            }

            // R-09: nguyên nhân sự cố quyết định KPI và bồi thường trễ, nên sự cố đã duyệt phải được đóng trước
            var unresolved = trip.Incidents.Where(i => i.Status == IncidentStatuses.Approved || i.Status == IncidentStatuses.AwaitingConsent).Select(i => i.IncidentCode).ToList();
            if (unresolved.Count > 0)
            {
                throw new BusinessException($"Resolve incident(s) {string.Join(", ", unresolved)} and choose their cause before closing the trip.");
            }

            var acceptedIncidents = trip.Incidents.Where(i => AcceptedIncidentStatuses.Contains(i.Status)).ToList();
            bool companyFault = DelayCompensationRules.IsCompanyFault(acceptedIncidents.Select(i => i.FaultParty).ToList());
            var now = DateTimeOffset.UtcNow;
            // Trễ chỉ được miễn (DelayedForceMajeure) khi mọi sự cố đều do bất khả kháng / khách
            var kpi = TripKpiCalculator.Calculate(trip.PlannedEndDate, trip.ActualEndDate ?? now, hasForceMajeure: !companyFault);
            var compensations = companyFault ? await BuildDelayCompensationsAsync(trip) : new List<DelayCompensation>();

            var bookingIds = trip.TripHorses.Select(th => th.BookingHorse!.BookingId).Distinct().ToList();
            var customerIds = trip.TripHorses.Select(th => th.BookingHorse!.Booking!.CustomerUserId).Distinct().ToList();
            var extras = AllocateExtraCharges(trip, acceptedIncidents);
            // R-16: phí chờ, chi phí khi không giao được (khách chịu)
            foreach (var (bookingId, amount) in await _deliveryService.GetChargesForTripAsync(trip.TripId, now))
            {
                extras[bookingId] = (extras.TryGetValue(bookingId, out var current) ? current : 0) + amount;
            }
            var paymentSettings = await _settingService.GetPaymentSettingsAsync();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                trip.ActualEndDate ??= now;
                trip.DelayMinutes = kpi.DelayMinutes;
                trip.OnTimeStatus = kpi.OnTimeStatus;
                trip.KpiScore = kpi.KpiScore;
                trip.ActualCost = trip.PlannedCost + acceptedIncidents.Sum(i => i.AdditionalCost);
                trip.OverallStatus = TripStatuses.Completed;
                trip.ClosedByUserId = currentUser.UserId;
                trip.ClosedAt = now;
                trip.ExecutiveRemarks = string.IsNullOrWhiteSpace(executiveRemarks) ? null : executiveRemarks.Trim();

                var vehicle = await _assetRepo.GetByIdAsync(trip.VehicleId);
                if (vehicle != null)
                {
                    vehicle.Status = AssetStatuses.Available;
                }

                await _tripRepo.UpdateAsync(trip);

                var (completedBookings, awaitingPayment) = await CompleteFinishedBookingsAsync(
                    bookingIds, extras, paymentSettings.ExtraPaymentDueDays);

                if (compensations.Count > 0)
                {
                    await _compensationRepo.AddRangeAsync(compensations);
                    foreach (var compensation in compensations)
                    {
                        var booking = trip.TripHorses.Select(th => th.BookingHorse!.Booking!).First(b => b.BookingId == compensation.BookingId);
                        await _notificationService.SendToRoleAsync(
                            UserRoles.LogisticsManager,
                            $"Delay compensation to approve: {booking.BookingCode}",
                            $"Trip {trip.TripCode} delivered {compensation.DelayHours:0.##} hour(s) late. Suggested compensation: {compensation.CalculatedAmount:N2} {compensation.CurrencyCode} ({compensation.Percent:0.##}% of freight).",
                            NotificationReferenceTypes.Booking,
                            booking.BookingId);
                    }
                }

                var recipients = new List<int>(trip.Crew.Select(c => c.UserId));
                if (trip.PlannedByUserId.HasValue)
                {
                    recipients.Add(trip.PlannedByUserId.Value);
                }
                await _notificationService.SendToUsersAsync(
                    recipients.Distinct(),
                    $"Trip {trip.TripCode} has been closed",
                    $"Result: {kpi.OnTimeStatus}, delay {kpi.DelayMinutes} minute(s), KPI {kpi.KpiScore:0.##}.",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);

                foreach (var booking in completedBookings)
                {
                    await _notificationService.SendAsync(
                        booking.CustomerUserId,
                        $"Booking {booking.BookingCode} is completed",
                        "All horses of this booking have been delivered and the transport has been closed. Thank you for travelling with us.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId);
                }

                foreach (var booking in awaitingPayment)
                {
                    await _notificationService.SendAsync(
                        booking.CustomerUserId,
                        $"Extra charges for booking {booking.BookingCode}",
                        $"All horses have been delivered. Approved extra costs during the trip (incidents) add up to {booking.ExtraCharges:N2} {booking.CurrencyCode}; please pay them by {booking.ExtraDueAt:yyyy-MM-dd} from the booking page to complete the booking.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId,
                        emailCustomers: true);
                }
            });

            await _trackingService.InvalidateTripAsync(tripId);
            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        // Đóng chuyến: cộng chi phí phát sinh vào từng đơn (R-02) rồi xét lại trạng thái đơn.
        //   - Đơn còn mở mà mọi ngựa đã giao -> Completed, hoặc AwaitingFinalPayment nếu còn nợ phát sinh.
        //   - Đơn đã Completed lúc ký e-POD giữa đường mà giờ mới có phát sinh -> AwaitingFinalPayment.
        // Đơn tạo trước khi có thanh toán (DepositPercent = null) không bị tính phát sinh.
        private async Task<(List<BookingRequest> Completed, List<BookingRequest> AwaitingPayment)> CompleteFinishedBookingsAsync(
            List<int> bookingIds, Dictionary<int, decimal> extras, int extraDueDays)
        {
            var bookings = await LoadBookingsForCompletion()
                .Where(b => bookingIds.Contains(b.BookingId)
                    && (OpenBookingStatuses.Contains(b.Status) || b.Status == BookingStatuses.Completed))
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;
            var completed = new List<BookingRequest>();
            var awaitingPayment = new List<BookingRequest>();
            foreach (var booking in bookings)
            {
                bool wasCompleted = booking.Status == BookingStatuses.Completed;
                bool gotExtra = false;
                if (booking.DepositPercent.HasValue && extras.TryGetValue(booking.BookingId, out decimal extra) && extra > 0)
                {
                    booking.ExtraCharges += extra;
                    booking.ExtraDueAt = now.AddDays(extraDueDays);
                    booking.PaymentReminderCount = 0;
                    gotExtra = true;
                }

                if (wasCompleted)
                {
                    if (gotExtra && booking.ExtraCharges > ExtraPaid(booking))
                    {
                        booking.Status = BookingStatuses.AwaitingFinalPayment;
                        awaitingPayment.Add(booking);
                    }
                    continue;
                }

                if (AllHorsesFinished(booking))
                {
                    booking.Status = booking.ExtraCharges > ExtraPaid(booking)
                        ? BookingStatuses.AwaitingFinalPayment
                        : BookingStatuses.Completed;
                    (booking.Status == BookingStatuses.Completed ? completed : awaitingPayment).Add(booking);
                }
            }

            await _bookingRepo.SaveChangesAsync();
            return (completed, awaitingPayment);
        }

        // Ký e-POD xong: đơn đã giao hết ngựa thì hoàn tất ngay (chi phí phát sinh, nếu có, chốt khi đóng chuyến)
        private async Task<BookingRequest?> CompleteIfDeliveredAsync(int bookingId)
        {
            var booking = await LoadBookingsForCompletion().FirstAsync(b => b.BookingId == bookingId);
            if (!OpenBookingStatuses.Contains(booking.Status) || !AllHorsesFinished(booking))
            {
                return null;
            }

            booking.Status = booking.ExtraCharges > ExtraPaid(booking)
                ? BookingStatuses.AwaitingFinalPayment
                : BookingStatuses.Completed;
            await _bookingRepo.SaveChangesAsync();
            return booking.Status == BookingStatuses.Completed ? booking : null;
        }

        private IQueryable<BookingRequest> LoadBookingsForCompletion()
        {
            return _bookingRepo.GetQueryable()
                .AsSplitQuery()
                .Include(b => b.Payments)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.TripHorses).ThenInclude(th => th.Trip);
        }

        // R-05 / R-12: mọi con ngựa chưa hủy của đơn đã xong trên một chuyến còn hiệu lực
        // (đã giao, kể cả giao tại chỗ; chết; hoặc được đưa về nơi đi)
        private static bool AllHorsesFinished(BookingRequest booking)
        {
            var activeHorses = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).ToList();
            return activeHorses.Count > 0
                && activeHorses.All(bh => bh.Status == BookingHorseStatuses.Deceased
                    || bh.TripHorses.Any(th => th.Trip!.OverallStatus != TripStatuses.Cancelled && OffloadRules.IsFinished(th)));
        }

        private static decimal ExtraPaid(BookingRequest booking)
        {
            return booking.Payments
                .Where(p => p.PaymentType == PaymentTypes.Extra && p.Status == PaymentStatuses.Succeeded)
                .Sum(p => p.Amount);
        }

        // R-09: bồi thường giao trễ cho từng đơn đã ký e-POD trên chuyến (chuyến đã xác định là trễ do lỗi công ty).
        // Đơn tạo trước khi có thanh toán (DepositPercent = null) và đơn đã có bồi thường cho chuyến này thì bỏ qua.
        private async Task<List<DelayCompensation>> BuildDelayCompensationsAsync(Trip trip)
        {
            var settings = await _settingService.GetPolicySettingsAsync();
            var result = new List<DelayCompensation>();

            var bookings = trip.TripHorses.Select(th => th.BookingHorse!.Booking!).DistinctBy(b => b.BookingId).ToList();
            foreach (var booking in bookings.Where(b => b.DepositPercent.HasValue))
            {
                var deliveredAt = trip.Handovers.Where(h => h.BookingId == booking.BookingId).Select(h => (DateTimeOffset?)h.HandoverDateTime).Max();
                if (deliveredAt == null)
                {
                    continue;
                }

                double lateMinutes = (deliveredAt.Value - booking.DeliveryDeadline).TotalMinutes;
                decimal percent = DelayCompensationRules.Percent(lateMinutes, settings);
                if (percent <= 0)
                {
                    continue;
                }

                decimal freight = DelayCompensationRules.FreightAmount(_pricingService.ParseLines(booking.QuoteBreakdown), booking.EstimatedCost);
                decimal amount = DelayCompensationRules.Amount(freight, percent);
                if (amount <= 0 || await _compensationRepo.ExistsAsync(c => c.BookingId == booking.BookingId && c.TripId == trip.TripId))
                {
                    continue;
                }

                result.Add(new DelayCompensation
                {
                    BookingId = booking.BookingId,
                    TripId = trip.TripId,
                    DelayHours = DelayCompensationRules.DelayHours(deliveredAt.Value, booking.DeliveryDeadline),
                    FreightAmount = freight,
                    Percent = percent,
                    CalculatedAmount = amount,
                    CurrencyCode = booking.CurrencyCode,
                    Status = DelayCompensationStatuses.AwaitingApproval
                });
            }
            return result;
        }

        private static string PendingReason(TripHorse tripHorse)
        {
            if (tripHorse.Status != TripHorseStatuses.Offloaded)
            {
                return "handover is not signed";
            }
            return tripHorse.OffloadOutcome == null
                ? "offloaded in an emergency, waiting for an approved OffloadHorse plan"
                : "on-site handover is not signed";
        }

        // Chia chi phí phát sinh của các sự cố được chấp nhận cho từng đơn trên chuyến:
        // sự cố gắn với một con ngựa -> tính cho đơn của con ngựa đó; còn lại chia theo số ngựa của mỗi đơn
        // đang ở trên xe lúc xảy ra sự cố (R-04: đơn đã giao giữa đường trước đó, hoặc chưa được đón, không chịu).
        public static Dictionary<int, decimal> AllocateExtraCharges(Trip trip, List<TripIncident> acceptedIncidents)
        {
            var result = new Dictionary<int, decimal>();

            void Add(int bookingId, decimal amount) =>
                result[bookingId] = (result.TryGetValue(bookingId, out var current) ? current : 0) + amount;

            // Chính sách 9.1: sự cố do lỗi công ty thì công ty chịu chi phí, không tính vào phát sinh của khách
            foreach (var incident in acceptedIncidents.Where(i => i.AdditionalCost > 0 && i.FaultParty != FaultParties.Company))
            {
                var affected = incident.AffectedTripHorseId.HasValue
                    ? trip.TripHorses.FirstOrDefault(th => th.TripHorseId == incident.AffectedTripHorseId.Value)
                    : null;

                if (affected != null)
                {
                    Add(affected.BookingHorse!.BookingId, incident.AdditionalCost);
                    continue;
                }

                var onBoard = trip.TripHorses.Where(th => WasOnBoard(th, incident.ReportedAt)).ToList();
                if (onBoard.Count == 0)
                {
                    onBoard = trip.TripHorses.Where(th => th.Status != TripHorseStatuses.Cancelled).ToList();
                }
                var horsesPerBooking = onBoard
                    .GroupBy(th => th.BookingHorse!.BookingId)
                    .Select(g => new { BookingId = g.Key, Count = g.Count() })
                    .OrderBy(x => x.BookingId)
                    .ToList();
                int totalHorses = horsesPerBooking.Sum(x => x.Count);
                if (totalHorses == 0)
                {
                    continue;
                }

                decimal allocated = 0;
                for (int i = 0; i < horsesPerBooking.Count; i++)
                {
                    var share = i == horsesPerBooking.Count - 1
                        ? incident.AdditionalCost - allocated // phần lẻ dồn cho đơn cuối để tổng đúng bằng chi phí
                        : Math.Round(incident.AdditionalCost * horsesPerBooking[i].Count / totalHorses, 2);
                    allocated += share;
                    Add(horsesPerBooking[i].BookingId, share);
                }
            }

            return result;
        }

        // Con ngựa có ở trên xe lúc at không: đã được đón (chưa có giờ đón thì coi như lên xe từ đầu), chưa giao, chưa rớt lại
        private static bool WasOnBoard(TripHorse tripHorse, DateTimeOffset at)
        {
            if (tripHorse.Status == TripHorseStatuses.Cancelled)
            {
                return false;
            }
            bool pickedUp = tripHorse.PickedUpAt == null || tripHorse.PickedUpAt <= at;
            bool stillOn = (tripHorse.DeliveredAt == null || tripHorse.DeliveredAt > at)
                && (tripHorse.OffloadedAt == null || tripHorse.OffloadedAt > at);
            return pickedUp && stillOn;
        }

        // ---------------------------------------------------------------------------------------------

        private async Task<Trip> GetTripWithCargoAsync(int tripId)
        {
            var trip = await _tripRepo.GetQueryable()
                .AsSplitQuery()
                .Include(t => t.Crew)
                .Include(t => t.Handovers)
                .Include(t => t.Incidents)
                .Include(t => t.Checkpoints)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Booking)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null)
            {
                throw new NotFoundException($"Trip with ID {tripId} was not found.");
            }
            return trip;
        }

        private IQueryable<HandoverAcceptance> QueryHandovers()
        {
            return _handoverRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(h => h.Driver)
                .Include(h => h.Booking!).ThenInclude(b => b.Customer)
                .Include(h => h.Trip!).ThenInclude(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Horse);
        }

        // Các con ngựa của một biên bản. Biên bản cũ (trước R-05) chưa gắn với con nào thì lấy các con chưa gắn biên bản của đơn.
        private static IEnumerable<TripHorse> HorsesOf(HandoverAcceptance handover)
        {
            var bookingHorses = (handover.Trip?.TripHorses ?? Enumerable.Empty<TripHorse>())
                .Where(th => th.BookingHorse?.BookingId == handover.BookingId)
                .ToList();
            var linked = bookingHorses.Where(th => th.HandoverId == handover.HandoverId).ToList();
            return linked.Count > 0 ? linked : bookingHorses.Where(th => th.HandoverId == null);
        }

        private async Task<List<HandoverModel>> ToModelsAsync(IEnumerable<HandoverAcceptance> handovers)
        {
            var models = handovers.Select(h => new HandoverModel
            {
                HandoverId = h.HandoverId,
                TripId = h.TripId,
                TripCode = h.Trip?.TripCode ?? string.Empty,
                BookingId = h.BookingId,
                BookingCode = h.Booking?.BookingCode ?? string.Empty,
                CustomerName = h.Booking?.Customer?.FullName,
                DropoffAddress = h.Booking?.DropoffAddress ?? string.Empty,
                HandoverDateTime = h.HandoverDateTime,
                RecipientName = h.RecipientName,
                RecipientPhone = h.RecipientPhone,
                RecipientIdCard = h.RecipientIdCard,
                SignatureUrl = h.DigitalSignatureUrl,
                OverallCondition = h.OverallCondition,
                Remarks = h.Remarks,
                DriverUserId = h.DriverUserId,
                DriverName = h.Driver?.FullName,
                CheckpointId = h.CheckpointId,
                Horses = HorsesOf(h)
                    .OrderBy(th => th.StallSlotNumber)
                    .Select(th => new HandoverHorseModel
                    {
                        TripHorseId = th.TripHorseId,
                        HorseId = th.BookingHorse!.HorseId,
                        HorseName = th.BookingHorse.Horse?.Name ?? string.Empty,
                        MicrochipNumber = th.BookingHorse.Horse?.MicrochipNumber ?? string.Empty
                    })
                    .ToList()
            }).ToList();

            await _fileStorage.ResolveViewUrlsAsync(models, m => m.SignatureUrl, (m, url) => m.SignatureUrl = url);
            return models;
        }
    }
}
