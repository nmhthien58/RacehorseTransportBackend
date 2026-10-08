using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    // Flow 5. Vòng đời sự cố:
    //   Reported -> PlanProposed (Coordinator đề xuất) -> Approved (Manager duyệt) -> Resolved
    //                    ^------- Rejected (Manager từ chối, Coordinator đề xuất lại)
    //   Nắn tuyến chỉ làm được khi phương án đã Approved; chuyến chuyển EmergencyRerouting cho tới khi sự cố Resolved.
    public class IncidentService : IIncidentService
    {
        // Chuyến đang trên đường (kể cả lúc đang nắn tuyến vì sự cố)
        private static readonly string[] OnTheRoadStatuses = { TripStatuses.InTransit, TripStatuses.EmergencyRerouting };

        // Mốc thêm khi nắn tuyến chỉ là điểm dừng hoặc cửa khẩu; điểm đi và điểm giao không đổi
        private static readonly string[] RerouteCheckpointTypes = { CheckpointTypes.RestStop, CheckpointTypes.BorderGate };

        // Vùng số tạm khi đánh lại thứ tự mốc (xem ResequenceAsync)
        private const int ResequenceOffset = 100000;

        private readonly IRepository<TripIncident> _incidentRepo;
        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IRepository<RouteCheckpoint> _checkpointRepo;
        private readonly ITripService _tripService;
        private readonly ITrackingService _trackingService;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public IncidentService(
            IRepository<TripIncident> incidentRepo,
            IRepository<Trip> tripRepo,
            IRepository<TripHorse> tripHorseRepo,
            IRepository<RouteCheckpoint> checkpointRepo,
            ITripService tripService,
            ITrackingService trackingService,
            ICodeGenerator codeGenerator,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _incidentRepo = incidentRepo;
            _tripRepo = tripRepo;
            _tripHorseRepo = tripHorseRepo;
            _checkpointRepo = checkpointRepo;
            _tripService = tripService;
            _trackingService = trackingService;
            _codeGenerator = codeGenerator;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        // =============================================================================================
        // Xem
        // =============================================================================================

        public async Task<PagedBusinessResult<IncidentModel>> GetIncidentsAsync(IncidentFilterOptions options, CurrentUser currentUser)
        {
            var query = ApplyVisibility(QueryIncidents(), currentUser);

            // Filtering
            if (!string.IsNullOrWhiteSpace(options.Status))
            {
                if (!IncidentStatuses.All.Contains(options.Status))
                {
                    throw new BusinessException($"Status '{options.Status}' is invalid. Allowed values: {string.Join(", ", IncidentStatuses.All)}.");
                }
                query = query.Where(i => i.Status == options.Status);
            }

            if (!string.IsNullOrWhiteSpace(options.Severity))
            {
                if (!IncidentSeverities.All.Contains(options.Severity))
                {
                    throw new BusinessException($"Severity '{options.Severity}' is invalid. Allowed values: {string.Join(", ", IncidentSeverities.All)}.");
                }
                query = query.Where(i => i.Severity == options.Severity);
            }

            if (options.TripId.HasValue)
            {
                query = query.Where(i => i.TripId == options.TripId.Value);
            }

            // Searching
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                string search = options.Search.Trim().ToLower();
                query = query.Where(i => i.IncidentCode.ToLower().Contains(search)
                    || i.Location.ToLower().Contains(search)
                    || i.Description.ToLower().Contains(search));
            }

            // Sorting: mặc định sự cố mới nhất lên đầu
            query = query.ApplySort(string.IsNullOrWhiteSpace(options.Sort) ? "-reportedAt" : options.Sort, nameof(TripIncident.IncidentId));

            // Paging
            return await query.ToPagedResultAsync(options, i => i.ToBusinessModel());
        }

        public async Task<IReadOnlyList<IncidentModel>> GetTripIncidentsAsync(int tripId, CurrentUser currentUser)
        {
            // Dùng lại quy tắc "ai được xem chuyến" của TripService (404 / 403)
            await _tripService.GetTripByIdAsync(tripId, currentUser);

            var incidents = await QueryIncidents()
                .Where(i => i.TripId == tripId)
                .OrderByDescending(i => i.ReportedAt)
                .ThenByDescending(i => i.IncidentId)
                .ToListAsync();

            return incidents.Select(i => i.ToBusinessModel()).ToList();
        }

        public async Task<IncidentModel> GetIncidentByIdAsync(int id, CurrentUser currentUser)
        {
            var incident = await QueryIncidents().FirstOrDefaultAsync(i => i.IncidentId == id);
            if (incident == null)
            {
                throw new NotFoundException($"Incident with ID {id} was not found.");
            }

            bool canView = await ApplyVisibility(_incidentRepo.GetQueryable(), currentUser).AnyAsync(i => i.IncidentId == id);
            if (!canView)
            {
                throw new ForbiddenException("You cannot view this incident.");
            }

            return incident.ToBusinessModel();
        }

        // =============================================================================================
        // Báo sự cố
        // =============================================================================================

        public async Task<IncidentModel> ReportIncidentAsync(int tripId, ReportIncidentModel model, CurrentUser currentUser)
        {
            var trip = await _tripRepo.GetQueryable()
                .Include(t => t.Crew)
                .Include(t => t.TripHorses)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null)
            {
                throw new NotFoundException($"Trip with ID {tripId} was not found.");
            }

            if (currentUser.Role != UserRoles.Admin && !trip.Crew.Any(c => c.UserId == currentUser.UserId))
            {
                throw new ForbiddenException("Only the crew assigned to this trip can report an incident.");
            }

            if (!OnTheRoadStatuses.Contains(trip.OverallStatus))
            {
                throw new BusinessException($"Incidents can only be reported while the trip is on the road (current status: {trip.OverallStatus}).");
            }

            if (!IncidentTypes.All.Contains(model.IncidentType))
            {
                throw new BusinessException($"IncidentType '{model.IncidentType}' is invalid. Allowed values: {string.Join(", ", IncidentTypes.All)}.");
            }
            if (!IncidentSeverities.All.Contains(model.Severity))
            {
                throw new BusinessException($"Severity '{model.Severity}' is invalid. Allowed values: {string.Join(", ", IncidentSeverities.All)}.");
            }
            if (model.AffectedTripHorseId.HasValue && !trip.TripHorses.Any(th => th.TripHorseId == model.AffectedTripHorseId.Value))
            {
                throw new BusinessException($"Trip horse with ID {model.AffectedTripHorseId} is not on trip {trip.TripCode}.");
            }

            var customerIds = await GetCustomerIdsAsync(tripId);

            var incident = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var created = new TripIncident
                {
                    IncidentCode = await _codeGenerator.NextAsync(CodePrefixes.Incident),
                    TripId = tripId,
                    ReportedByUserId = currentUser.UserId,
                    IncidentType = model.IncidentType,
                    Severity = model.Severity,
                    Location = model.Location,
                    Description = model.Description,
                    AffectedTripHorseId = model.AffectedTripHorseId,
                    Status = IncidentStatuses.Reported
                };
                await _incidentRepo.AddAsync(created);

                string title = $"Incident {created.IncidentCode} on trip {trip.TripCode}";
                string message = $"{created.Severity} {created.IncidentType} at {created.Location}: {created.Description}";

                // Người lập chuyến phụ trách xử lý; chuyến không có người lập thì báo mọi Coordinator
                if (trip.PlannedByUserId.HasValue)
                {
                    await _notificationService.SendAsync(
                        trip.PlannedByUserId.Value, title, message, NotificationReferenceTypes.Incident, created.IncidentId);
                }
                else
                {
                    await _notificationService.SendToRoleAsync(
                        UserRoles.FleetCoordinator, title, message, NotificationReferenceTypes.Incident, created.IncidentId);
                }

                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager, title, message, NotificationReferenceTypes.Incident, created.IncidentId);

                await _notificationService.SendToUsersAsync(
                    customerIds,
                    $"An incident was reported on trip {trip.TripCode}",
                    $"{created.IncidentType} at {created.Location}. Our team is working on it and will keep you updated.",
                    NotificationReferenceTypes.Incident,
                    created.IncidentId);

                return created;
            });

            await _trackingService.InvalidateTripAsync(tripId);
            return await GetIncidentByIdAsync(incident.IncidentId, currentUser);
        }

        // =============================================================================================
        // Phương án xử lý: đề xuất -> duyệt / từ chối
        // =============================================================================================

        public async Task<IncidentModel> ProposePlanAsync(int incidentId, ProposeIncidentPlanModel model, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);

            if (incident.Status != IncidentStatuses.Reported && incident.Status != IncidentStatuses.Rejected)
            {
                throw new BusinessException($"A plan can only be proposed for a reported incident or after a rejection (current status: {incident.Status}).");
            }

            var trip = incident.Trip!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                incident.ProposedAction = model.ProposedAction;
                incident.RevisedRouteNotes = model.RevisedRouteNotes;
                incident.AdditionalCost = model.AdditionalCost;
                incident.Status = IncidentStatuses.PlanProposed;
                await _incidentRepo.UpdateAsync(incident);

                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Plan for incident {incident.IncidentCode} needs approval",
                    $"Trip {trip.TripCode}: {incident.ProposedAction} (additional cost {incident.AdditionalCost:0.##}).",
                    NotificationReferenceTypes.Incident,
                    incident.IncidentId);
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await GetIncidentByIdAsync(incidentId, currentUser);
        }

        public async Task<IncidentModel> ApprovePlanAsync(int incidentId, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            EnsurePlanProposed(incident);

            var trip = incident.Trip!;
            var customerIds = await GetCustomerIdsAsync(trip.TripId);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                incident.Status = IncidentStatuses.Approved;
                incident.RejectionReason = null;
                incident.ApprovedByUserId = currentUser.UserId;
                incident.ApprovedAt = DateTimeOffset.UtcNow;
                await _incidentRepo.UpdateAsync(incident);

                await _notificationService.SendToUsersAsync(
                    WithPlanner(CrewIds(trip), trip),
                    $"Plan for incident {incident.IncidentCode} was approved",
                    $"Trip {trip.TripCode}: {incident.ProposedAction}",
                    NotificationReferenceTypes.Incident,
                    incident.IncidentId);

                await _notificationService.SendToUsersAsync(
                    customerIds,
                    $"Update on the incident on trip {trip.TripCode}",
                    $"A response plan has been approved: {incident.ProposedAction}",
                    NotificationReferenceTypes.Incident,
                    incident.IncidentId);
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await GetIncidentByIdAsync(incidentId, currentUser);
        }

        // Lý do từ chối lưu vào sự cố và gửi cho Coordinator qua thông báo; Coordinator sửa rồi đề xuất lại
        public async Task<IncidentModel> RejectPlanAsync(int incidentId, string reason, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            EnsurePlanProposed(incident);

            var trip = incident.Trip!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                incident.Status = IncidentStatuses.Rejected;
                incident.RejectionReason = reason.Trim();
                await _incidentRepo.UpdateAsync(incident);

                string title = $"Plan for incident {incident.IncidentCode} was rejected";
                string message = $"Trip {trip.TripCode}. Reason: {incident.RejectionReason} Please propose a new plan.";

                if (trip.PlannedByUserId.HasValue)
                {
                    await _notificationService.SendAsync(
                        trip.PlannedByUserId.Value, title, message, NotificationReferenceTypes.Incident, incident.IncidentId);
                }
                else
                {
                    await _notificationService.SendToRoleAsync(
                        UserRoles.FleetCoordinator, title, message, NotificationReferenceTypes.Incident, incident.IncidentId);
                }
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await GetIncidentByIdAsync(incidentId, currentUser);
        }

        // =============================================================================================
        // Nắn tuyến và đóng sự cố
        // =============================================================================================

        // Bỏ các mốc chưa tới (IsActive = false) và chèn mốc mới vào lộ trình. Gọi lại được nhiều lần khi phương án còn Approved.
        public async Task<TripDetailModel> ApplyRerouteAsync(int incidentId, ApplyRerouteModel model, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            var trip = incident.Trip!;

            if (incident.Status != IncidentStatuses.Approved)
            {
                throw new BusinessException($"The route can only be changed after the plan is approved (current status: {incident.Status}).");
            }
            if (!OnTheRoadStatuses.Contains(trip.OverallStatus))
            {
                throw new BusinessException($"The route can only be changed while the trip is on the road (current status: {trip.OverallStatus}).");
            }

            var removeIds = model.DeactivateCheckpointIds.Distinct().ToList();
            if (removeIds.Count == 0 && model.NewCheckpoints.Count == 0)
            {
                throw new BusinessException("Nothing to change: provide checkpoints to remove or new checkpoints to add.");
            }

            var route = trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();

            // Mốc bị bỏ: phải thuộc lộ trình hiện tại, chưa tới, không phải điểm đi / điểm giao
            var removed = new List<RouteCheckpoint>();
            foreach (int checkpointId in removeIds)
            {
                var checkpoint = route.FirstOrDefault(c => c.CheckpointId == checkpointId);
                if (checkpoint == null)
                {
                    throw new BusinessException($"Checkpoint with ID {checkpointId} is not on the current route of trip {trip.TripCode}.");
                }
                if (checkpoint.CheckpointType == CheckpointTypes.Origin || checkpoint.CheckpointType == CheckpointTypes.Destination)
                {
                    throw new BusinessException($"'{checkpoint.CheckpointName}' is the {checkpoint.CheckpointType.ToLower()} and cannot be removed from the route.");
                }
                if (checkpoint.Status != CheckpointStatuses.Pending)
                {
                    throw new BusinessException($"'{checkpoint.CheckpointName}' has already been reached (status: {checkpoint.Status}) and cannot be removed.");
                }
                removed.Add(checkpoint);
            }

            // Mốc mới
            DateTimeOffset? previousTime = null;
            foreach (var input in model.NewCheckpoints)
            {
                if (!RerouteCheckpointTypes.Contains(input.CheckpointType))
                {
                    throw new BusinessException($"CheckpointType '{input.CheckpointType}' is invalid for a reroute. Allowed values: {string.Join(", ", RerouteCheckpointTypes)}.");
                }
                if (previousTime.HasValue && input.PlannedTime <= previousTime.Value)
                {
                    throw new BusinessException("New checkpoints must be listed in travel order with increasing PlannedTime.");
                }
                previousTime = input.PlannedTime;
            }

            // Vị trí chèn: sau mốc được chỉ định, mặc định là mốc gần nhất đoàn xe đã tới
            var lastReached = route.LastOrDefault(c => c.Status != CheckpointStatuses.Pending) ?? route.First();
            var anchor = lastReached;
            if (model.InsertAfterCheckpointId.HasValue)
            {
                anchor = route.FirstOrDefault(c => c.CheckpointId == model.InsertAfterCheckpointId.Value);
                if (anchor == null)
                {
                    throw new BusinessException($"Checkpoint with ID {model.InsertAfterCheckpointId} is not on the current route of trip {trip.TripCode}.");
                }
                if (removed.Contains(anchor))
                {
                    throw new BusinessException($"'{anchor.CheckpointName}' is being removed, so new checkpoints cannot be inserted after it.");
                }
                if (anchor.CheckpointType == CheckpointTypes.Destination)
                {
                    throw new BusinessException("New checkpoints cannot be inserted after the destination.");
                }
                if (anchor.SequenceOrder < lastReached.SequenceOrder)
                {
                    throw new BusinessException($"New checkpoints cannot be inserted before '{lastReached.CheckpointName}', which the trip has already reached.");
                }
            }

            var added = model.NewCheckpoints.Select(input => new RouteCheckpoint
            {
                TripId = trip.TripId,
                CheckpointName = input.CheckpointName,
                CheckpointType = input.CheckpointType,
                Address = input.Address,
                PlannedTime = input.PlannedTime,
                MandatoryRestMinutes = input.MandatoryRestMinutes,
                Status = CheckpointStatuses.Pending,
                IsActive = true
            }).ToList();

            var newRoute = route.Except(removed).ToList();
            newRoute.InsertRange(newRoute.IndexOf(anchor) + 1, added);

            var customerIds = await GetCustomerIdsAsync(trip.TripId);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var checkpoint in removed)
                {
                    checkpoint.IsActive = false;
                    checkpoint.Status = CheckpointStatuses.Cancelled;
                }

                await ResequenceAsync(newRoute, added);

                trip.OverallStatus = TripStatuses.EmergencyRerouting;
                await _tripRepo.UpdateAsync(trip);

                string change = DescribeRouteChange(removed, added);

                await _notificationService.SendToUsersAsync(
                    CrewIds(trip),
                    $"Route changed for trip {trip.TripCode}",
                    $"Incident {incident.IncidentCode}: {change} Check the updated checkpoint list before moving on.",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);

                await _notificationService.SendToUsersAsync(
                    customerIds,
                    $"Trip {trip.TripCode} is being rerouted",
                    $"Because of an incident the route has changed. {change}",
                    NotificationReferenceTypes.Incident,
                    incident.IncidentId);
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await _tripService.GetTripByIdAsync(trip.TripId, currentUser);
        }

        public async Task<IncidentModel> ResolveIncidentAsync(int incidentId, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            var trip = incident.Trip!;

            // Phải có phương án đã duyệt: sự cố Resolved được tính là bất khả kháng khi chốt KPI (T-26)
            if (incident.Status != IncidentStatuses.Approved)
            {
                throw new BusinessException($"Only an incident with an approved plan can be resolved (current status: {incident.Status}).");
            }

            var customerIds = await GetCustomerIdsAsync(trip.TripId);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                incident.Status = IncidentStatuses.Resolved;
                incident.ResolvedAt = DateTimeOffset.UtcNow;
                await _incidentRepo.UpdateAsync(incident);

                // Chuyến trở lại hành trình bình thường khi không còn sự cố nào khác đang xử lý theo phương án đã duyệt
                bool otherInProgress = await _incidentRepo.ExistsAsync(i =>
                    i.TripId == trip.TripId && i.IncidentId != incident.IncidentId && i.Status == IncidentStatuses.Approved);

                if (trip.OverallStatus == TripStatuses.EmergencyRerouting && !otherInProgress)
                {
                    trip.OverallStatus = TripStatuses.InTransit;
                    await _tripRepo.UpdateAsync(trip);
                }

                await _notificationService.SendToUsersAsync(
                    CrewIds(trip).Concat(customerIds),
                    $"Incident {incident.IncidentCode} on trip {trip.TripCode} is resolved",
                    "The incident has been handled and the trip continues.",
                    NotificationReferenceTypes.Incident,
                    incident.IncidentId);
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await GetIncidentByIdAsync(incidentId, currentUser);
        }

        // ---------------------------------------------------------------------------------------------

        private IQueryable<TripIncident> QueryIncidents()
        {
            return _incidentRepo.GetQueryable()
                .AsNoTracking()
                .Include(i => i.Trip)
                .Include(i => i.ReportedBy)
                .Include(i => i.ApprovedBy)
                .Include(i => i.AffectedTripHorse!).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Horse);
        }

        // Khách: sự cố trên chuyến có ngựa của mình. Tài xế / áp tải: chuyến mình có tên trong đoàn. Các role vận hành: tất cả.
        private static IQueryable<TripIncident> ApplyVisibility(IQueryable<TripIncident> query, CurrentUser currentUser)
        {
            if (currentUser.IsCustomer)
            {
                return query.Where(i => i.Trip!.TripHorses.Any(th => th.BookingHorse!.Booking!.CustomerUserId == currentUser.UserId));
            }
            if (currentUser.Role == UserRoles.DriverEscort)
            {
                return query.Where(i => i.Trip!.Crew.Any(c => c.UserId == currentUser.UserId));
            }
            return query;
        }

        private async Task<TripIncident> GetTrackedIncidentAsync(int incidentId)
        {
            var incident = await _incidentRepo.GetQueryable()
                .Include(i => i.Trip!).ThenInclude(t => t.Crew)
                .Include(i => i.Trip!).ThenInclude(t => t.Checkpoints)
                .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

            if (incident == null)
            {
                throw new NotFoundException($"Incident with ID {incidentId} was not found.");
            }
            return incident;
        }

        private static void EnsurePlanProposed(TripIncident incident)
        {
            if (incident.Status != IncidentStatuses.PlanProposed)
            {
                throw new BusinessException($"Only a proposed plan can be reviewed (current status: {incident.Status}).");
            }
        }

        // DB cấm hai mốc đang hiệu lực của một chuyến trùng thứ tự, và kiểm tra ngay từng câu lệnh,
        // nên đẩy tất cả ra vùng số tạm (lưu luôn các mốc mới) rồi mới gán thứ tự thật.
        private async Task ResequenceAsync(List<RouteCheckpoint> ordered, List<RouteCheckpoint> added)
        {
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].SequenceOrder = ResequenceOffset + i;
            }
            await _checkpointRepo.AddRangeAsync(added);
            await _checkpointRepo.SaveChangesAsync();

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].SequenceOrder = i + 1;
            }
            await _checkpointRepo.SaveChangesAsync();
        }

        private static string DescribeRouteChange(List<RouteCheckpoint> removed, List<RouteCheckpoint> added)
        {
            var parts = new List<string>();
            if (removed.Count > 0)
            {
                parts.Add($"Removed: {string.Join(", ", removed.Select(c => c.CheckpointName))}.");
            }
            if (added.Count > 0)
            {
                parts.Add($"Added: {string.Join(", ", added.Select(c => c.CheckpointName))}.");
            }
            return string.Join(" ", parts);
        }

        private async Task<List<int>> GetCustomerIdsAsync(int tripId)
        {
            return await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId)
                .Select(th => th.BookingHorse!.Booking!.CustomerUserId)
                .Distinct()
                .ToListAsync();
        }

        private static List<int> CrewIds(Trip trip)
        {
            return trip.Crew.Select(c => c.UserId).Distinct().ToList();
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
