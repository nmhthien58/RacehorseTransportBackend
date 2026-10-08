using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    public class TripPlanningService : ITripPlanningService
    {
        // Chuyến ở hai trạng thái này không còn giữ xe, người hay ngựa
        private static readonly string[] ReleasedTripStatuses = { TripStatuses.Completed, TripStatuses.Cancelled };

        private readonly IRepository<BookingHorse> _bookingHorseRepo;
        private readonly IRepository<TransportAsset> _assetRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Trip> _tripRepo;

        public TripPlanningService(
            IRepository<BookingHorse> bookingHorseRepo,
            IRepository<TransportAsset> assetRepo,
            IRepository<User> userRepo,
            IRepository<Trip> tripRepo)
        {
            _bookingHorseRepo = bookingHorseRepo;
            _assetRepo = assetRepo;
            _userRepo = userRepo;
            _tripRepo = tripRepo;
        }

        public async Task<IReadOnlyList<UnplannedHorseModel>> GetUnplannedHorsesAsync(string? transportMode, int? bookingId)
        {
            var query = _bookingHorseRepo.GetQueryable().AsNoTracking()
                .Where(bh => bh.Status != BookingHorseStatuses.Cancelled
                    && bh.Booking!.Status == BookingStatuses.Approved
                    // Ngựa trên chuyến đã hủy được coi là chưa xếp; trên chuyến đã hoàn tất thì đã đi rồi
                    && !bh.TripHorses.Any(th => th.Trip!.OverallStatus != TripStatuses.Cancelled));

            if (!string.IsNullOrWhiteSpace(transportMode))
            {
                ValidateMode(transportMode);
                query = query.Where(bh => bh.Booking!.TransportMode == transportMode);
            }

            if (bookingId.HasValue)
            {
                query = query.Where(bh => bh.BookingId == bookingId.Value);
            }

            // Đơn khởi hành sớm nhất lên đầu
            return await query
                .OrderBy(bh => bh.Booking!.DepartureDate)
                .ThenBy(bh => bh.BookingId)
                .ThenBy(bh => bh.BookingHorseId)
                .Select(bh => new UnplannedHorseModel
                {
                    BookingHorseId = bh.BookingHorseId,
                    BookingId = bh.BookingId,
                    BookingCode = bh.Booking!.BookingCode,
                    CustomerName = bh.Booking.Customer!.FullName,
                    TransportMode = bh.Booking.TransportMode,
                    PickupAddress = bh.Booking.PickupAddress,
                    PickupCountryCode = bh.Booking.PickupCountryCode,
                    DropoffAddress = bh.Booking.DropoffAddress,
                    DropoffCountryCode = bh.Booking.DropoffCountryCode,
                    DepartureDate = bh.Booking.DepartureDate,
                    DeliveryDate = bh.Booking.DeliveryDate,
                    RequiresClimateControl = bh.Booking.RequiresClimateControl,
                    HorseId = bh.HorseId,
                    HorseName = bh.Horse!.Name,
                    MicrochipNumber = bh.Horse.MicrochipNumber,
                    SpecialCareRequirements = bh.Horse.SpecialCareRequirements,
                    StallClass = bh.StallClass,
                    DossierStatus = bh.Dossier!.Status
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<AvailableVehicleModel>> GetAvailableVehiclesAsync(DateTimeOffset from, DateTimeOffset to, string? transportMode)
        {
            var (fromUtc, toUtc) = NormalizeWindow(from, to);

            var busyVehicleIds = OverlappingTrips(fromUtc, toUtc).Select(t => t.VehicleId);

            var query = _assetRepo.GetQueryable().AsNoTracking()
                .Where(a => a.Status != AssetStatuses.Maintenance && !busyVehicleIds.Contains(a.AssetId));

            // Đơn đường bay đi bằng khoang bay (AirStall), đơn đường bộ đi bằng xe
            if (!string.IsNullOrWhiteSpace(transportMode))
            {
                ValidateMode(transportMode);
                query = transportMode == TransportModes.Air
                    ? query.Where(a => a.AssetType == AssetTypes.AirStall)
                    : query.Where(a => a.AssetType != AssetTypes.AirStall);
            }

            return await query
                .OrderBy(a => a.AssetCode)
                .Select(a => new AvailableVehicleModel
                {
                    AssetId = a.AssetId,
                    AssetCode = a.AssetCode,
                    AssetType = a.AssetType,
                    CapacityHorses = a.CapacityHorses,
                    HasClimateControl = a.HasClimateControl,
                    HasGpsTracker = a.HasGpsTracker,
                    LastSanitizationDate = a.LastSanitizationDate
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<UserModel>> GetAvailableCrewAsync(DateTimeOffset from, DateTimeOffset to)
        {
            var (fromUtc, toUtc) = NormalizeWindow(from, to);

            var busyUserIds = OverlappingTrips(fromUtc, toUtc).SelectMany(t => t.Crew).Select(c => c.UserId);

            var crew = await _userRepo.GetQueryable().AsNoTracking()
                .Where(u => u.Role == UserRoles.DriverEscort && u.IsActive && !busyUserIds.Contains(u.UserId))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return crew.Select(u => u.ToBusinessModel()).ToList();
        }

        // Các chuyến còn hiệu lực có thời gian kế hoạch giao với [from, to]
        private IQueryable<Trip> OverlappingTrips(DateTimeOffset fromUtc, DateTimeOffset toUtc)
        {
            return _tripRepo.GetQueryable()
                .Where(t => !ReleasedTripStatuses.Contains(t.OverallStatus)
                    && t.PlannedStartDate < toUtc
                    && t.PlannedEndDate > fromUtc);
        }

        private static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) NormalizeWindow(DateTimeOffset from, DateTimeOffset to)
        {
            if (to <= from)
            {
                throw new BusinessException("'to' must be after 'from'.");
            }
            return (from.ToUniversalTime(), to.ToUniversalTime());
        }

        private static void ValidateMode(string transportMode)
        {
            if (!TransportModes.All.Contains(transportMode))
            {
                throw new BusinessException($"TransportMode '{transportMode}' is invalid. Allowed values: {string.Join(", ", TransportModes.All)}.");
            }
        }
    }
}
