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
    // Flow 6:
    //   ArrivedDestination ──(mỗi đơn trên chuyến ký một biên bản e-POD)──> (đủ biên bản) ──(Manager đóng chuyến)──> Completed
    public class TripCompletionService : ITripCompletionService
    {
        // Sự cố ở các trạng thái này được tính là bất khả kháng và chi phí phát sinh được cộng vào chuyến
        private static readonly string[] AcceptedIncidentStatuses = { IncidentStatuses.Approved, IncidentStatuses.Resolved };

        // Đơn còn đang chạy (Assigned là trạng thái cũ, không còn được tạo mới)
        private static readonly string[] OpenBookingStatuses = { BookingStatuses.Approved, BookingStatuses.Assigned };

        private static readonly string[] FinishedTripStatuses = { TripStatuses.Completed, TripStatuses.Cancelled };

        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<HandoverAcceptance> _handoverRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<TransportAsset> _assetRepo;
        private readonly ITripService _tripService;
        private readonly IBookingService _bookingService;
        private readonly ITrackingService _trackingService;
        private readonly IFileStorageService _fileStorage;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public TripCompletionService(
            IRepository<Trip> tripRepo,
            IRepository<HandoverAcceptance> handoverRepo,
            IRepository<BookingRequest> bookingRepo,
            IRepository<TransportAsset> assetRepo,
            ITripService tripService,
            IBookingService bookingService,
            ITrackingService trackingService,
            IFileStorageService fileStorage,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _tripRepo = tripRepo;
            _handoverRepo = handoverRepo;
            _bookingRepo = bookingRepo;
            _assetRepo = assetRepo;
            _tripService = tripService;
            _bookingService = bookingService;
            _trackingService = trackingService;
            _fileStorage = fileStorage;
            _notificationService = notificationService;
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

            if (trip.OverallStatus != TripStatuses.ArrivedDestination)
            {
                throw new BusinessException($"Horses can only be handed over after the trip has arrived at the destination (current status: {trip.OverallStatus}).");
            }

            if (!HandoverConditions.All.Contains(model.OverallCondition))
            {
                throw new BusinessException($"OverallCondition '{model.OverallCondition}' is invalid. Allowed values: {string.Join(", ", HandoverConditions.All)}.");
            }

            // Ngựa của đơn này trên chuyến
            var horses = trip.TripHorses.Where(th => th.BookingHorse!.BookingId == model.BookingId).ToList();
            if (horses.Count == 0)
            {
                throw new BusinessException($"Booking with ID {model.BookingId} has no horses on trip {trip.TripCode}.");
            }
            var booking = horses[0].BookingHorse!.Booking!;

            if (trip.Handovers.Any(h => h.BookingId == model.BookingId))
            {
                throw new BusinessException($"Booking {booking.BookingCode} has already been handed over on trip {trip.TripCode}.");
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

            // Mọi kiểm tra nghiệp vụ đã qua mới đẩy ảnh chữ ký lên storage
            var signature = await _fileStorage.UploadAsync(
                model.Signature.Content, model.Signature.FileName, model.Signature.SizeBytes, FileCategories.Signatures);

            var handover = new HandoverAcceptance
            {
                TripId = tripId,
                BookingId = model.BookingId,
                RecipientName = model.RecipientName,
                RecipientPhone = model.RecipientPhone,
                RecipientIdCard = model.RecipientIdCard,
                DigitalSignatureUrl = signature.Path,
                OverallCondition = model.OverallCondition,
                Remarks = model.Remarks,
                DriverUserId = currentUser.UserId
            };

            // Đây có phải biên bản cuối cùng của chuyến không
            var bookingIdsOnTrip = trip.TripHorses.Select(th => th.BookingHorse!.BookingId).Distinct().ToList();
            bool allHandedOver = bookingIdsOnTrip.All(id => id == model.BookingId || trip.Handovers.Any(h => h.BookingId == id));

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _handoverRepo.AddAsync(handover);

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Booking {booking.BookingCode} has been delivered",
                    $"{horses.Count} horse(s) were handed over to {handover.RecipientName} (condition: {handover.OverallCondition}).",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);

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

            // Mọi đơn trên chuyến phải đã ký biên bản bàn giao
            var notHandedOver = trip.TripHorses
                .Select(th => th.BookingHorse!.Booking!)
                .DistinctBy(b => b.BookingId)
                .Where(b => !trip.Handovers.Any(h => h.BookingId == b.BookingId))
                .Select(b => b.BookingCode)
                .ToList();

            if (notHandedOver.Count > 0)
            {
                throw new BusinessException($"The trip cannot be closed: handover is not signed for {string.Join(", ", notHandedOver)}.");
            }

            var acceptedIncidents = trip.Incidents.Where(i => AcceptedIncidentStatuses.Contains(i.Status)).ToList();
            var now = DateTimeOffset.UtcNow;
            var kpi = TripKpiCalculator.Calculate(trip.PlannedEndDate, trip.ActualEndDate ?? now, acceptedIncidents.Count > 0);

            var bookingIds = trip.TripHorses.Select(th => th.BookingHorse!.BookingId).Distinct().ToList();
            var customerIds = trip.TripHorses.Select(th => th.BookingHorse!.Booking!.CustomerUserId).Distinct().ToList();

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

                var completedBookings = await CompleteFinishedBookingsAsync(bookingIds);

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
            });

            await _trackingService.InvalidateTripAsync(tripId);
            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        // Đơn hoàn tất khi mọi con ngựa chưa hủy của đơn đã nằm trên ít nhất một chuyến Completed
        // và đơn không còn chuyến nào đang dang dở.
        private async Task<List<BookingRequest>> CompleteFinishedBookingsAsync(List<int> bookingIds)
        {
            var bookings = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.TripHorses).ThenInclude(th => th.Trip)
                .Where(b => bookingIds.Contains(b.BookingId) && OpenBookingStatuses.Contains(b.Status))
                .ToListAsync();

            var completed = new List<BookingRequest>();
            foreach (var booking in bookings)
            {
                var activeHorses = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).ToList();

                bool allDelivered = activeHorses.Count > 0
                    && activeHorses.All(bh => bh.TripHorses.Any(th => th.Trip!.OverallStatus == TripStatuses.Completed));
                bool hasUnfinishedTrip = activeHorses
                    .SelectMany(bh => bh.TripHorses)
                    .Any(th => !FinishedTripStatuses.Contains(th.Trip!.OverallStatus));

                if (allDelivered && !hasUnfinishedTrip)
                {
                    booking.Status = BookingStatuses.Completed;
                    completed.Add(booking);
                }
            }

            await _bookingRepo.SaveChangesAsync();
            return completed;
        }

        // ---------------------------------------------------------------------------------------------

        private async Task<Trip> GetTripWithCargoAsync(int tripId)
        {
            var trip = await _tripRepo.GetQueryable()
                .AsSplitQuery()
                .Include(t => t.Crew)
                .Include(t => t.Handovers)
                .Include(t => t.Incidents)
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
                Horses = (h.Trip?.TripHorses ?? Enumerable.Empty<TripHorse>())
                    .Where(th => th.BookingHorse?.BookingId == h.BookingId)
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
