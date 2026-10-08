using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    public class TripService : ITripService
    {
        private readonly IRepository<Trip> _tripRepo;

        public TripService(IRepository<Trip> tripRepo)
        {
            _tripRepo = tripRepo;
        }

        public async Task<PagedBusinessResult<TripSummaryModel>> GetTripsAsync(TripFilterOptions options, CurrentUser currentUser)
        {
            EnsureStaff(currentUser);

            var query = _tripRepo.GetQueryable().AsNoTracking().AsSplitQuery();

            // Tài xế / áp tải chỉ thấy chuyến mình có tên trong đoàn; các role vận hành thấy tất cả
            if (currentUser.Role == UserRoles.DriverEscort)
            {
                query = query.Where(t => t.Crew.Any(c => c.UserId == currentUser.UserId));
            }

            // Filtering
            if (!string.IsNullOrWhiteSpace(options.Status))
            {
                if (!TripStatuses.All.Contains(options.Status))
                {
                    throw new BusinessException($"Status '{options.Status}' is invalid. Allowed values: {string.Join(", ", TripStatuses.All)}.");
                }
                query = query.Where(t => t.OverallStatus == options.Status);
            }

            if (options.StartFrom.HasValue)
            {
                var from = options.StartFrom.Value.ToUniversalTime();
                query = query.Where(t => t.PlannedStartDate >= from);
            }

            if (options.StartTo.HasValue)
            {
                var to = options.StartTo.Value.ToUniversalTime();
                query = query.Where(t => t.PlannedStartDate <= to);
            }

            // Searching
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                string search = options.Search.Trim().ToLower();
                query = query.Where(t => t.TripCode.ToLower().Contains(search)
                    || t.Vehicle!.AssetCode.ToLower().Contains(search));
            }

            query = query
                .Include(t => t.Vehicle)
                .Include(t => t.Crew).ThenInclude(c => c.User)
                .Include(t => t.TripHorses);

            // Sorting: mặc định chuyến khởi hành gần nhất lên đầu
            query = query.ApplySort(string.IsNullOrWhiteSpace(options.Sort) ? "-plannedStartDate" : options.Sort, nameof(Trip.TripId));

            // Paging
            return await query.ToPagedResultAsync(options, t => t.ToSummaryModel());
        }

        public async Task<TripDetailModel> GetTripByIdAsync(int id, CurrentUser currentUser)
        {
            EnsureStaff(currentUser);

            var trip = await _tripRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Vehicle)
                .Include(t => t.PlannedBy)
                .Include(t => t.ApprovedBy)
                .Include(t => t.ClosedBy)
                .Include(t => t.Crew).ThenInclude(c => c.User)
                .Include(t => t.Checkpoints)
                .Include(t => t.Handovers)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Dossier)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Booking!).ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null)
            {
                throw new NotFoundException($"Trip with ID {id} was not found.");
            }

            if (currentUser.Role == UserRoles.DriverEscort && !trip.Crew.Any(c => c.UserId == currentUser.UserId))
            {
                throw new ForbiddenException("You are not assigned to this trip.");
            }

            return trip.ToDetailModel();
        }

        // Khách hàng không xem chuyến trực tiếp: họ theo dõi qua đơn của mình (tracking, T-22)
        private static void EnsureStaff(CurrentUser currentUser)
        {
            if (currentUser.IsCustomer)
            {
                throw new ForbiddenException("Customers follow their shipment through their bookings, not trips.");
            }
        }
    }
}
