using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // R-16 (BE-16, Chính sách 10.4, 10.5): không giao được ngựa.
    //   Đoàn xe ghi "không giao được" ở điểm giao của đơn -> lưu lý do + ảnh, phí chờ, tạo sự cố để điều phối xử lý, xe được rời điểm giao.
    //   Vắng mặt / từ chối không lý do (lỗi khách): email DeliveryFailed, khách chọn trong Delivery.DecisionHours giờ:
    //     người nhận / địa chỉ khác (km thêm × Delivery.ExtraKmFeeUsd), chuồng tạm (ngày × ngựa × Stable.FeePerHorsePerDayUsd, tính khi đóng chuyến),
    //     chở về (báo giá chiều ngược lại). Quá hạn -> chuồng tạm. Mọi khoản cộng vào phát sinh của đơn khi đóng chuyến.
    //   Từ chối vì ngựa bị thương / nhầm ngựa: công ty chịu, không thu phí chờ, mở sự cố (hồ sơ bồi thường: BE-12).
    public class DeliveryService : IDeliveryService
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly string[] DeliveringTripStatuses =
        {
            TripStatuses.InTransit, TripStatuses.EmergencyRerouting, TripStatuses.ArrivedDestination
        };

        private readonly IRepository<DeliveryFailure> _failureRepo;
        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<TripIncident> _incidentRepo;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGeoService _geoService;
        private readonly IPricingService _pricingService;
        private readonly ISystemSettingService _settingService;
        private readonly IFileStorageService _fileStorage;
        private readonly INotificationService _notificationService;

        public DeliveryService(
            IRepository<DeliveryFailure> failureRepo,
            IRepository<Trip> tripRepo,
            IRepository<BookingRequest> bookingRepo,
            IRepository<TripIncident> incidentRepo,
            ICodeGenerator codeGenerator,
            IUnitOfWork unitOfWork,
            IGeoService geoService,
            IPricingService pricingService,
            ISystemSettingService settingService,
            IFileStorageService fileStorage,
            INotificationService notificationService)
        {
            _failureRepo = failureRepo;
            _tripRepo = tripRepo;
            _bookingRepo = bookingRepo;
            _incidentRepo = incidentRepo;
            _codeGenerator = codeGenerator;
            _unitOfWork = unitOfWork;
            _geoService = geoService;
            _pricingService = pricingService;
            _settingService = settingService;
            _fileStorage = fileStorage;
            _notificationService = notificationService;
        }

        // =============================================================================================
        // Đoàn xe ghi không giao được
        // =============================================================================================

        public async Task<DeliveryFailureModel> RecordFailureAsync(int tripId, int bookingId, RecordDeliveryFailureModel model, CurrentUser currentUser)
        {
            var trip = await _tripRepo.GetQueryable()
                .Include(t => t.Crew)
                .Include(t => t.Checkpoints)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Booking)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.TripId == tripId)
                ?? throw new NotFoundException($"Trip with ID {tripId} was not found.");
            if (currentUser.Role != UserRoles.Admin && !trip.Crew.Any(c => c.UserId == currentUser.UserId))
            {
                throw new ForbiddenException("Only the crew of this trip can record a failed delivery.");
            }
            if (!DeliveryFailureReasons.All.Contains(model.Reason))
            {
                throw new BusinessException($"Reason '{model.Reason}' is invalid. Allowed values: {string.Join(", ", DeliveryFailureReasons.All)}.");
            }
            if (!DeliveringTripStatuses.Contains(trip.OverallStatus))
            {
                throw new BusinessException($"A failed delivery can only be recorded while the trip is delivering (current status: {trip.OverallStatus}).");
            }

            var horses = trip.TripHorses.Where(th => th.BookingHorse!.BookingId == bookingId).ToList();
            if (horses.Count == 0)
            {
                throw new BusinessException($"Booking with ID {bookingId} has no horses on trip {trip.TripCode}.");
            }
            var booking = horses[0].BookingHorse!.Booking!;
            var route = trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
            var dropoff = TripRouteRules.DropoffCheckpoint(route, bookingId);
            if (dropoff?.ActualArrivalTime == null || dropoff.Status != CheckpointStatuses.Arrived)
            {
                throw new BusinessException($"Record the arrival at the drop-off point of booking {booking.BookingCode} first.");
            }
            if (!horses.Any(th => th.Status == TripHorseStatuses.InTransit))
            {
                throw new BusinessException($"Booking {booking.BookingCode} has no horses on board to deliver.");
            }
            if (await _failureRepo.ExistsAsync(f => f.TripId == tripId && f.BookingId == bookingId && f.CheckpointId == dropoff.CheckpointId))
            {
                throw new BusinessException($"A failed delivery of booking {booking.BookingCode} has already been recorded at '{dropoff.CheckpointName}'.");
            }
            var photos = (model.PhotoPaths ?? new List<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
            if (photos.Any(p => !_fileStorage.IsValidPath(p)))
            {
                throw new BusinessException("Each photo path must come from POST /api/files.");
            }

            var policy = await _settingService.GetPolicySettingsAsync();
            var now = DateTimeOffset.UtcNow;
            bool customerFault = DeliveryFailureReasons.CustomerFault.Contains(model.Reason);
            int waitingHours = customerFault
                ? DeliveryRules.WaitingHours(dropoff.ActualArrivalTime.Value, now, booking.RecipientNotifiedWindowStart, policy.DeliveryFreeWaitingHours, policy.DeliveryMaxWaitingHours)
                : 0;

            var failure = new DeliveryFailure
            {
                TripId = tripId,
                BookingId = bookingId,
                CheckpointId = dropoff.CheckpointId,
                Reason = model.Reason,
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
                PhotoPaths = JsonSerializer.Serialize(photos),
                RecordedByUserId = currentUser.UserId,
                RecordedAt = now,
                WaitingHours = waitingHours,
                WaitingFee = DeliveryRules.WaitingFee(waitingHours, policy.DeliveryWaitingFeePerHourUsd),
                CustomerPays = customerFault,
                DecisionDueAt = customerFault ? now.AddHours(policy.DeliveryDecisionHours) : null
            };
            // Sự cố để điều phối / quản lý xử lý ngựa còn trên xe (chuồng tạm, giao chỗ khác, chở về) theo quyết định của khách.
            // Tạo trực tiếp (không qua "báo sự cố" của đoàn xe) vì chuyến có thể đã ở ArrivedDestination.
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var incident = new TripIncident
                {
                    IncidentCode = await _codeGenerator.NextAsync(CodePrefixes.Incident),
                    TripId = tripId,
                    ReportedByUserId = currentUser.UserId,
                    IncidentType = customerFault ? IncidentTypes.RecipientUnavailable
                        : model.Reason == DeliveryFailureReasons.RefusedInjured ? IncidentTypes.EquineHealthIssue : IncidentTypes.Other,
                    Severity = customerFault ? IncidentSeverities.Moderate : IncidentSeverities.Major,
                    Location = dropoff.CheckpointName,
                    Description = $"Failed delivery of booking {booking.BookingCode}: {model.Reason}. {failure.Note}".Trim(),
                    AffectedTripHorseId = horses.First(th => th.Status == TripHorseStatuses.InTransit).TripHorseId,
                    ReportedAt = now
                };
                await _incidentRepo.AddAsync(incident);
                await _incidentRepo.SaveChangesAsync();

                failure.IncidentId = incident.IncidentId;
                await _failureRepo.AddAsync(failure);
                await _failureRepo.SaveChangesAsync();

                string staffMessage = $"Trip {trip.TripCode}, booking {booking.BookingCode}: {ReasonText(model.Reason)} at {dropoff.CheckpointName}. Incident {incident.IncidentCode} was opened; propose how to handle the horses"
                    + (customerFault ? " once the customer decides." : " (the company bears the costs).");
                await _notificationService.SendToRoleAsync(UserRoles.FleetCoordinator, $"Failed delivery: {booking.BookingCode}", staffMessage, NotificationReferenceTypes.Incident, incident.IncidentId);
                await _notificationService.SendToRoleAsync(UserRoles.LogisticsManager, $"Failed delivery: {booking.BookingCode}", staffMessage, NotificationReferenceTypes.Incident, incident.IncidentId);
            });

            if (customerFault)
            {
                string deadline = VietnamTime(failure.DecisionDueAt!.Value);
                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"The horses of booking {booking.BookingCode} could not be delivered",
                    $"Reason: {ReasonText(model.Reason)}. Please choose by {deadline} (Vietnam time): another recipient / address nearby, a temporary stable, or return to the origin. Otherwise the horses go to a temporary stable.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
                await _notificationService.EmailCustomerAsync(
                    booking.CustomerUserId,
                    EmailTemplateNames.DeliveryFailed,
                    CustomerPages.Booking(booking.BookingId),
                    new Dictionary<string, string>
                    {
                        ["BookingCode"] = booking.BookingCode,
                        ["Reason"] = ReasonText(model.Reason),
                        ["DecisionDeadline"] = deadline
                    });
            }
            else
            {
                // TODO(BE-12): mở hồ sơ bồi thường cho ngựa bị thương / giao nhầm
                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Delivery of booking {booking.BookingCode} was refused",
                    $"Reason: {ReasonText(model.Reason)}. The horses are cared for at our cost while we handle it; our team will contact you about compensation.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId,
                    emailCustomers: true);
            }

            return ToModel(failure, booking.BookingCode);
        }

        // =============================================================================================
        // Khách chọn cách xử lý
        // =============================================================================================

        public async Task<DeliveryFailureModel> DecideAsync(int bookingId, DeliveryDecisionModel model, CurrentUser currentUser)
        {
            var booking = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId)
                ?? throw new NotFoundException($"Booking with ID {bookingId} was not found.");
            if (booking.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only decide for your own bookings.");
            }
            if (!DeliveryDecisions.All.Contains(model.Decision))
            {
                throw new BusinessException($"Decision '{model.Decision}' is invalid. Allowed values: {string.Join(", ", DeliveryDecisions.All)}.");
            }

            var failure = await _failureRepo.GetQueryable()
                .Where(f => f.BookingId == bookingId && f.DecisionDueAt != null)
                .OrderByDescending(f => f.RecordedAt)
                .FirstOrDefaultAsync()
                ?? throw new BusinessException($"Booking {booking.BookingCode} has no failed delivery waiting for your decision.");
            if (failure.Decision != null)
            {
                throw new BusinessException($"The decision has already been made ({failure.Decision}{(failure.DecidedBySystem ? ", chosen automatically after the deadline" : string.Empty)}).");
            }
            var now = DateTimeOffset.UtcNow;
            if (now > failure.DecisionDueAt!.Value)
            {
                throw new BusinessException("The decision deadline has passed; the horses go to a temporary stable.");
            }

            var policy = await _settingService.GetPolicySettingsAsync();
            string suggestion;
            switch (model.Decision)
            {
                case DeliveryDecisions.AlternateRecipient:
                    if (string.IsNullOrWhiteSpace(model.RecipientName) || string.IsNullOrWhiteSpace(model.RecipientPhone) || string.IsNullOrWhiteSpace(model.Address))
                    {
                        throw new BusinessException("Another recipient needs a name, a phone number and an address.");
                    }
                    failure.AlternateRecipientName = model.RecipientName.Trim();
                    failure.AlternateRecipientPhone = model.RecipientPhone.Trim();
                    failure.AlternateAddress = model.Address.Trim();
                    failure.AlternateLat = model.Lat;
                    failure.AlternateLng = model.Lng;
                    failure.ExtraDistanceKm = await ExtraDistanceKmAsync(failure, booking, model);
                    failure.DecisionCost = DeliveryRules.ExtraDistanceCost(failure.ExtraDistanceKm ?? 0, policy.DeliveryExtraKmFeeUsd);
                    // Người nhận mới được ký e-POD
                    booking.AlternateRecipientName = failure.AlternateRecipientName;
                    booking.AlternateRecipientIdNumber = null;
                    booking.AlternateRecipientApprovedAt = now;
                    suggestion = $"deliver to {failure.AlternateRecipientName} ({failure.AlternateRecipientPhone}) at {failure.AlternateAddress}, {failure.ExtraDistanceKm:0.#} km extra";
                    break;
                case DeliveryDecisions.ReturnToOrigin:
                    failure.DecisionCost = await ReturnQuoteAsync(booking);
                    suggestion = $"return the horses to {booking.PickupAddress}";
                    break;
                default:
                    suggestion = "keep the horses at a temporary stable until the customer collects them";
                    break;
            }
            failure.Decision = model.Decision;
            failure.DecidedAt = now;
            await _failureRepo.SaveChangesAsync();

            await NotifyDecisionAsync(booking, failure, suggestion);
            return ToModel(failure, booking.BookingCode);
        }

        public async Task<List<DeliveryFailureModel>> GetForBookingAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await _bookingRepo.GetQueryable().AsNoTracking().FirstOrDefaultAsync(b => b.BookingId == bookingId)
                ?? throw new NotFoundException($"Booking with ID {bookingId} was not found.");
            if (!BookingService.CanView(booking, currentUser))
            {
                throw new ForbiddenException("You do not have access to this booking.");
            }
            var failures = await _failureRepo.GetQueryable().AsNoTracking()
                .Where(f => f.BookingId == bookingId)
                .OrderByDescending(f => f.RecordedAt)
                .ToListAsync();
            return failures.Select(f => ToModel(f, booking.BookingCode)).ToList();
        }

        // Job: quá hạn chọn -> chuồng tạm
        public async Task<int> ProcessOverdueDecisionsAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var overdue = await _failureRepo.GetQueryable()
                .Include(f => f.Booking)
                .Where(f => f.Decision == null && f.DecisionDueAt != null && f.DecisionDueAt < now)
                .ToListAsync();
            foreach (var failure in overdue)
            {
                failure.Decision = DeliveryDecisions.HoldAtStable;
                failure.DecidedAt = failure.DecisionDueAt;
                failure.DecidedBySystem = true;
                await _failureRepo.SaveChangesAsync();
                await NotifyDecisionAsync(failure.Booking!, failure, "keep the horses at a temporary stable (no answer before the deadline)");
            }
            return overdue.Count;
        }

        // =============================================================================================
        // Tiền tính khi đóng chuyến
        // =============================================================================================

        public async Task<Dictionary<int, decimal>> GetChargesForTripAsync(int tripId, DateTimeOffset closedAt)
        {
            var policy = await _settingService.GetPolicySettingsAsync();
            var trip = await _tripRepo.GetQueryable().AsNoTracking()
                .Include(t => t.Handovers)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse)
                .FirstAsync(t => t.TripId == tripId);
            var failures = await _failureRepo.GetQueryable().AsNoTracking()
                .Where(f => f.TripId == tripId && f.CustomerPays)
                .ToListAsync();

            var result = new Dictionary<int, decimal>();
            void Add(int bookingId, decimal amount)
            {
                if (amount > 0)
                {
                    result[bookingId] = (result.TryGetValue(bookingId, out var current) ? current : 0) + amount;
                }
            }

            foreach (var handover in trip.Handovers)
            {
                Add(handover.BookingId, handover.WaitingFee);
            }
            foreach (var failure in failures)
            {
                Add(failure.BookingId, failure.WaitingFee + failure.DecisionCost);
                if (failure.Decision == DeliveryDecisions.HoldAtStable && failure.DecidedAt.HasValue)
                {
                    // Chuồng tạm: từ lúc chọn tới lúc ngựa được giao / rời chuyến (chưa có thì tới lúc đóng chuyến)
                    var horses = trip.TripHorses.Where(th => th.BookingHorse!.BookingId == failure.BookingId && th.Status != TripHorseStatuses.Cancelled).ToList();
                    var until = horses.Select(th => th.DeliveredAt ?? th.OffloadedAt).Where(t => t.HasValue).Select(t => t!.Value).DefaultIfEmpty(closedAt).Max();
                    Add(failure.BookingId, ReturnRouteRules.StableFee(failure.DecidedAt.Value, until, horses.Count, policy.StableFeePerHorsePerDayUsd));
                }
            }
            return result;
        }

        public async Task<bool> HasFailureAtAsync(int tripId, int bookingId, int checkpointId) =>
            await _failureRepo.ExistsAsync(f => f.TripId == tripId && f.BookingId == bookingId && f.CheckpointId == checkpointId);

        // ---------------------------------------------------------------------------------------------

        private async Task<decimal?> ExtraDistanceKmAsync(DeliveryFailure failure, BookingRequest booking, DeliveryDecisionModel model)
        {
            if (model.Lat == null || model.Lng == null)
            {
                return 0;
            }
            var checkpoint = await _tripRepo.GetQueryable().AsNoTracking()
                .SelectMany(t => t.Checkpoints)
                .FirstOrDefaultAsync(c => c.CheckpointId == failure.CheckpointId);
            decimal? fromLat = checkpoint?.Latitude ?? booking.DropoffLat;
            decimal? fromLng = checkpoint?.Longitude ?? booking.DropoffLng;
            if (fromLat == null || fromLng == null)
            {
                return 0;
            }
            var route = await _geoService.RouteAsync(new[]
            {
                new GeoPointModel { Lat = (double)fromLat.Value, Lng = (double)fromLng.Value },
                new GeoPointModel { Lat = (double)model.Lat.Value, Lng = (double)model.Lng.Value }
            });
            return Math.Round((decimal)route.TotalDistanceKm, 2);
        }

        // Chở về: báo giá như một đơn đi ngược lại (cùng ngựa, hạng chuồng, quãng đường; không bảo hiểm).
        // Bảng giá chưa có chiều ngược (ví dụ chỉ có cước xuất từ VN) thì tính theo cước cùng tuyến chiều đi.
        private async Task<decimal> ReturnQuoteAsync(BookingRequest booking)
        {
            var stalls = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).Select(bh => bh.StallClass).ToList();
            QuoteInputModel Input(string from, string to) => new()
            {
                TransportMode = booking.TransportMode,
                PickupCountryCode = from,
                DropoffCountryCode = to,
                DistanceKm = booking.DistanceKm,
                RequiresClimateControl = booking.RequiresClimateControl,
                StallClasses = stalls,
                QuotedAt = DateTimeOffset.UtcNow,
                SkipScheduleCheck = true
            };
            QuoteModel quote;
            try
            {
                quote = await _pricingService.CalculateQuoteAsync(Input(booking.DropoffCountryCode, booking.PickupCountryCode));
            }
            catch (BusinessException)
            {
                quote = await _pricingService.CalculateQuoteAsync(Input(booking.PickupCountryCode, booking.DropoffCountryCode));
            }
            return quote.Lines.Sum(l => l.Amount);
        }

        private async Task NotifyDecisionAsync(BookingRequest booking, DeliveryFailure failure, string suggestion)
        {
            string costText = failure.Decision == DeliveryDecisions.HoldAtStable
                ? "Stable fees are charged per horse per day until the horses are collected."
                : $"Cost: {failure.DecisionCost:N2} USD, added to the extra charges of the booking.";
            string message = $"Booking {booking.BookingCode}: {suggestion}. {costText}";
            await _notificationService.SendToRoleAsync(UserRoles.FleetCoordinator, $"Failed delivery decision for {booking.BookingCode}", message, NotificationReferenceTypes.Booking, booking.BookingId);
            await _notificationService.SendToRoleAsync(UserRoles.LogisticsManager, $"Failed delivery decision for {booking.BookingCode}", message, NotificationReferenceTypes.Booking, booking.BookingId);
            await _notificationService.SendAsync(booking.CustomerUserId, $"Your choice for booking {booking.BookingCode} is recorded", message, NotificationReferenceTypes.Booking, booking.BookingId, emailCustomers: true);
        }

        private DeliveryFailureModel ToModel(DeliveryFailure f, string bookingCode) => new()
        {
            FailureId = f.FailureId,
            TripId = f.TripId,
            BookingId = f.BookingId,
            BookingCode = bookingCode,
            CheckpointId = f.CheckpointId,
            Reason = f.Reason,
            Note = f.Note,
            PhotoPaths = JsonSerializer.Deserialize<List<string>>(f.PhotoPaths) ?? new List<string>(),
            RecordedAt = f.RecordedAt,
            IncidentId = f.IncidentId,
            WaitingHours = f.WaitingHours,
            WaitingFee = f.WaitingFee,
            CustomerPays = f.CustomerPays,
            DecisionDueAt = f.DecisionDueAt,
            Decision = f.Decision,
            DecidedAt = f.DecidedAt,
            DecidedBySystem = f.DecidedBySystem,
            DecisionCost = f.DecisionCost,
            AlternateRecipientName = f.AlternateRecipientName,
            AlternateRecipientPhone = f.AlternateRecipientPhone,
            AlternateAddress = f.AlternateAddress,
            ExtraDistanceKm = f.ExtraDistanceKm
        };

        private static string ReasonText(string reason) => reason switch
        {
            DeliveryFailureReasons.RecipientAbsent => "người nhận vắng mặt",
            DeliveryFailureReasons.RecipientRefused => "người nhận từ chối nhận",
            DeliveryFailureReasons.RefusedInjured => "từ chối vì ngựa bị thương",
            _ => "từ chối vì giao nhầm ngựa"
        };

        private static string VietnamTime(DateTimeOffset value) =>
            value.ToOffset(VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
