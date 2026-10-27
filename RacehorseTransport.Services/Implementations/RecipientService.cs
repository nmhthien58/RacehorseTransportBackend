using System.Globalization;
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
    // R-16 (BE-15, Chính sách mục 10): người nhận ngựa.
    //   Khách nhập người nhận khi gửi đơn (hoặc "Tôi tự nhận"), sửa được tới khi xe check-in điểm giao.
    //   Email người nhận: chuyến được duyệt (RecipientScheduled), xe xuất phát (RecipientDeparted, ghi khung giờ đã báo),
    //   xe rời mốc ngay trước điểm giao (RecipientArrivingSoon). 24 giờ trước ETA: nhắc khách xác nhận người nhận có mặt.
    //   e-POD: người ký phải là người nhận trong đơn hoặc người nhận thay khách đã xác nhận; ký xong báo khách (HandoverCompleted).
    public class RecipientService : IRecipientService
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly string[] ClosedStatuses =
        {
            BookingStatuses.Completed, BookingStatuses.Cancelled, BookingStatuses.Rejected,
            BookingStatuses.Expired, BookingStatuses.QuoteExpired, BookingStatuses.AwaitingFinalPayment
        };
        private static readonly string[] ActiveTripStatuses =
        {
            TripStatuses.Scheduled, TripStatuses.InTransit, TripStatuses.EmergencyRerouting, TripStatuses.ArrivedDestination
        };

        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IRepository<BookingContract> _contractRepo;
        private readonly IContractService _contractService;
        private readonly INotificationService _notificationService;

        public RecipientService(
            IRepository<BookingRequest> bookingRepo,
            IRepository<User> userRepo,
            IRepository<Trip> tripRepo,
            IRepository<TripHorse> tripHorseRepo,
            IRepository<BookingContract> contractRepo,
            IContractService contractService,
            INotificationService notificationService)
        {
            _bookingRepo = bookingRepo;
            _userRepo = userRepo;
            _tripRepo = tripRepo;
            _tripHorseRepo = tripHorseRepo;
            _contractRepo = contractRepo;
            _contractService = contractService;
            _notificationService = notificationService;
        }

        // =============================================================================================
        // Khách nhập / sửa người nhận
        // =============================================================================================

        public async Task ApplyRecipientAsync(BookingRequest booking, RecipientInputModel input)
        {
            if (input.RecipientIsCustomer)
            {
                var customer = await _userRepo.GetQueryable().AsNoTracking().FirstAsync(u => u.UserId == booking.CustomerUserId);
                if (string.IsNullOrWhiteSpace(customer.PhoneNumber))
                {
                    throw new BusinessException("Add a phone number to your profile to receive the horses yourself, or enter another recipient.");
                }
                booking.RecipientIsCustomer = true;
                booking.RecipientName = customer.FullName;
                booking.RecipientPhone = customer.PhoneNumber;
                booking.RecipientEmail = customer.Email;
                booking.RecipientIdNumber = Clean(input.RecipientIdNumber);
                return;
            }

            if (string.IsNullOrWhiteSpace(input.RecipientName) || string.IsNullOrWhiteSpace(input.RecipientPhone))
            {
                throw new BusinessException("The recipient's name and phone number are required (or set RecipientIsCustomer = true to receive the horses yourself).");
            }
            booking.RecipientIsCustomer = false;
            booking.RecipientName = input.RecipientName.Trim();
            booking.RecipientPhone = input.RecipientPhone.Trim();
            booking.RecipientEmail = Clean(input.RecipientEmail);
            booking.RecipientIdNumber = Clean(input.RecipientIdNumber);
        }

        public async Task UpdateRecipientAsync(int bookingId, RecipientInputModel input, CurrentUser currentUser)
        {
            var booking = await GetOwnBookingAsync(bookingId, currentUser);
            if (ClosedStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"The recipient can no longer be changed (booking status: {booking.Status}).");
            }
            // Sửa được tới khi xe check-in điểm giao của đơn
            var trips = await LoadTripsAsync(bookingId);
            if (trips.Any(t => TripRouteRules.DropoffCheckpoint(ActiveRoute(t), bookingId)?.ActualArrivalTime != null))
            {
                throw new BusinessException("The vehicle has already arrived at the drop-off point; the recipient can no longer be changed in the app. Please call the crew.");
            }

            string? previousName = booking.RecipientName;
            await ApplyRecipientAsync(booking, input);
            bool changed = RecipientRules.NormalizeName(previousName) != RecipientRules.NormalizeName(booking.RecipientName);
            if (changed)
            {
                // Người nhận mới phải xác nhận lại; người nhận thay đã duyệt không còn giá trị
                booking.RecipientConfirmedAt = null;
                booking.RecipientConfirmRequestedAt = null;
                booking.AlternateRecipientName = null;
                booking.AlternateRecipientIdNumber = null;
                booking.AlternateRecipientApprovedAt = null;
            }
            await _bookingRepo.SaveChangesAsync();

            if (!changed)
            {
                return;
            }

            // Chuyến đã lên lịch: báo người nhận mới; nhân sự trên chuyến biết người nhận đã đổi
            foreach (var trip in trips.Where(t => ActiveTripStatuses.Contains(t.OverallStatus)))
            {
                await SendScheduledEmailAsync(trip, booking);
                await _notificationService.SendToUsersAsync(
                    trip.Crew.Select(c => c.UserId),
                    $"Recipient changed for booking {booking.BookingCode}",
                    $"New recipient: {booking.RecipientName} ({booking.RecipientPhone}).",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);
            }

            // R-17: đổi người nhận sau khi đã giao kết hợp đồng -> bản hợp đồng mới để khách chấp nhận
            bool hasAcceptedContract = await _contractRepo.GetQueryable().AnyAsync(c => c.BookingId == bookingId && c.Status == ContractStatuses.Accepted);
            if (hasAcceptedContract)
            {
                await _contractService.CreateAmendmentAsync(bookingId, ContractReasons.RecipientChanged);
            }
        }

        public async Task ConfirmRecipientAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await GetOwnBookingAsync(bookingId, currentUser);
            if (string.IsNullOrWhiteSpace(booking.RecipientName))
            {
                throw new BusinessException("This booking has no recipient yet. Add the recipient first.");
            }
            if (ClosedStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"The booking is already closed (status: {booking.Status}).");
            }
            booking.RecipientConfirmedAt = DateTimeOffset.UtcNow;
            await _bookingRepo.SaveChangesAsync();
        }

        public async Task ApproveAlternateRecipientAsync(int bookingId, string name, string? idNumber, CurrentUser currentUser)
        {
            var booking = await GetOwnBookingAsync(bookingId, currentUser);
            if (ClosedStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"The booking is already closed (status: {booking.Status}).");
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new BusinessException("Name is required.");
            }
            booking.AlternateRecipientName = name.Trim();
            booking.AlternateRecipientIdNumber = Clean(idNumber);
            booking.AlternateRecipientApprovedAt = DateTimeOffset.UtcNow;
            await _bookingRepo.SaveChangesAsync();

            foreach (var trip in (await LoadTripsAsync(bookingId)).Where(t => ActiveTripStatuses.Contains(t.OverallStatus)))
            {
                await _notificationService.SendToUsersAsync(
                    trip.Crew.Select(c => c.UserId),
                    $"Alternate recipient approved for booking {booking.BookingCode}",
                    $"The customer allows {booking.AlternateRecipientName} to sign for the horses.",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);
            }
        }

        // =============================================================================================
        // Đoàn xe xem người nhận của từng đơn trên chuyến (e-POD điền sẵn, nút gọi điện)
        // =============================================================================================

        public async Task<List<TripRecipientModel>> GetTripRecipientsAsync(int tripId, CurrentUser currentUser)
        {
            var trip = await _tripRepo.GetQueryable().AsNoTracking()
                .Include(t => t.Crew)
                .Include(t => t.Checkpoints)
                .FirstOrDefaultAsync(t => t.TripId == tripId)
                ?? throw new NotFoundException($"Trip with ID {tripId} was not found.");
            if (currentUser.IsCustomer
                || (currentUser.Role == UserRoles.DriverEscort && !trip.Crew.Any(c => c.UserId == currentUser.UserId)))
            {
                throw new ForbiddenException("Only staff and the crew of this trip can see the recipients.");
            }

            var bookings = await BookingsOnTripAsync(tripId);
            var route = ActiveRoute(trip);
            return bookings.Select(b =>
            {
                var dropoff = TripRouteRules.DropoffCheckpoint(route, b.BookingId);
                return new TripRecipientModel
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    DropoffCheckpointId = dropoff?.CheckpointId,
                    DropoffCheckpointName = dropoff?.CheckpointName,
                    RecipientName = b.RecipientName,
                    RecipientPhone = b.RecipientPhone,
                    RecipientEmail = b.RecipientEmail,
                    RecipientIdNumber = b.RecipientIdNumber,
                    RecipientIsCustomer = b.RecipientIsCustomer,
                    RecipientConfirmedAt = b.RecipientConfirmedAt,
                    AlternateRecipientName = b.AlternateRecipientName,
                    AlternateRecipientIdNumber = b.AlternateRecipientIdNumber,
                    AlternateRecipientApprovedAt = b.AlternateRecipientApprovedAt,
                    NotifiedWindowStart = b.RecipientNotifiedWindowStart,
                    NotifiedWindowEnd = b.RecipientNotifiedWindowEnd
                };
            }).ToList();
        }

        // =============================================================================================
        // Gọi từ luồng chuyến
        // =============================================================================================

        public async Task OnTripApprovedAsync(int tripId)
        {
            var trip = await LoadTripAsync(tripId);
            foreach (var booking in await BookingsOnTripAsync(tripId))
            {
                await SendScheduledEmailAsync(trip, booking);
            }
        }

        public async Task OnTripStartedAsync(int tripId)
        {
            var trip = await LoadTripAsync(tripId);
            var etas = LiveEtaCalculator.Calculate(ActiveRoute(trip), trip.OverallStatus, DateTimeOffset.UtcNow).EstimatedArrivals;
            foreach (var booking in await BookingsOnTripAsync(tripId, tracked: true))
            {
                var dropoff = TripRouteRules.DropoffCheckpoint(ActiveRoute(trip), booking.BookingId);
                if (dropoff == null || !etas.TryGetValue(dropoff.CheckpointId, out var eta))
                {
                    continue;
                }
                (booking.RecipientNotifiedWindowStart, booking.RecipientNotifiedWindowEnd) = RecipientRules.NotifiedWindow(eta);
                await EmailRecipientAsync(booking, EmailTemplateNames.RecipientDeparted, new Dictionary<string, string>
                {
                    ["RecipientName"] = booking.RecipientName ?? string.Empty,
                    ["BookingCode"] = booking.BookingCode,
                    ["Eta"] = VietnamTime(eta)
                });
            }
            await _bookingRepo.SaveChangesAsync();
        }

        public async Task OnCheckpointDepartedAsync(int tripId, int checkpointId)
        {
            var trip = await LoadTripAsync(tripId);
            var route = ActiveRoute(trip);
            int index = route.FindIndex(c => c.CheckpointId == checkpointId);
            if (index < 0 || index + 1 >= route.Count)
            {
                return;
            }
            var next = route[index + 1];
            var etas = LiveEtaCalculator.Calculate(route, trip.OverallStatus, DateTimeOffset.UtcNow).EstimatedArrivals;
            if (!etas.TryGetValue(next.CheckpointId, out var eta))
            {
                return;
            }
            string? driverPhone = trip.Crew
                .OrderByDescending(c => c.CrewRole == CrewRoles.Driver).ThenByDescending(c => c.IsLead)
                .Select(c => c.User?.PhoneNumber)
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

            // Đơn có điểm giao là mốc kế tiếp và còn ngựa trên xe
            var onBoardBookingIds = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId && TripHorseStatuses.OnBoard.Contains(th.Status))
                .Select(th => th.BookingHorse!.BookingId)
                .Distinct()
                .ToListAsync();
            foreach (var booking in (await BookingsOnTripAsync(tripId, tracked: true)).Where(b => onBoardBookingIds.Contains(b.BookingId)))
            {
                if (TripRouteRules.DropoffCheckpoint(route, booking.BookingId)?.CheckpointId != next.CheckpointId)
                {
                    continue;
                }
                var window = RecipientRules.NotifiedWindow(eta);
                (booking.RecipientNotifiedWindowStart, booking.RecipientNotifiedWindowEnd) = window;
                await EmailRecipientAsync(booking, EmailTemplateNames.RecipientArrivingSoon, new Dictionary<string, string>
                {
                    ["RecipientName"] = booking.RecipientName ?? string.Empty,
                    ["BookingCode"] = booking.BookingCode,
                    ["EtaWindow"] = $"{window.Start.ToOffset(VietnamOffset):HH:mm} - {window.End.ToOffset(VietnamOffset):HH:mm dd/MM/yyyy}",
                    ["DriverPhone"] = driverPhone ?? string.Empty
                });
            }
            await _bookingRepo.SaveChangesAsync();
        }

        public async Task EnsureHandoverSignerAsync(int bookingId, string signerName)
        {
            var booking = await _bookingRepo.GetQueryable().AsNoTracking().FirstAsync(b => b.BookingId == bookingId);
            string? alternate = booking.AlternateRecipientApprovedAt != null ? booking.AlternateRecipientName : null;
            if (!RecipientRules.IsAllowedSigner(signerName, booking.RecipientName, alternate))
            {
                throw new BusinessException(
                    $"{signerName} is not the recipient of booking {booking.BookingCode} ({booking.RecipientName}). " +
                    "Ask the customer to approve this person as an alternate recipient in the app before handing over.");
            }
        }

        public async Task OnHandoverSignedAsync(int bookingId, string recipientName, DateTimeOffset signedAt, string condition)
        {
            var booking = await _bookingRepo.GetQueryable().AsNoTracking().FirstAsync(b => b.BookingId == bookingId);
            await _notificationService.EmailCustomerAsync(
                booking.CustomerUserId,
                EmailTemplateNames.HandoverCompleted,
                CustomerPages.Booking(booking.BookingId),
                new Dictionary<string, string>
                {
                    ["BookingCode"] = booking.BookingCode,
                    ["RecipientName"] = recipientName,
                    ["SignedAt"] = VietnamTime(signedAt),
                    ["Condition"] = condition
                });
        }

        // =============================================================================================
        // Job: 24 giờ trước ETA tới điểm giao -> nhắc khách xác nhận người nhận có mặt
        // =============================================================================================

        public async Task<int> ProcessConfirmationRemindersAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var trips = await _tripRepo.GetQueryable().AsNoTracking()
                .Include(t => t.Checkpoints)
                .Where(t => t.OverallStatus == TripStatuses.Scheduled || t.OverallStatus == TripStatuses.InTransit || t.OverallStatus == TripStatuses.EmergencyRerouting)
                .ToListAsync();

            int sent = 0;
            foreach (var trip in trips)
            {
                var route = ActiveRoute(trip);
                var etas = LiveEtaCalculator.Calculate(route, trip.OverallStatus, now).EstimatedArrivals;
                foreach (var booking in await BookingsOnTripAsync(trip.TripId, tracked: true))
                {
                    var dropoff = TripRouteRules.DropoffCheckpoint(route, booking.BookingId);
                    if (string.IsNullOrWhiteSpace(booking.RecipientName) || dropoff == null || dropoff.ActualArrivalTime != null
                        || !etas.TryGetValue(dropoff.CheckpointId, out var eta)
                        || !RecipientRules.NeedsConfirmRequest(eta, booking.RecipientConfirmRequestedAt, booking.RecipientConfirmedAt, now))
                    {
                        continue;
                    }
                    booking.RecipientConfirmRequestedAt = now;
                    await _bookingRepo.SaveChangesAsync();
                    await _notificationService.SendAsync(
                        booking.CustomerUserId,
                        $"Please confirm the recipient for booking {booking.BookingCode}",
                        $"The horses are expected at {dropoff.CheckpointName} around {VietnamTime(eta)} (Vietnam time). Confirm that {booking.RecipientName} will be there, or change the recipient in the app.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId);
                    await _notificationService.EmailCustomerAsync(
                        booking.CustomerUserId,
                        EmailTemplateNames.ConfirmRecipient,
                        CustomerPages.Booking(booking.BookingId),
                        new Dictionary<string, string>
                        {
                            ["BookingCode"] = booking.BookingCode,
                            ["RecipientName"] = booking.RecipientName!,
                            ["Eta"] = VietnamTime(eta)
                        });
                    sent++;
                }
            }
            return sent;
        }

        // ---------------------------------------------------------------------------------------------

        private async Task SendScheduledEmailAsync(Trip trip, BookingRequest booking)
        {
            var dropoff = TripRouteRules.DropoffCheckpoint(ActiveRoute(trip), booking.BookingId);
            var horseNames = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == trip.TripId && th.BookingHorse!.BookingId == booking.BookingId)
                .Select(th => th.BookingHorse!.Horse!.Name)
                .ToListAsync();
            string? contactPhone = trip.Crew
                .OrderByDescending(c => c.CrewRole == CrewRoles.Driver).ThenByDescending(c => c.IsLead)
                .Select(c => c.User?.PhoneNumber)
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

            await EmailRecipientAsync(booking, EmailTemplateNames.RecipientScheduled, new Dictionary<string, string>
            {
                ["RecipientName"] = booking.RecipientName ?? string.Empty,
                ["BookingCode"] = booking.BookingCode,
                ["HorseNames"] = string.Join(", ", horseNames),
                ["ExpectedDate"] = dropoff != null ? VietnamTime(dropoff.PlannedTime) : string.Empty,
                ["ContactPhone"] = contactPhone ?? string.Empty
            });
        }

        // Người nhận không có email thì bỏ qua (chính sách: chỉ email, không SMS)
        private async Task EmailRecipientAsync(BookingRequest booking, string template, Dictionary<string, string> values)
        {
            if (string.IsNullOrWhiteSpace(booking.RecipientEmail))
            {
                return;
            }
            await _notificationService.EmailAddressAsync(booking.RecipientEmail, booking.RecipientName ?? string.Empty, template, values);
        }

        private async Task<BookingRequest> GetOwnBookingAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await _bookingRepo.GetQueryable().FirstOrDefaultAsync(b => b.BookingId == bookingId)
                ?? throw new NotFoundException($"Booking with ID {bookingId} was not found.");
            if (booking.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only manage the recipient of your own bookings.");
            }
            return booking;
        }

        private Task<Trip> LoadTripAsync(int tripId) =>
            _tripRepo.GetQueryable().AsNoTracking()
                .Include(t => t.Checkpoints)
                .Include(t => t.Crew).ThenInclude(c => c.User)
                .FirstAsync(t => t.TripId == tripId);

        private Task<List<Trip>> LoadTripsAsync(int bookingId) =>
            _tripRepo.GetQueryable().AsNoTracking()
                .Include(t => t.Checkpoints)
                .Include(t => t.Crew).ThenInclude(c => c.User)
                .Where(t => t.OverallStatus != TripStatuses.Cancelled
                    && t.TripHorses.Any(th => th.BookingHorse!.BookingId == bookingId))
                .ToListAsync();

        private async Task<List<BookingRequest>> BookingsOnTripAsync(int tripId, bool tracked = false)
        {
            var ids = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId)
                .Select(th => th.BookingHorse!.BookingId)
                .Distinct()
                .ToListAsync();
            var query = _bookingRepo.GetQueryable().Where(b => ids.Contains(b.BookingId));
            return await (tracked ? query : query.AsNoTracking()).ToListAsync();
        }

        private static List<RouteCheckpoint> ActiveRoute(Trip trip) =>
            trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string VietnamTime(DateTimeOffset value) =>
            value.ToOffset(VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
