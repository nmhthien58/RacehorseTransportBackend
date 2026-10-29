using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Phần tra cứu nằm ở file này; phần lập kế hoạch và trình duyệt nằm ở TripPlanningService.Workflow.cs
    public partial class TripPlanningService : ITripPlanningService
    {
        // Chuyến ở hai trạng thái này không còn giữ xe, người hay ngựa
        private static readonly string[] ReleasedTripStatuses = { TripStatuses.Completed, TripStatuses.Cancelled };

        private readonly IRepository<BookingHorse> _bookingHorseRepo;
        private readonly IRepository<TransportAsset> _assetRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<TripCrew> _crewRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IRepository<RouteCheckpoint> _checkpointRepo;
        private readonly IRepository<RoutePlan> _routePlanRepo;
        private readonly ITripService _tripService;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;
        private readonly IRecipientService _recipientService;
        private readonly ISystemSettingService _settingService;
        private readonly IDistanceService _distanceService;
        private readonly ITransitTimeEstimator _transitEstimator;
        private readonly IGeoService _geoService;
        private readonly TransitOptions _transitOptions;
        private readonly IUnitOfWork _unitOfWork;

        public TripPlanningService(
            IRepository<BookingHorse> bookingHorseRepo,
            IRepository<TransportAsset> assetRepo,
            IRepository<User> userRepo,
            IRepository<Trip> tripRepo,
            IRepository<TripCrew> crewRepo,
            IRepository<TripHorse> tripHorseRepo,
            IRepository<RouteCheckpoint> checkpointRepo,
            IRepository<RoutePlan> routePlanRepo,
            ITripService tripService,
            ICodeGenerator codeGenerator,
            INotificationService notificationService,
            IRecipientService recipientService,
            ISystemSettingService settingService,
            IDistanceService distanceService,
            ITransitTimeEstimator transitEstimator,
            IGeoService geoService,
            IOptions<TransitOptions> transitOptions,
            IUnitOfWork unitOfWork)
        {
            _bookingHorseRepo = bookingHorseRepo;
            _assetRepo = assetRepo;
            _userRepo = userRepo;
            _tripRepo = tripRepo;
            _crewRepo = crewRepo;
            _tripHorseRepo = tripHorseRepo;
            _checkpointRepo = checkpointRepo;
            _routePlanRepo = routePlanRepo;
            _tripService = tripService;
            _codeGenerator = codeGenerator;
            _notificationService = notificationService;
            _recipientService = recipientService;
            _settingService = settingService;
            _distanceService = distanceService;
            _transitEstimator = transitEstimator;
            _geoService = geoService;
            _transitOptions = transitOptions.Value;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<UnplannedHorseModel>> GetUnplannedHorsesAsync(string? transportMode, int? bookingId)
        {
            var query = _bookingHorseRepo.GetQueryable().AsNoTracking()
                .Where(bh => bh.Status != BookingHorseStatuses.Cancelled
                    && bh.Status != BookingHorseStatuses.Deceased
                    && bh.Booking!.Status == BookingStatuses.Approved
                    // Ngựa trên chuyến đã hủy, hoặc rời chuyến để chờ hồi phục (Offloaded + WaitRecover), được coi là chưa xếp;
                    // đã giao, chết, rời chuyến với hướng xử lý khác hoặc chưa có hướng xử lý thì không xếp lại (R-12)
                    && !bh.TripHorses.Any(th => th.Trip!.OverallStatus != TripStatuses.Cancelled
                        && (TripHorseStatuses.Holding.Contains(th.Status)
                            || (th.Status == TripHorseStatuses.Offloaded && th.OffloadOutcome != OffloadOutcomes.WaitRecover))));

            if (!string.IsNullOrWhiteSpace(transportMode))
            {
                ValidateMode(transportMode);
                query = query.Where(bh => bh.Booking!.TransportMode == transportMode);
            }

            if (bookingId.HasValue)
            {
                query = query.Where(bh => bh.BookingId == bookingId.Value);
            }

            // Đơn có khoảng pickup sớm nhất lên đầu
            return await query
                .OrderBy(bh => bh.Booking!.PickupWindowStart)
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
                    TransitCountryCodes = bh.Booking.TransitCountries.OrderBy(t => t.SequenceOrder).Select(t => t.CountryCode).ToList(),
                    PickupWindowStart = bh.Booking.PickupWindowStart,
                    PickupWindowEnd = bh.Booking.PickupWindowEnd,
                    DeliveryDeadline = bh.Booking.DeliveryDeadline,
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
