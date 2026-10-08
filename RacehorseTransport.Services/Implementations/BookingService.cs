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
    public class BookingService : IBookingService
    {
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IPricingService _pricingService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly BookingOptions _options;

        public BookingService(
            IRepository<BookingRequest> bookingRepo,
            IPricingService pricingService,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            IOptions<BookingOptions> options)
        {
            _bookingRepo = bookingRepo;
            _pricingService = pricingService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _options = options.Value;
        }

        public async Task<PagedBusinessResult<BookingSummaryModel>> GetBookingsAsync(BookingFilterOptions options, CurrentUser currentUser)
        {
            var query = ApplyVisibility(_bookingRepo.GetQueryable().AsNoTracking(), currentUser)
                .Include(b => b.Customer)
                .Include(b => b.AssignedSpecialist)
                .AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(options.Status))
            {
                if (!BookingStatuses.All.Contains(options.Status))
                {
                    throw new BusinessException($"Status '{options.Status}' is invalid. Allowed values: {string.Join(", ", BookingStatuses.All)}.");
                }
                query = query.Where(b => b.Status == options.Status);
            }

            if (options.CustomerUserId.HasValue && currentUser.IsStaff)
            {
                query = query.Where(b => b.CustomerUserId == options.CustomerUserId.Value);
            }

            // Tham số thời gian phải đổi về UTC trước khi so với cột timestamptz
            if (options.DepartureFrom.HasValue)
            {
                var from = options.DepartureFrom.Value.ToUniversalTime();
                query = query.Where(b => b.DepartureDate >= from);
            }

            if (options.DepartureTo.HasValue)
            {
                var to = options.DepartureTo.Value.ToUniversalTime();
                query = query.Where(b => b.DepartureDate <= to);
            }

            // Searching
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                string search = options.Search.Trim().ToLower();
                query = query.Where(b => b.BookingCode.ToLower().Contains(search)
                    || b.PickupAddress.ToLower().Contains(search)
                    || b.DropoffAddress.ToLower().Contains(search)
                    || b.Customer!.FullName.ToLower().Contains(search));
            }

            // Sorting: mặc định đơn mới nhất lên đầu
            query = query.ApplySort(string.IsNullOrWhiteSpace(options.Sort) ? "-createdAt" : options.Sort);

            // Paging
            return await query.ToPagedResultAsync(options, b => b.ToSummaryModel());
        }

        public async Task<BookingDetailModel> GetBookingByIdAsync(int id, CurrentUser currentUser)
        {
            var booking = await _bookingRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.Customer)
                .Include(b => b.AssignedSpecialist)
                .Include(b => b.ReviewedBy)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Horse)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Dossier)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.TripHorses).ThenInclude(th => th.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {id} was not found.");
            }

            if (!CanView(booking, currentUser))
            {
                throw new ForbiddenException("You do not have access to this booking.");
            }

            return booking.ToDetailModel(_pricingService.ParseLines(booking.QuoteBreakdown));
        }

        public async Task<int> ExpireOverdueBookingsAsync()
        {
            var cutoff = DateTimeOffset.UtcNow.AddHours(-_options.ReviewSlaHours);

            var overdue = await _bookingRepo.GetQueryable()
                .Where(b => b.Status == BookingStatuses.Submitted && b.CreatedAt < cutoff)
                .ToListAsync();

            foreach (var booking in overdue)
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    booking.Status = BookingStatuses.Expired;
                    await _bookingRepo.UpdateAsync(booking);

                    await _notificationService.SendAsync(
                        booking.CustomerUserId,
                        $"Booking {booking.BookingCode} has expired",
                        $"Your transport request was not reviewed within {_options.ReviewSlaHours} hours and has expired. Please submit a new request.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId);
                });
            }

            return overdue.Count;
        }

        // Ai thấy đơn nào:
        //   Customer            -> đơn của chính mình
        //   TransportSpecialist -> đơn mình được giao phụ trách
        //   Manager, Coordinator, Admin -> tất cả
        //   DriverEscort        -> không xem đơn (làm việc theo chuyến)
        private static IQueryable<BookingRequest> ApplyVisibility(IQueryable<BookingRequest> query, CurrentUser currentUser)
        {
            if (currentUser.IsCustomer)
            {
                return query.Where(b => b.CustomerUserId == currentUser.UserId);
            }

            if (currentUser.Role == UserRoles.TransportSpecialist)
            {
                return query.Where(b => b.AssignedSpecialistId == currentUser.UserId);
            }

            if (currentUser.Role == UserRoles.DriverEscort)
            {
                throw new ForbiddenException("Drivers and escorts work with trips, not bookings.");
            }

            return query;
        }

        private static bool CanView(BookingRequest booking, CurrentUser currentUser)
        {
            if (currentUser.IsCustomer)
            {
                return booking.CustomerUserId == currentUser.UserId;
            }

            if (currentUser.Role == UserRoles.TransportSpecialist)
            {
                return booking.AssignedSpecialistId == currentUser.UserId;
            }

            return currentUser.Role != UserRoles.DriverEscort;
        }
    }
}
