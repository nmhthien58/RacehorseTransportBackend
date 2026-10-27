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
    // R-10: tuyến dự kiến của đơn.
    //   Đơn PreApproved -> Coordinator lập tuyến trên bản đồ (điểm đầu / cuối lấy từ đơn, thêm mốc nghỉ / cửa khẩu)
    //   -> hệ thống tính từng chặng, nước quá cảnh, cảnh báo đổi nước thiếu cửa khẩu -> trình (chặn khi còn cảnh báo)
    //   -> Manager duyệt: tuyến Approved (bản duyệt cũ -> Superseded), đơn RoutePlanned, nước quá cảnh của đơn theo tuyến.
    public class RoutePlanService : IRoutePlanService
    {
        // Đơn được lập tuyến: vừa duyệt sơ bộ, hoặc đã có tuyến duyệt mà cần phiên bản mới
        // Quoted: khách chưa chấp nhận báo giá, công ty có thể lập lại tuyến -> duyệt bản mới thì báo giá được tính lại (hạn mới)
        private static readonly string[] PlannableBookingStatuses = { BookingStatuses.PreApproved, BookingStatuses.RoutePlanned, BookingStatuses.Quoted };

        // Dịch vụ bản đồ nhận tối đa 25 điểm (đầu + cuối + 23 mốc)
        private const int MaxIntermediateStops = 23;

        private readonly IRepository<RoutePlan> _planRepo;
        private readonly IRepository<RoutePlanStop> _stopRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IBookingService _bookingService;
        private readonly IGeoService _geoService;
        private readonly ITransitTimeEstimator _transitEstimator;
        private readonly IBookingQuoteService _quoteService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public RoutePlanService(
            IRepository<RoutePlan> planRepo,
            IRepository<RoutePlanStop> stopRepo,
            IRepository<BookingRequest> bookingRepo,
            IBookingService bookingService,
            IGeoService geoService,
            ITransitTimeEstimator transitEstimator,
            IBookingQuoteService quoteService,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _planRepo = planRepo;
            _stopRepo = stopRepo;
            _bookingRepo = bookingRepo;
            _bookingService = bookingService;
            _geoService = geoService;
            _transitEstimator = transitEstimator;
            _quoteService = quoteService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        // =============================================================================================
        // Xem
        // =============================================================================================

        public async Task<IReadOnlyList<RoutePlanModel>> GetForBookingAsync(int bookingId, CurrentUser currentUser)
        {
            // Dùng lại quy tắc "ai được xem đơn" của BookingService (404 / 403)
            await _bookingService.GetBookingByIdAsync(bookingId, currentUser);

            var query = Query().Where(p => p.BookingId == bookingId);
            if (currentUser.IsCustomer)
            {
                query = query.Where(p => p.Status == RoutePlanStatuses.Approved);
            }

            var plans = await query.OrderByDescending(p => p.Version).ToListAsync();
            return plans.Select(ToModel).ToList();
        }

        public async Task<RoutePlanModel> GetByIdAsync(int routePlanId, CurrentUser currentUser)
        {
            var plan = await Query().FirstOrDefaultAsync(p => p.RoutePlanId == routePlanId)
                ?? throw new NotFoundException($"Route plan with ID {routePlanId} was not found.");

            await _bookingService.GetBookingByIdAsync(plan.BookingId, currentUser);
            if (currentUser.IsCustomer && plan.Status != RoutePlanStatuses.Approved)
            {
                throw new ForbiddenException("You can only view the approved route of your booking.");
            }
            return ToModel(plan);
        }

        // =============================================================================================
        // Lập / sửa / trình
        // =============================================================================================

        public async Task<RoutePlanModel> PreviewAsync(int bookingId, SaveRoutePlanModel model, CurrentUser currentUser)
        {
            var booking = await GetBookingAsync(bookingId);
            var computed = await ComputeAsync(booking, model);
            computed.Model.BookingId = booking.BookingId;
            computed.Model.BookingCode = booking.BookingCode;
            computed.Model.Status = RoutePlanStatuses.Draft;
            return computed.Model;
        }

        public async Task<RoutePlanModel> CreateAsync(int bookingId, SaveRoutePlanModel model, CurrentUser currentUser)
        {
            var booking = await GetBookingAsync(bookingId);
            if (!PlannableBookingStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"A route can only be planned after the booking passes the first review (current status: {booking.Status}).");
            }

            var open = await _planRepo.GetQueryable()
                .Where(p => p.BookingId == bookingId && (RoutePlanStatuses.Editable.Contains(p.Status) || p.Status == RoutePlanStatuses.Submitted))
                .Select(p => new { p.RoutePlanId, p.Version, p.Status })
                .FirstOrDefaultAsync();
            if (open != null)
            {
                throw new BusinessException($"Booking {booking.BookingCode} already has route version {open.Version} ({open.Status}). Edit or review that version instead of starting a new one.");
            }

            var computed = await ComputeAsync(booking, model);
            int version = (await _planRepo.GetQueryable().Where(p => p.BookingId == bookingId).MaxAsync(p => (int?)p.Version) ?? 0) + 1;

            var plan = new RoutePlan
            {
                BookingId = bookingId,
                Version = version,
                Status = RoutePlanStatuses.Draft,
                PlannedByUserId = currentUser.UserId
            };
            Apply(plan, computed, model.Notes);
            await _planRepo.AddAsync(plan);

            return await LoadModelAsync(plan.RoutePlanId, computed.Model.Warnings);
        }

        public async Task<RoutePlanModel> UpdateAsync(int routePlanId, SaveRoutePlanModel model, CurrentUser currentUser)
        {
            var plan = await GetTrackedPlanAsync(routePlanId);
            if (!RoutePlanStatuses.Editable.Contains(plan.Status))
            {
                throw new BusinessException($"Only a draft or rejected route can be edited (current status: {plan.Status}).");
            }

            var booking = await GetBookingAsync(plan.BookingId);
            var computed = await ComputeAsync(booking, model);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Thay toàn bộ danh sách mốc (khóa ngoại để Restrict nên xóa mốc cũ tường minh)
                await _stopRepo.DeleteRangeAsync(plan.Stops.ToList());
                plan.Stops.Clear();

                Apply(plan, computed, model.Notes);
                plan.Status = RoutePlanStatuses.Draft;
                plan.PlannedByUserId = currentUser.UserId;
                plan.UpdatedAt = DateTimeOffset.UtcNow;
                await _planRepo.SaveChangesAsync();
            });

            return await LoadModelAsync(plan.RoutePlanId, computed.Model.Warnings);
        }

        public async Task<RoutePlanModel> SubmitAsync(int routePlanId, CurrentUser currentUser)
        {
            var plan = await GetTrackedPlanAsync(routePlanId);
            if (!RoutePlanStatuses.Editable.Contains(plan.Status))
            {
                throw new BusinessException($"Only a draft or rejected route can be submitted (current status: {plan.Status}).");
            }

            var booking = await GetBookingAsync(plan.BookingId);

            // Tính lại cảnh báo trên tuyến đã lưu (kết quả bản đồ lấy từ bộ nhớ đệm nên nhanh)
            var computed = await ComputeAsync(booking, ToSaveModel(plan));
            if (computed.Model.Warnings.Count > 0)
            {
                throw new BusinessException($"The route cannot be submitted yet: {string.Join(" ", computed.Model.Warnings.Select(w => w.Message))}");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                plan.Status = RoutePlanStatuses.Submitted;
                plan.UpdatedAt = DateTimeOffset.UtcNow;
                await _planRepo.UpdateAsync(plan);

                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Route for booking {booking.BookingCode} needs approval",
                    $"Version {plan.Version}: {plan.TotalDistanceKm:0.#} km, about {plan.EstimatedHours:0.#} hours, transit: {DescribeTransit(plan)}.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            return await LoadModelAsync(plan.RoutePlanId);
        }

        // =============================================================================================
        // Duyệt / từ chối
        // =============================================================================================

        public async Task<RoutePlanModel> ApproveAsync(int routePlanId, CurrentUser currentUser)
        {
            var plan = await GetTrackedPlanAsync(routePlanId);
            EnsureSubmitted(plan);

            var booking = await _bookingRepo.GetQueryable()
                .Include(b => b.TransitCountries)
                .FirstAsync(b => b.BookingId == plan.BookingId);
            if (!PlannableBookingStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"The route of booking {booking.BookingCode} cannot be approved in status {booking.Status}.");
            }

            var previous = await _planRepo.GetQueryable()
                .Where(p => p.BookingId == plan.BookingId && p.Status == RoutePlanStatuses.Approved && p.RoutePlanId != plan.RoutePlanId)
                .ToListAsync();
            var transit = ParseCodes(plan.TransitCountryCodes);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Chỉ một tuyến Approved hiệu lực: hạ bản cũ trước (chỉ mục duy nhất kiểm tra ngay)
                foreach (var old in previous)
                {
                    old.Status = RoutePlanStatuses.Superseded;
                }
                await _planRepo.SaveChangesAsync();

                plan.Status = RoutePlanStatuses.Approved;
                plan.ReviewedByUserId = currentUser.UserId;
                plan.ReviewedAt = DateTimeOffset.UtcNow;
                plan.RejectionReason = null;
                plan.UpdatedAt = DateTimeOffset.UtcNow;

                // Đơn theo tuyến đã duyệt: nước quá cảnh, quãng đường, thời gian vận chuyển
                foreach (var row in booking.TransitCountries.ToList())
                {
                    booking.TransitCountries.Remove(row);
                }
                await _bookingRepo.SaveChangesAsync();
                for (int i = 0; i < transit.Count; i++)
                {
                    booking.TransitCountries.Add(new BookingTransitCountry { SequenceOrder = i + 1, CountryCode = transit[i] });
                }
                booking.DistanceKm = (int)Math.Ceiling(plan.TotalDistanceKm);
                booking.EstimatedTransitHours = plan.EstimatedHours;
                booking.Status = BookingStatuses.RoutePlanned;
                await _bookingRepo.SaveChangesAsync();

                // R-10: báo giá chốt theo tuyến vừa duyệt, đơn -> Quoted, email cho khách
                await _quoteService.IssueFinalQuoteAsync(booking.BookingId, plan.RoutePlanId);

                await _notificationService.SendAsync(
                    plan.PlannedByUserId,
                    $"Route for booking {booking.BookingCode} was approved",
                    $"Version {plan.Version} is now the approved route.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            return await LoadModelAsync(plan.RoutePlanId);
        }

        public async Task<RoutePlanModel> RejectAsync(int routePlanId, string reason, CurrentUser currentUser)
        {
            var plan = await GetTrackedPlanAsync(routePlanId);
            EnsureSubmitted(plan);
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new BusinessException("Reason is required to reject a route.");
            }

            var booking = await GetBookingAsync(plan.BookingId);
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                plan.Status = RoutePlanStatuses.Rejected;
                plan.RejectionReason = reason.Trim();
                plan.ReviewedByUserId = currentUser.UserId;
                plan.ReviewedAt = DateTimeOffset.UtcNow;
                plan.UpdatedAt = DateTimeOffset.UtcNow;
                await _planRepo.UpdateAsync(plan);

                await _notificationService.SendAsync(
                    plan.PlannedByUserId,
                    $"Route for booking {booking.BookingCode} was rejected",
                    $"Version {plan.Version}. Reason: {plan.RejectionReason} Please edit the route and submit it again.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            return await LoadModelAsync(plan.RoutePlanId);
        }

        // =============================================================================================
        // Tính tuyến
        // =============================================================================================

        private sealed record ComputedRoute(RoutePlanModel Model, List<RoutePlanStop> Stops, List<string> Transit, List<GeoPointModel> Polyline);

        private async Task<ComputedRoute> ComputeAsync(BookingRequest booking, SaveRoutePlanModel model)
        {
            if (booking.TransportMode != TransportModes.Ground)
            {
                throw new BusinessException("Route plans are only made for ground transport.");
            }
            if (model.Stops.Count > MaxIntermediateStops)
            {
                throw new BusinessException($"A route can have at most {MaxIntermediateStops} stops between the pickup and the drop-off.");
            }

            decimal? originLat = booking.PickupLat ?? model.OriginLat;
            decimal? originLng = booking.PickupLng ?? model.OriginLng;
            decimal? destLat = booking.DropoffLat ?? model.DestinationLat;
            decimal? destLng = booking.DropoffLng ?? model.DestinationLng;
            if (originLat == null || originLng == null || destLat == null || destLng == null)
            {
                throw new BusinessException($"Booking {booking.BookingCode} has no map coordinates for its pickup / drop-off. Send OriginLat, OriginLng, DestinationLat and DestinationLng.");
            }

            var stops = new List<RoutePlanStopInputModel>
            {
                new() { Name = "Pickup", StopType = RouteStopTypes.Origin, Address = booking.PickupAddress, Lat = originLat.Value, Lng = originLng.Value, CountryCode = booking.PickupCountryCode }
            };
            foreach (var input in model.Stops)
            {
                if (!RouteStopTypes.Intermediate.Contains(input.StopType))
                {
                    throw new BusinessException($"StopType '{input.StopType}' is invalid for a stop. Allowed values: {string.Join(", ", RouteStopTypes.Intermediate)}.");
                }
                if (string.IsNullOrWhiteSpace(input.Name))
                {
                    throw new BusinessException("Every stop needs a name.");
                }
                if (input.RestMinutes < 0)
                {
                    throw new BusinessException("RestMinutes cannot be negative.");
                }
                stops.Add(input);
            }
            stops.Add(new RoutePlanStopInputModel { Name = "Drop-off", StopType = RouteStopTypes.Destination, Address = booking.DropoffAddress, Lat = destLat.Value, Lng = destLng.Value, CountryCode = booking.DropoffCountryCode });

            var points = stops.Select(s => new GeoPointModel((double)s.Lat, (double)s.Lng, string.IsNullOrWhiteSpace(s.CountryCode) ? null : s.CountryCode.Trim().ToUpperInvariant())).ToList();
            var route = await _geoService.RouteAsync(points, includeCountries: true);

            // Nước dọc từng chặng để phát hiện đổi nước thiếu cửa khẩu
            var legCountries = new List<IReadOnlyList<string>>();
            foreach (var leg in route.Legs)
            {
                legCountries.Add(await _geoService.CountriesAlongAsync(leg.Polyline));
            }

            var names = stops.Select(s => s.Name.Trim()).ToList();
            var warnings = RoutePlanRules.BorderWarnings(stops.Select(s => s.StopType).ToList(), names, legCountries);
            var along = route.CountryCodes ?? new List<string>();
            var transit = RoutePlanRules.TransitCountries(along, booking.PickupCountryCode, booking.DropoffCountryCode);

            int km = Math.Max(1, (int)Math.Ceiling(route.TotalDistanceKm));
            var estimate = _transitEstimator.Estimate(new TransitEstimateInputModel
            {
                TransportMode = TransportModes.Ground,
                DistanceKm = km,
                BorderCrossings = RoutePlanRules.BorderCrossings(along)
            });

            var entities = stops.Select((s, i) => new RoutePlanStop
            {
                SequenceOrder = i + 1,
                StopName = names[i],
                StopType = s.StopType,
                Address = string.IsNullOrWhiteSpace(s.Address) ? names[i] : s.Address.Trim(),
                Latitude = Math.Round(s.Lat, 6),
                Longitude = Math.Round(s.Lng, 6),
                CountryCode = points[i].CountryCode,
                DistanceKmFromPrevious = i == 0 ? null : route.Legs[i - 1].DistanceKm,
                DurationHoursFromPrevious = i == 0 ? null : route.Legs[i - 1].DurationHours,
                RestMinutes = s.RestMinutes
            }).ToList();

            var result = new RoutePlanModel
            {
                Stops = entities.Select(ToStopModel).ToList(),
                TransitCountryCodes = transit,
                TotalDistanceKm = route.TotalDistanceKm,
                EstimatedHours = estimate.TotalHours,
                IsEstimate = route.IsEstimate,
                Polyline = route.Polyline,
                Warnings = warnings,
                Notes = model.Notes
            };
            return new ComputedRoute(result, entities, transit, route.Polyline);
        }

        private static void Apply(RoutePlan plan, ComputedRoute computed, string? notes)
        {
            foreach (var stop in computed.Stops)
            {
                plan.Stops.Add(stop);
            }
            plan.TransitCountryCodes = JsonSerializer.Serialize(computed.Transit);
            plan.TotalDistanceKm = computed.Model.TotalDistanceKm;
            plan.EstimatedHours = computed.Model.EstimatedHours;
            plan.IsEstimate = computed.Model.IsEstimate;
            plan.Polyline = JsonSerializer.Serialize(computed.Polyline.Select(p => new[] { Math.Round(p.Lat, 6), Math.Round(p.Lng, 6) }));
            plan.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        }

        private static SaveRoutePlanModel ToSaveModel(RoutePlan plan)
        {
            var ordered = plan.Stops.OrderBy(s => s.SequenceOrder).ToList();
            return new SaveRoutePlanModel
            {
                OriginLat = ordered.First().Latitude,
                OriginLng = ordered.First().Longitude,
                DestinationLat = ordered.Last().Latitude,
                DestinationLng = ordered.Last().Longitude,
                Notes = plan.Notes,
                Stops = ordered.Skip(1).Take(ordered.Count - 2).Select(s => new RoutePlanStopInputModel
                {
                    Name = s.StopName,
                    StopType = s.StopType,
                    Address = s.Address,
                    Lat = s.Latitude,
                    Lng = s.Longitude,
                    CountryCode = s.CountryCode,
                    RestMinutes = s.RestMinutes
                }).ToList()
            };
        }

        // ---------------------------------------------------------------------------------------------

        private IQueryable<RoutePlan> Query()
        {
            return _planRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Booking)
                .Include(p => p.PlannedBy)
                .Include(p => p.ReviewedBy)
                .Include(p => p.Stops);
        }

        private async Task<RoutePlanModel> LoadModelAsync(int routePlanId, List<RoutePlanWarningModel>? warnings = null)
        {
            var model = ToModel(await Query().FirstAsync(p => p.RoutePlanId == routePlanId));
            model.Warnings = warnings ?? new List<RoutePlanWarningModel>();
            return model;
        }

        private async Task<RoutePlan> GetTrackedPlanAsync(int routePlanId)
        {
            return await _planRepo.GetQueryable()
                .Include(p => p.Stops)
                .FirstOrDefaultAsync(p => p.RoutePlanId == routePlanId)
                ?? throw new NotFoundException($"Route plan with ID {routePlanId} was not found.");
        }

        private async Task<BookingRequest> GetBookingAsync(int bookingId)
        {
            return await _bookingRepo.GetQueryable().AsNoTracking().FirstOrDefaultAsync(b => b.BookingId == bookingId)
                ?? throw new NotFoundException($"Booking with ID {bookingId} was not found.");
        }

        private static void EnsureSubmitted(RoutePlan plan)
        {
            if (plan.Status != RoutePlanStatuses.Submitted)
            {
                throw new BusinessException($"Only a submitted route can be reviewed (current status: {plan.Status}).");
            }
        }

        private static List<string> ParseCodes(string json) =>
            string.IsNullOrWhiteSpace(json) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

        private static string DescribeTransit(RoutePlan plan)
        {
            var codes = ParseCodes(plan.TransitCountryCodes);
            return codes.Count == 0 ? "none" : string.Join(" → ", codes);
        }

        private static RoutePlanStopModel ToStopModel(RoutePlanStop s) => new()
        {
            SequenceOrder = s.SequenceOrder,
            Name = s.StopName,
            StopType = s.StopType,
            Address = s.Address,
            Lat = s.Latitude,
            Lng = s.Longitude,
            CountryCode = s.CountryCode,
            DistanceKmFromPrevious = s.DistanceKmFromPrevious,
            DurationHoursFromPrevious = s.DurationHoursFromPrevious,
            RestMinutes = s.RestMinutes
        };

        private static RoutePlanModel ToModel(RoutePlan p)
        {
            var polyline = string.IsNullOrWhiteSpace(p.Polyline)
                ? new List<GeoPointModel>()
                : (JsonSerializer.Deserialize<List<double[]>>(p.Polyline) ?? new List<double[]>()).Select(x => new GeoPointModel(x[0], x[1])).ToList();

            return new RoutePlanModel
            {
                RoutePlanId = p.RoutePlanId,
                BookingId = p.BookingId,
                BookingCode = p.Booking?.BookingCode ?? string.Empty,
                Version = p.Version,
                Status = p.Status,
                Stops = p.Stops.OrderBy(s => s.SequenceOrder).Select(ToStopModel).ToList(),
                TransitCountryCodes = ParseCodes(p.TransitCountryCodes),
                TotalDistanceKm = p.TotalDistanceKm,
                EstimatedHours = p.EstimatedHours,
                IsEstimate = p.IsEstimate,
                Polyline = polyline,
                Notes = p.Notes,
                PlannedByUserId = p.PlannedByUserId,
                PlannedByName = p.PlannedBy?.FullName,
                ReviewedByName = p.ReviewedBy?.FullName,
                ReviewedAt = p.ReviewedAt,
                RejectionReason = p.RejectionReason,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }
    }
}
