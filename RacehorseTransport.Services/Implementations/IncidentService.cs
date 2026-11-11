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
    //   Phương án = loại hành động + chỉ đạo + (tùy chọn) thay đổi tuyến. Tuyến được kiểm tra lúc đề xuất và áp vào lộ trình
    //   khi Manager duyệt; sau đó Coordinator vẫn điều chỉnh tuyến thêm được. Tuyến đổi thì chuyến EmergencyRerouting tới khi Resolved.
    //   Ngựa rời chuyến chỉ qua phương án OffloadHorse (R-12): duyệt xong con ngựa mới đổi trạng thái theo hướng xử lý tiếp.
    //   Ngoại lệ: tài xế dỡ khẩn cấp lúc báo sự cố (EmergencyOffload), phương án vẫn phải được đề xuất và duyệt sau.
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
        private readonly IRepository<TransportAsset> _assetRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<TripCrew> _crewRepo;
        private readonly ITripService _tripService;
        private readonly ITrackingService _trackingService;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;
        private readonly IFileStorageService _fileStorage;
        private readonly IRepository<IncidentConsent> _consentRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IPaymentService _paymentService;
        private readonly IPricingService _pricingService;
        private readonly ISystemSettingService _settingService;
        private readonly IContractService _contractService;
        private readonly IClaimService _claimService;
        private readonly IRepository<IncidentDocument> _documentRepo;
        private readonly IUnitOfWork _unitOfWork;

        public IncidentService(
            IRepository<TripIncident> incidentRepo,
            IRepository<Trip> tripRepo,
            IRepository<TripHorse> tripHorseRepo,
            IRepository<RouteCheckpoint> checkpointRepo,
            IRepository<TransportAsset> assetRepo,
            IRepository<User> userRepo,
            IRepository<TripCrew> crewRepo,
            ITripService tripService,
            ITrackingService trackingService,
            ICodeGenerator codeGenerator,
            INotificationService notificationService,
            IFileStorageService fileStorage,
            IRepository<IncidentConsent> consentRepo,
            IRepository<BookingRequest> bookingRepo,
            IPaymentService paymentService,
            IPricingService pricingService,
            ISystemSettingService settingService,
            IContractService contractService,
            IClaimService claimService,
            IRepository<IncidentDocument> documentRepo,
            IUnitOfWork unitOfWork)
        {
            _incidentRepo = incidentRepo;
            _tripRepo = tripRepo;
            _tripHorseRepo = tripHorseRepo;
            _checkpointRepo = checkpointRepo;
            _assetRepo = assetRepo;
            _userRepo = userRepo;
            _crewRepo = crewRepo;
            _tripService = tripService;
            _trackingService = trackingService;
            _codeGenerator = codeGenerator;
            _notificationService = notificationService;
            _fileStorage = fileStorage;
            _consentRepo = consentRepo;
            _bookingRepo = bookingRepo;
            _paymentService = paymentService;
            _pricingService = pricingService;
            _settingService = settingService;
            _contractService = contractService;
            _claimService = claimService;
            _documentRepo = documentRepo;
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
            var page = await query.ToPagedResultAsync(options, i => i.ToBusinessModel());
            await ResolveVetReportUrlsAsync(page.Items);
            await FillDetourShareAsync(page.Items, currentUser);
            return page;
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

            var models = incidents.Select(i => i.ToBusinessModel()).ToList();
            await ResolveVetReportUrlsAsync(models);
            await FillDetourShareAsync(models, currentUser);
            return models;
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

            var model = incident.ToBusinessModel();
            await ResolveVetReportUrlsAsync(new[] { model });
            await FillDetourShareAsync(new[] { model }, currentUser);
            return model;
        }

        // =============================================================================================
        // Báo sự cố
        // =============================================================================================

        public async Task<IncidentModel> ReportIncidentAsync(int tripId, ReportIncidentModel model, CurrentUser currentUser)
        {
            var trip = await _tripRepo.GetQueryable()
                .Include(t => t.Crew)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Booking)
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

            // Bấm gửi lặp / gửi lại sau khi mạng chập chờn: trả lại sự cố vừa báo thay vì tạo bản thứ hai
            var duplicateId = await FindDuplicateReportAsync(tripId, model, currentUser);
            if (duplicateId.HasValue)
            {
                return await GetIncidentByIdAsync(duplicateId.Value, currentUser);
            }

            // Dỡ khẩn cấp (thú y bắt buộc dỡ ngay): con ngựa rời xe ngay, hướng xử lý tiếp chờ phương án được duyệt
            TripHorse? emergencyHorse = null;
            if (model.EmergencyOffload)
            {
                if (!model.AffectedTripHorseId.HasValue)
                {
                    throw new BusinessException("Choose the horse that is offloaded in an emergency (AffectedTripHorseId).");
                }
                emergencyHorse = trip.TripHorses.First(th => th.TripHorseId == model.AffectedTripHorseId.Value);
                if (!TripHorseStatuses.OnBoard.Contains(emergencyHorse.Status))
                {
                    throw new BusinessException($"{emergencyHorse.BookingHorse!.Horse!.Name} is not on the vehicle (status: {emergencyHorse.Status}), so it cannot be offloaded.");
                }
            }

            var customerIds = await GetCustomerIdsAsync(tripId);
            var bookingRecipients = await _tripHorseRepo.GetQueryable().GetBookingRecipientsAsync(tripId);

            int? duplicateInTransaction = null;
            var incident = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Hai request trùng tới cùng lúc: request sau chờ request trước commit rồi mới kiểm tra lại
                await _unitOfWork.LockAsync($"incident-report:{tripId}:{currentUser.UserId}");
                duplicateInTransaction = await FindDuplicateReportAsync(tripId, model, currentUser);
                if (duplicateInTransaction.HasValue)
                {
                    return null;
                }

                var created = new TripIncident
                {
                    IncidentCode = await _codeGenerator.NextAsync(CodePrefixes.Incident),
                    TripId = tripId,
                    ReportedByUserId = currentUser.UserId,
                    IncidentType = model.IncidentType,
                    Severity = model.Severity,
                    InitialSeverity = model.Severity,
                    Location = model.Location,
                    Description = model.Description,
                    AffectedTripHorseId = model.AffectedTripHorseId,
                    EmergencyOffload = model.EmergencyOffload,
                    Status = IncidentStatuses.Reported
                };

                if (emergencyHorse != null)
                {
                    emergencyHorse.Status = TripHorseStatuses.Offloaded;
                    emergencyHorse.OffloadedAt = DateTimeOffset.UtcNow;
                    emergencyHorse.OffloadReason = Truncate(model.Description, OffloadReasonMaxLength);
                    emergencyHorse.OffloadOutcome = null;
                }
                await _incidentRepo.AddAsync(created);

                string title = $"Incident {created.IncidentCode} on trip {trip.TripCode}";
                string emergencyText = emergencyHorse == null
                    ? string.Empty
                    : $" {emergencyHorse.BookingHorse!.Horse!.Name} was taken off the vehicle in an emergency; propose an OffloadHorse plan with the next step for the horse.";
                string message = $"{created.Severity} {created.IncidentType} at {created.Location}: {created.Description}{emergencyText}";

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

                if (emergencyHorse != null)
                {
                    var owner = emergencyHorse.BookingHorse!.Booking!;
                    await _notificationService.SendAsync(
                        owner.CustomerUserId,
                        $"{emergencyHorse.BookingHorse.Horse!.Name} was taken off trip {trip.TripCode}",
                        $"On the veterinarian's instruction the horse was offloaded at {created.Location}. We will tell you the next step as soon as it is decided.",
                        NotificationReferenceTypes.Booking,
                        owner.BookingId,
                        emailCustomers: true);
                }

                foreach (var recipient in bookingRecipients)
                {
                    await _notificationService.EmailCustomerAsync(
                        recipient.CustomerUserId,
                        EmailTemplateNames.TripIncident,
                        CustomerPages.Trip(trip.TripId),
                        new Dictionary<string, string>
                        {
                            ["TripCode"] = trip.TripCode,
                            ["BookingCode"] = recipient.BookingCode,
                            ["IncidentCode"] = created.IncidentCode,
                            ["Severity"] = SeverityLabel(created.Severity),
                            ["IncidentDescription"] = $"{created.Description} (vị trí: {created.Location})"
                        });
                }

                return (TripIncident?)created;
            });

            if (incident == null)
            {
                return await GetIncidentByIdAsync(duplicateInTransaction!.Value, currentUser);
            }

            await _trackingService.InvalidateTripAsync(tripId);
            // R-13: sự cố rất nghiêm trọng gắn với một con ngựa -> tự mở hồ sơ bồi thường
            var created = await _incidentRepo.GetQueryable().AsNoTracking()
                .Where(i => i.TripId == tripId && i.ReportedByUserId == currentUser.UserId)
                .OrderByDescending(i => i.IncidentId)
                .Select(i => new { i.IncidentId, i.IncidentCode, i.Severity, i.AffectedTripHorseId, i.Description })
                .FirstAsync();
            if (created.Severity == IncidentSeverities.Critical && created.AffectedTripHorseId.HasValue)
            {
                var affected = trip.TripHorses.First(th => th.TripHorseId == created.AffectedTripHorseId);
                await _claimService.OpenAutomaticallyAsync(affected.BookingHorseId, ClaimTypes.Injury,
                    $"Critical incident {created.IncidentCode}: {created.Description}", created.IncidentId);
            }

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
            if (!IncidentResponseActions.All.Contains(model.ResponseAction))
            {
                throw new BusinessException($"ResponseAction '{model.ResponseAction}' is invalid. Allowed values: {string.Join(", ", IncidentResponseActions.All)}.");
            }

            var trip = incident.Trip!;
            var actionPayload = BuildActionPayload(model);
            await ValidateReplacementPlanAsync(trip, model.ResponseAction, actionPayload);

            // OffloadHorse (R-12): chọn con ngựa + hướng xử lý tiếp; con đã dỡ khẩn cấp thì phương án phải là OffloadHorse cho chính con đó
            TripHorse? offloadHorse = null;
            if (model.ResponseAction == IncidentResponseActions.OffloadHorse)
            {
                offloadHorse = ResolveOffloadHorse(incident, model.AffectedTripHorseId);
                if (!string.IsNullOrWhiteSpace(model.VetReportPath) && !_fileStorage.IsValidPath(model.VetReportPath.Trim(), FileCategories.Documents))
                {
                    throw new BusinessException("VetReportPath is not a valid file path. Attach the report with POST /api/incidents/{id}/documents instead.");
                }
                // VetReportPath (cách cũ) vẫn nhận: được tính như một tài liệu VetReport của sự cố
                var docTypes = incident.Documents.Select(d => d.DocumentType).ToList();
                if (!string.IsNullOrWhiteSpace(model.VetReportPath))
                {
                    docTypes.Add(IncidentDocumentTypes.VetReport);
                }
                OffloadRules.ValidatePlan(model.OffloadOutcome, docTypes);
            }
            else if (incident.EmergencyOffload)
            {
                throw new BusinessException($"A horse was offloaded in an emergency on this incident, so the plan must be {IncidentResponseActions.OffloadHorse} with the next step for that horse.");
            }
            EuthanasiaConsentRules.ValidatePlan(model.ResponseAction, model.OffloadOutcome, model.RequiresEuthanasiaConsent);
            OffloadRules.ValidateVetClinic(offloadHorse == null ? null : model.OffloadOutcome, model.VetClinic);
            if (offloadHorse != null && model.VetClinic != null)
            {
                actionPayload = new IncidentActionPayloadModel { VetClinic = model.VetClinic };
            }

            // R-14: quay về nơi đi -> tuyến về do hệ thống dựng lúc duyệt, không kèm thay đổi tuyến tự nhập
            bool hasRouteInput = model.RouteChange != null && (model.RouteChange.DeactivateCheckpointIds.Count > 0 || model.RouteChange.NewCheckpoints.Count > 0);
            if (model.ResponseAction == IncidentResponseActions.ReturnToOrigin)
            {
                EnsureOnTheRoad(trip);
                if (hasRouteInput)
                {
                    throw new BusinessException("A ReturnToOrigin plan builds the return route automatically; do not send RouteChange.");
                }
            }
            if (model.ExtraCostForCustomer < 0)
            {
                throw new BusinessException("ExtraCostForCustomer cannot be negative.");
            }
            if (model.ExtraCostForCustomer > 0 && model.ResponseAction != IncidentResponseActions.Reroute)
            {
                throw new BusinessException("ExtraCostForCustomer is only used with a Reroute plan (a detour the customers must agree to).");
            }

            // Thay đổi tuyến đi kèm: kiểm tra ngay trên lộ trình hiện tại để Manager duyệt đúng tuyến sẽ đi
            var change = model.RouteChange;
            bool hasChange = change != null && (change.DeactivateCheckpointIds.Count > 0 || change.NewCheckpoints.Count > 0);
            if (model.ResponseAction == IncidentResponseActions.Reroute && !hasChange)
            {
                throw new BusinessException("A reroute plan must remove or add at least one checkpoint.");
            }

            RoutePlan? plan = null;
            if (hasChange)
            {
                EnsureOnTheRoad(trip);
                plan = BuildRoutePlan(trip, change!);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                incident.ProposedAction = model.ProposedAction;
                incident.ResponseAction = model.ResponseAction;
                incident.ActionPayload = IncidentMapper.SerializeActionPayload(actionPayload);
                incident.RouteChange = IncidentMapper.SerializeRouteChange(plan == null ? null : ToRouteChange(plan, change!.InsertAfterCheckpointId));
                incident.RevisedRouteNotes = model.RevisedRouteNotes;
                incident.AdditionalCost = model.AdditionalCost;
                incident.ExtraCostForCustomer = model.ExtraCostForCustomer;
                incident.OffloadOutcome = offloadHorse == null ? null : model.OffloadOutcome;
                incident.VetReportPath = offloadHorse == null || string.IsNullOrWhiteSpace(model.VetReportPath) ? null : model.VetReportPath.Trim();
                // Biên bản gửi theo cách cũ cũng lưu thành tài liệu của sự cố, để mọi nơi chỉ đọc một chỗ
                if (incident.VetReportPath != null && !incident.Documents.Any(d => d.FilePath == incident.VetReportPath))
                {
                    await AddDocumentAsync(incident, incident.VetReportPath, IncidentDocumentTypes.VetReport, null, currentUser.UserId);
                }
                if (offloadHorse != null)
                {
                    incident.AffectedTripHorseId = offloadHorse.TripHorseId;
                }
                incident.RequiresEuthanasiaConsent = offloadHorse != null && model.RequiresEuthanasiaConsent;
                incident.PendingConsentType = null;
                incident.ConsentRequestedAt = null;
                incident.Status = IncidentStatuses.PlanProposed;
                await _incidentRepo.UpdateAsync(incident);

                string routeText = plan == null ? string.Empty : $" Route change: {DescribeRouteChange(plan.Removed, plan.Added)}";
                string offloadText = offloadHorse == null
                    ? string.Empty
                    : $" {offloadHorse.BookingHorse!.Horse!.Name} leaves the trip ({incident.OffloadOutcome}).{(incident.RequiresEuthanasiaConsent ? " The vet recommends euthanasia: the owner is asked to agree after approval." : string.Empty)}";
                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Plan for incident {incident.IncidentCode} needs approval",
                    $"Trip {trip.TripCode}: {incident.ProposedAction} (additional cost {incident.AdditionalCost:0.##}).{routeText}{offloadText}",
                    NotificationReferenceTypes.Incident,
                    incident.IncidentId);
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await GetIncidentByIdAsync(incidentId, currentUser);
        }

        // Duyệt phương án. Phương án có thay đổi tuyến thì tuyến được áp vào lộ trình ngay trong lần duyệt này.
        public async Task<IncidentModel> ApprovePlanAsync(int incidentId, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            EnsurePlanProposed(incident);

            var trip = incident.Trip!;
            var customerIds = await GetCustomerIdsAsync(trip.TripId);
            var replacementPlan = await ValidateReplacementPlanAsync(
                trip,
                incident.ResponseAction!,
                IncidentMapper.ParseActionPayload(incident.ActionPayload));

            var routeChange = IncidentMapper.ParseRouteChange(incident.RouteChange);
            RoutePlan? plan = null;
            if (routeChange != null && routeChange.AppliedAt == null)
            {
                // Đoàn xe có thể đã đi tiếp từ lúc đề xuất: tuyến không còn hợp lệ thì Manager trả về để đề xuất lại
                try
                {
                    EnsureOnTheRoad(trip);
                    plan = BuildRoutePlan(trip, ToRerouteModel(routeChange));
                }
                catch (BusinessException ex)
                {
                    throw new BusinessException($"The proposed route change no longer fits the current route: {ex.Message} Reject the plan so the coordinator can propose a new one.");
                }
            }

            // Con ngựa có thể đã được giao / đổi trạng thái từ lúc đề xuất; phương án cũ (chưa có hướng xử lý) cũng phải đề xuất lại
            TripHorse? offloadHorse = null;
            if (incident.ResponseAction == IncidentResponseActions.OffloadHorse)
            {
                offloadHorse = trip.TripHorses.FirstOrDefault(th => th.TripHorseId == incident.AffectedTripHorseId);
                if (offloadHorse == null || incident.OffloadOutcome == null || !OffloadRules.CanBeOffloaded(offloadHorse, incident))
                {
                    string state = offloadHorse == null ? "no horse is selected" : $"{offloadHorse.BookingHorse!.Horse!.Name} is {offloadHorse.Status}";
                    string outcomeText = incident.OffloadOutcome == null ? ", no next step chosen" : string.Empty;
                    throw new BusinessException($"The offload plan can no longer be applied ({state}{outcomeText}). Reject the plan so the coordinator can propose a new one.");
                }
            }

            // An tử theo khuyến nghị thú y: chưa đổi trạng thái con ngựa, chờ chủ ngựa đồng ý (Chính sách 9.6)
            if (offloadHorse != null && incident.RequiresEuthanasiaConsent)
            {
                var policy = await _settingService.GetPolicySettingsAsync();
                var owner = offloadHorse.BookingHorse!.Booking!;
                string horseName = offloadHorse.BookingHorse.Horse!.Name;
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    incident.Status = IncidentStatuses.AwaitingConsent;
                    incident.PendingConsentType = ConsentTypes.Euthanasia;
                    incident.ConsentRequestedAt = DateTimeOffset.UtcNow;
                    incident.RejectionReason = null;
                    incident.ApprovedByUserId = currentUser.UserId;
                    incident.ApprovedAt = DateTimeOffset.UtcNow;
                    await _incidentRepo.UpdateAsync(incident);

                    await _notificationService.SendAsync(
                        owner.CustomerUserId,
                        $"Your decision is needed about {horseName}",
                        $"Incident {incident.IncidentCode} on trip {trip.TripCode}: the veterinarian recommends humane euthanasia for {horseName}. Please read the vet report in the app and agree or decline within {policy.EuthanasiaConsentMinutes} minutes. If we cannot reach you in time, the emergency authorisation clause (service policy 9.6) lets us follow the vet's advice to spare the horse further suffering. If you decline, further treatment costs are yours.",
                        NotificationReferenceTypes.Incident,
                        incident.IncidentId,
                        emailCustomers: true);
                    await _notificationService.SendToUsersAsync(
                        WithPlanner(CrewIds(trip), trip),
                        $"Incident {incident.IncidentCode} waits for the owner's decision",
                        $"{horseName}: euthanasia is recommended. The owner has {policy.EuthanasiaConsentMinutes} minutes to answer; keep the horse comfortable under the vet's care until then.",
                        NotificationReferenceTypes.Incident,
                        incident.IncidentId);
                });
                await _trackingService.InvalidateTripAsync(trip.TripId);
                return await GetIncidentByIdAsync(incidentId, currentUser);
            }

            // R-14: đi đường vòng tốn thêm tiền -> chưa áp tuyến, chờ các khách có ngựa trên xe đồng ý
            if (plan != null && incident.ExtraCostForCustomer > 0)
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    incident.Status = IncidentStatuses.AwaitingConsent;
                    incident.PendingConsentType = ConsentTypes.Detour;
                    incident.ConsentRequestedAt = DateTimeOffset.UtcNow;
                    incident.RejectionReason = null;
                    incident.ApprovedByUserId = currentUser.UserId;
                    incident.ApprovedAt = DateTimeOffset.UtcNow;
                    await _incidentRepo.UpdateAsync(incident);

                    await _notificationService.SendToUsersAsync(
                        OnBoardCustomerIds(trip),
                        $"Your agreement is needed for trip {trip.TripCode}",
                        $"Incident {incident.IncidentCode}: {incident.ProposedAction} The detour ({DescribeRouteChange(plan.Removed, plan.Added)}) costs {incident.ExtraCostForCustomer:N2} USD more in total, shared by the horses on board. Accept or decline it in the app; if anyone declines, the convoy returns to the origin.",
                        NotificationReferenceTypes.Incident,
                        incident.IncidentId,
                        emailCustomers: true);
                    await _notificationService.SendToUsersAsync(
                        WithPlanner(CrewIds(trip), trip),
                        $"Detour for incident {incident.IncidentCode} waits for the customers",
                        "The plan is approved but costs the customers extra; the route changes once every customer with horses on board agrees.",
                        NotificationReferenceTypes.Incident,
                        incident.IncidentId);
                });
                await _trackingService.InvalidateTripAsync(trip.TripId);
                return await GetIncidentByIdAsync(incidentId, currentUser);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                incident.Status = IncidentStatuses.Approved;
                incident.RejectionReason = null;
                incident.ApprovedByUserId = currentUser.UserId;
                incident.ApprovedAt = DateTimeOffset.UtcNow;

                // R-14: quay về nơi đi: dựng tuyến về từ các mốc đã đi
                if (incident.ResponseAction == IncidentResponseActions.ReturnToOrigin)
                {
                    await ApplyReturnToOriginAsync(trip, incident);
                }

                if (plan != null)
                {
                    await ApplyRoutePlanAsync(trip, plan);
                    for (int i = 0; i < plan.Added.Count; i++)
                    {
                        routeChange!.NewCheckpoints[i].CheckpointId = plan.Added[i].CheckpointId;
                    }
                    routeChange!.AppliedAt = DateTimeOffset.UtcNow;
                    incident.RouteChange = IncidentMapper.SerializeRouteChange(routeChange);
                }
                if (offloadHorse != null)
                {
                    incident.Severity = IncidentSeverityRules.EffectiveSeverityOnApproval(incident.Severity, incident.OffloadOutcome);
                    await ApplyOffloadAsync(trip, incident, offloadHorse);
                }
                if (replacementPlan != null)
                {
                    await ApplyReplacementAsync(trip, incident, replacementPlan);
                }
                await _incidentRepo.UpdateAsync(incident);

                string routeText = plan == null ? string.Empty : $" Route changed: {DescribeRouteChange(plan.Removed, plan.Added)}";

                await _notificationService.SendToUsersAsync(
                    WithPlanner(CrewIds(trip), trip),
                    $"Plan for incident {incident.IncidentCode} was approved",
                    $"Trip {trip.TripCode}: {incident.ProposedAction}{routeText}{(plan == null ? string.Empty : " Check the updated checkpoint list before moving on.")}",
                    NotificationReferenceTypes.Incident,
                    incident.IncidentId);

                await _notificationService.SendToUsersAsync(
                    customerIds,
                    $"Update on the incident on trip {trip.TripCode}",
                    $"A response plan has been approved: {incident.ProposedAction}{routeText}",
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

        // Điều chỉnh tuyến thêm sau khi phương án đã duyệt: bỏ các mốc chưa tới (IsActive = false) và chèn mốc mới.
        // Gọi lại được nhiều lần khi phương án còn Approved; mỗi lần được ghi nối vào thay đổi tuyến của sự cố.
        public async Task<TripDetailModel> ApplyRerouteAsync(int incidentId, ApplyRerouteModel model, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            var trip = incident.Trip!;

            if (incident.Status != IncidentStatuses.Approved)
            {
                throw new BusinessException($"The route can only be changed after the plan is approved (current status: {incident.Status}).");
            }
            EnsureOnTheRoad(trip);

            var plan = BuildRoutePlan(trip, model);
            var customerIds = await GetCustomerIdsAsync(trip.TripId);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await ApplyRoutePlanAsync(trip, plan);

                var applied = ToRouteChange(plan, model.InsertAfterCheckpointId);
                applied.AppliedAt = DateTimeOffset.UtcNow;
                var existing = IncidentMapper.ParseRouteChange(incident.RouteChange);
                if (existing != null)
                {
                    existing.RemovedCheckpoints.AddRange(applied.RemovedCheckpoints);
                    existing.NewCheckpoints.AddRange(applied.NewCheckpoints);
                    existing.AppliedAt ??= applied.AppliedAt;
                    applied = existing;
                }
                incident.RouteChange = IncidentMapper.SerializeRouteChange(applied);
                await _incidentRepo.UpdateAsync(incident);

                string change = DescribeRouteChange(plan.Removed, plan.Added);

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

        // Kết quả kiểm tra một thay đổi tuyến trên lộ trình hiện tại của chuyến
        private sealed class RoutePlan
        {
            public List<RouteCheckpoint> Removed { get; init; } = new();
            public List<RouteCheckpoint> Added { get; init; } = new();
            public RouteCheckpoint Anchor { get; init; } = null!;
            public List<RouteCheckpoint> NewRoute { get; init; } = new();
        }

        private static void EnsureOnTheRoad(Trip trip)
        {
            if (!OnTheRoadStatuses.Contains(trip.OverallStatus))
            {
                throw new BusinessException($"The route can only be changed while the trip is on the road (current status: {trip.OverallStatus}).");
            }
        }

        // Kiểm tra các mốc bỏ / thêm và dựng lộ trình mới, chưa ghi gì vào DB
        private static RoutePlan BuildRoutePlan(Trip trip, ApplyRerouteModel model)
        {
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
                // Điểm đón / trả của một đơn: bỏ đi thì ngựa của đơn không được đón / giao (R-04)
                if (CheckpointTypes.BookingStops.Contains(checkpoint.CheckpointType))
                {
                    throw new BusinessException($"'{checkpoint.CheckpointName}' is where a booking is picked up or dropped off and cannot be removed from the route.");
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
                Latitude = input.Latitude,
                Longitude = input.Longitude,
                Status = CheckpointStatuses.Pending,
                IsActive = true
            }).ToList();

            var newRoute = route.Except(removed).ToList();
            newRoute.InsertRange(newRoute.IndexOf(anchor) + 1, added);

            return new RoutePlan { Removed = removed, Added = added, Anchor = anchor, NewRoute = newRoute };
        }

        // Ghi thay đổi tuyến vào lộ trình (gọi trong transaction); chuyến chuyển EmergencyRerouting tới khi sự cố được đóng
        private async Task ApplyRoutePlanAsync(Trip trip, RoutePlan plan)
        {
            foreach (var checkpoint in plan.Removed)
            {
                checkpoint.IsActive = false;
                checkpoint.Status = CheckpointStatuses.Cancelled;
            }

            await ResequenceAsync(plan.NewRoute, plan.Added);

            trip.OverallStatus = TripStatuses.EmergencyRerouting;
            await _tripRepo.UpdateAsync(trip);
        }

        // insertAfterCheckpointId giữ nguyên lựa chọn của Coordinator (null = sau mốc gần nhất đã tới lúc áp tuyến)
        private static IncidentRouteChangeModel ToRouteChange(RoutePlan plan, int? insertAfterCheckpointId)
        {
            return new IncidentRouteChangeModel
            {
                InsertAfterCheckpointId = insertAfterCheckpointId,
                InsertAfterName = plan.Added.Count > 0 ? plan.Anchor.CheckpointName : null,
                RemovedCheckpoints = plan.Removed.Select(c => new RouteChangeRemovedCheckpointModel
                {
                    CheckpointId = c.CheckpointId,
                    CheckpointName = c.CheckpointName,
                    PlannedTime = c.PlannedTime
                }).ToList(),
                NewCheckpoints = plan.Added.Select(c => new RouteChangeNewCheckpointModel
                {
                    CheckpointId = c.CheckpointId == 0 ? null : c.CheckpointId,
                    CheckpointName = c.CheckpointName,
                    CheckpointType = c.CheckpointType,
                    Address = c.Address,
                    PlannedTime = c.PlannedTime,
                    MandatoryRestMinutes = c.MandatoryRestMinutes,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude
                }).ToList()
            };
        }

        private static ApplyRerouteModel ToRerouteModel(IncidentRouteChangeModel routeChange)
        {
            return new ApplyRerouteModel
            {
                InsertAfterCheckpointId = routeChange.InsertAfterCheckpointId,
                DeactivateCheckpointIds = routeChange.RemovedCheckpoints.Select(c => c.CheckpointId).ToList(),
                NewCheckpoints = routeChange.NewCheckpoints.Select(c => new CheckpointInputModel
                {
                    CheckpointName = c.CheckpointName,
                    CheckpointType = c.CheckpointType,
                    Address = c.Address,
                    PlannedTime = c.PlannedTime,
                    MandatoryRestMinutes = c.MandatoryRestMinutes,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude
                }).ToList()
            };
        }

        public async Task<IncidentModel> ResolveIncidentAsync(int incidentId, string faultParty, string? faultNote, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            var trip = incident.Trip!;

            // R-09: nguyên nhân quyết định ai chịu chi phí, hoàn tiền và bồi thường giao trễ (Chính sách mục 9.1)
            if (!FaultParties.All.Contains(faultParty))
            {
                throw new BusinessException($"FaultParty '{faultParty}' is invalid. Allowed values: {string.Join(", ", FaultParties.All)}.");
            }

            // Phải có phương án đã duyệt; nguyên nhân dùng khi chốt KPI và bồi thường giao trễ lúc đóng chuyến
            if (incident.Status != IncidentStatuses.Approved)
            {
                throw new BusinessException($"Only an incident with an approved plan can be resolved (current status: {incident.Status}).");
            }

            var customerIds = await GetCustomerIdsAsync(trip.TripId);
            var now = DateTimeOffset.UtcNow;

            // R-14: gửi chuồng tạm -> phí chuồng (ngày × số ngựa trên xe × phí / con / ngày) cộng vào chi phí phát sinh của sự cố;
            // ai trả theo nguyên nhân (lỗi công ty thì không thu khách, xem lúc đóng chuyến)
            decimal stableFee = 0;
            if (incident.ResponseAction == IncidentResponseActions.HoldAtStable && incident.ApprovedAt.HasValue)
            {
                var policy = await _settingService.GetPolicySettingsAsync();
                int horses = trip.TripHorses.Count(th => TripHorseStatuses.OnBoard.Contains(th.Status));
                stableFee = ReturnRouteRules.StableFee(incident.ApprovedAt.Value, now, horses, policy.StableFeePerHorsePerDayUsd);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                incident.Status = IncidentStatuses.Resolved;
                incident.ResolvedAt = now;
                incident.AdditionalCost += stableFee;
                incident.FaultParty = faultParty;
                incident.FaultNote = string.IsNullOrWhiteSpace(faultNote) ? null : faultNote.Trim();
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
                    stableFee > 0
                        ? $"The incident has been handled. Temporary stable: {stableFee:N2} USD (charged according to the cause)."
                        : "The incident has been handled and the trip continues.",
                    NotificationReferenceTypes.Incident,
                    incident.IncidentId);
            });

            // R-14: quay về nơi đi -> hoàn tiền từng đơn theo nguyên nhân (gọi cổng thanh toán, ngoài transaction)
            if (incident.ResponseAction == IncidentResponseActions.ReturnToOrigin)
            {
                await RefundReturnedBookingsAsync(incident, faultParty, currentUser);
            }
            // R-12 / R-14: một con ngựa rời chuyến -> hoàn phần cước chưa đi của con đó (theo nguyên nhân).
            // WaitRecover cũng vậy: đơn của con ngựa kết thúc tại phòng khám, khỏi bệnh thì đi tiếp bằng đơn nối tiếp
            else if (incident.ResponseAction == IncidentResponseActions.OffloadHorse && incident.OffloadOutcome != null)
            {
                await RefundOffloadedHorseAsync(trip, incident, faultParty, currentUser);
            }

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await GetIncidentByIdAsync(incidentId, currentUser);
        }

        // Tài liệu của sự cố: tài xế bổ sung sau lúc báo SOS (biên bản thú y thường có sau), Coordinator đính kèm lúc lập phương án
        public async Task<IncidentDocumentModel> UploadDocumentAsync(int incidentId, UploadIncidentDocumentModel model, CurrentUser currentUser)
        {
            if (!IncidentDocumentTypes.All.Contains(model.DocumentType))
            {
                throw new BusinessException($"DocumentType '{model.DocumentType}' is invalid. Allowed values: {string.Join(", ", IncidentDocumentTypes.All)}.");
            }

            var incident = await GetTrackedIncidentAsync(incidentId);
            await EnsureCanSeeIncidentAsync(incidentId, currentUser);

            // Mọi kiểm tra nghiệp vụ đã qua mới đẩy file lên storage
            var stored = await _fileStorage.UploadAsync(model.File.Content, model.File.FileName, model.File.SizeBytes, FileCategories.Documents);
            var doc = await AddDocumentAsync(incident, stored.Path, model.DocumentType, model.Note, currentUser.UserId);

            // Người xử lý sự cố cần biết có giấy mới (ví dụ biên bản thú y để lập phương án Deceased)
            var trip = incident.Trip!;
            string title = $"New {doc.DocumentType} on incident {incident.IncidentCode}";
            string message = $"Trip {trip.TripCode}: a {doc.DocumentType} was attached to incident {incident.IncidentCode}.{(doc.Note == null ? string.Empty : $" Note: {doc.Note}")}";
            var recipients = WithPlanner(new List<int>(), trip).Where(id => id != currentUser.UserId).ToList();
            if (recipients.Count > 0)
            {
                await _notificationService.SendToUsersAsync(recipients, title, message, NotificationReferenceTypes.Incident, incident.IncidentId);
            }
            else if (!trip.PlannedByUserId.HasValue)
            {
                await _notificationService.SendToRoleAsync(UserRoles.FleetCoordinator, title, message, NotificationReferenceTypes.Incident, incident.IncidentId);
            }
            await _notificationService.SendToRoleAsync(UserRoles.LogisticsManager, title, message, NotificationReferenceTypes.Incident, incident.IncidentId);

            var user = await _userRepo.GetByIdAsync(currentUser.UserId);
            var result = ToDocumentModel(doc, user?.FullName);
            await _fileStorage.ResolveViewUrlsAsync(new[] { result }, d => d.FilePath, (d, url) => d.FileUrl = url);
            return result;
        }

        public async Task DeleteDocumentAsync(int incidentId, int documentId, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            await EnsureCanSeeIncidentAsync(incidentId, currentUser);

            var doc = incident.Documents.FirstOrDefault(d => d.IncidentDocumentId == documentId)
                ?? throw new NotFoundException($"Document with ID {documentId} was not found on incident {incident.IncidentCode}.");

            bool isManager = currentUser.Role == UserRoles.LogisticsManager || currentUser.Role == UserRoles.Admin;
            if (doc.UploadedByUserId != currentUser.UserId && !isManager)
            {
                throw new ForbiddenException("Only the person who uploaded this document (or a logistics manager) can delete it.");
            }
            if (incident.Status == IncidentStatuses.Approved || incident.Status == IncidentStatuses.Resolved)
            {
                throw new BusinessException($"Incident {incident.IncidentCode} is {incident.Status}; its documents are kept as evidence and cannot be deleted.");
            }

            await _documentRepo.DeleteAsync(doc);
        }

        private async Task<IncidentDocument> AddDocumentAsync(TripIncident incident, string path, string documentType, string? note, int userId)
        {
            var doc = new IncidentDocument
            {
                IncidentId = incident.IncidentId,
                FilePath = path,
                DocumentType = documentType,
                Note = note,
                UploadedByUserId = userId,
                UploadedAt = DateTimeOffset.UtcNow
            };
            await _documentRepo.AddAsync(doc);
            await _documentRepo.SaveChangesAsync();
            incident.Documents.Add(doc);
            return doc;
        }

        private static IncidentDocumentModel ToDocumentModel(IncidentDocument doc, string? uploadedByName) => new()
        {
            IncidentDocumentId = doc.IncidentDocumentId,
            IncidentId = doc.IncidentId,
            FilePath = doc.FilePath,
            DocumentType = doc.DocumentType,
            Note = doc.Note,
            UploadedByUserId = doc.UploadedByUserId,
            UploadedByName = uploadedByName,
            UploadedAt = doc.UploadedAt
        };

        // Khách / tài xế chỉ thấy sự cố của chuyến mình (cùng luật với GET)
        private async Task EnsureCanSeeIncidentAsync(int incidentId, CurrentUser currentUser)
        {
            bool canView = await ApplyVisibility(_incidentRepo.GetQueryable(), currentUser).AnyAsync(i => i.IncidentId == incidentId);
            if (!canView)
            {
                throw new ForbiddenException("You can only manage documents of incidents on your own trips.");
            }
        }

        // =============================================================================================
        // Quay về nơi đi, hỏi ý khách khi đi đường vòng (R-14, BE-14b)
        // =============================================================================================

        public async Task<IncidentModel> RecordCustomerConsentAsync(int incidentId, bool accept, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            var trip = incident.Trip!;
            if (incident.Status != IncidentStatuses.AwaitingConsent)
            {
                throw new BusinessException($"This incident is not waiting for your agreement (status: {incident.Status}).");
            }
            if (EuthanasiaConsentRules.PendingType(incident) == ConsentTypes.Euthanasia)
            {
                return await RecordEuthanasiaConsentAsync(incident, accept, currentUser);
            }

            var myBookingIds = trip.TripHorses
                .Where(th => TripHorseStatuses.OnBoard.Contains(th.Status) && th.BookingHorse!.Booking!.CustomerUserId == currentUser.UserId)
                .Select(th => th.BookingHorse!.BookingId)
                .Distinct()
                .ToList();
            if (myBookingIds.Count == 0)
            {
                throw new ForbiddenException("Only customers with horses on this trip can answer.");
            }

            var existing = await _consentRepo.GetQueryable().Where(c => c.IncidentId == incidentId && c.ConsentType == ConsentTypes.Detour).ToListAsync();
            var now = DateTimeOffset.UtcNow;
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (int bookingId in myBookingIds)
                {
                    var consent = existing.FirstOrDefault(c => c.BookingId == bookingId);
                    if (consent == null)
                    {
                        consent = new IncidentConsent { IncidentId = incidentId, ConsentType = ConsentTypes.Detour, BookingId = bookingId, CustomerUserId = currentUser.UserId };
                        await _consentRepo.AddAsync(consent);
                        existing.Add(consent);
                    }
                    consent.Accepted = accept;
                    consent.DecidedAt = now;
                }
                await _consentRepo.SaveChangesAsync();

                var onBoardBookingIds = trip.TripHorses
                    .Where(th => TripHorseStatuses.OnBoard.Contains(th.Status))
                    .Select(th => th.BookingHorse!.BookingId)
                    .Distinct()
                    .ToList();

                if (!accept)
                {
                    // Một khách không đồng ý -> cả đoàn quay về (Chính sách 9.4), không thu tiền đi vòng
                    incident.ResponseAction = IncidentResponseActions.ReturnToOrigin;
                    incident.ProposedAction = $"Return to the origin: a customer declined the detour. ({incident.ProposedAction})";
                    incident.RouteChange = null;
                    incident.ExtraCostForCustomer = 0;
                    incident.Status = IncidentStatuses.Approved;
                    incident.PendingConsentType = null;
                    await ApplyReturnToOriginAsync(trip, incident);
                    await _incidentRepo.UpdateAsync(incident);
                    await NotifyEveryoneAsync(trip, incident, $"Trip {trip.TripCode} returns to the origin",
                        "A customer declined the detour, so the convoy returns to the origin. Refunds follow the service policy (section 9).");
                    return;
                }

                bool everyoneAccepted = onBoardBookingIds.All(id => existing.Any(c => c.BookingId == id && c.Accepted));
                if (!everyoneAccepted)
                {
                    return;
                }

                // Mọi khách đồng ý -> áp tuyến đi vòng; tiền đi vòng khách trả giữ ở ExtraCostForCustomer
                // (thu khi đóng chuyến, chia theo số ngựa), không cộng vào chi phí vận hành AdditionalCost của công ty
                var routeChange = IncidentMapper.ParseRouteChange(incident.RouteChange)
                    ?? throw new BusinessException("The detour route is missing; reject the plan so the coordinator can propose it again.");
                RoutePlan plan;
                try
                {
                    EnsureOnTheRoad(trip);
                    plan = BuildRoutePlan(trip, ToRerouteModel(routeChange));
                }
                catch (BusinessException ex)
                {
                    throw new BusinessException($"The detour no longer fits the current route: {ex.Message} The logistics team will propose a new plan.");
                }
                await ApplyRoutePlanAsync(trip, plan);
                for (int i = 0; i < plan.Added.Count; i++)
                {
                    routeChange.NewCheckpoints[i].CheckpointId = plan.Added[i].CheckpointId;
                }
                routeChange.AppliedAt = now;
                incident.RouteChange = IncidentMapper.SerializeRouteChange(routeChange);
                incident.Status = IncidentStatuses.Approved;
                incident.PendingConsentType = null;
                await _incidentRepo.UpdateAsync(incident);
                await NotifyEveryoneAsync(trip, incident, $"Trip {trip.TripCode} takes the detour",
                    $"Every customer agreed. Route changed: {DescribeRouteChange(plan.Removed, plan.Added)}");
            });

            // R-17: đi đường vòng có tính thêm tiền -> bản hợp đồng mới cho các đơn đã giao kết hợp đồng
            if (accept && incident.Status == IncidentStatuses.Approved && incident.ResponseAction == IncidentResponseActions.Reroute)
            {
                var bookingIds = trip.TripHorses.Where(th => TripHorseStatuses.OnBoard.Contains(th.Status))
                    .Select(th => th.BookingHorse!.BookingId).Distinct().ToList();
                foreach (int bookingId in bookingIds)
                {
                    try
                    {
                        await _contractService.CreateAmendmentAsync(bookingId, ContractReasons.DetourAccepted);
                    }
                    catch (BusinessException)
                    {
                        // Đơn chưa giao kết hợp đồng (đơn cũ): không có bản sửa đổi
                    }
                }
            }

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await GetIncidentByIdAsync(incidentId, currentUser);
        }

        // Chủ của con ngựa trả lời khuyến nghị an tử. Đồng ý -> áp phương án Deceased. Từ chối -> phương án bị trả về để điều phối
        // lập phương án khác (thường là WaitRecover tại phòng khám); đoàn xe đi tiếp, không quay về. Chi phí điều trị từ đó do chủ ngựa chịu.
        private async Task<IncidentModel> RecordEuthanasiaConsentAsync(TripIncident incident, bool accept, CurrentUser currentUser)
        {
            var trip = incident.Trip!;
            var horse = trip.TripHorses.FirstOrDefault(th => th.TripHorseId == incident.AffectedTripHorseId)
                ?? throw new BusinessException("The horse of this euthanasia request is no longer on the trip; the logistics team will review the incident.");
            var booking = horse.BookingHorse!.Booking!;
            string horseName = horse.BookingHorse.Horse!.Name;
            if (booking.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException($"Only the owner of {horseName} can answer this euthanasia request.");
            }
            if (accept && !OffloadRules.CanBeOffloaded(horse, incident))
            {
                throw new BusinessException($"{horseName} is {horse.Status}; the plan can no longer be applied. The logistics team will review the incident.");
            }

            var now = DateTimeOffset.UtcNow;
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var consent = await _consentRepo.GetQueryable().FirstOrDefaultAsync(c =>
                    c.IncidentId == incident.IncidentId && c.BookingId == booking.BookingId && c.ConsentType == ConsentTypes.Euthanasia);
                if (consent == null)
                {
                    consent = new IncidentConsent
                    {
                        IncidentId = incident.IncidentId,
                        ConsentType = ConsentTypes.Euthanasia,
                        BookingId = booking.BookingId,
                        CustomerUserId = currentUser.UserId
                    };
                    await _consentRepo.AddAsync(consent);
                }
                consent.Accepted = accept;
                consent.DecidedAt = now;
                await _consentRepo.SaveChangesAsync();

                incident.PendingConsentType = null;
                if (accept)
                {
                    incident.Status = IncidentStatuses.Approved;
                    incident.Severity = IncidentSeverityRules.EffectiveSeverityOnApproval(incident.Severity, incident.OffloadOutcome);
                    await ApplyOffloadAsync(trip, incident, horse);
                    await _incidentRepo.UpdateAsync(incident);
                    await NotifyStaffAsync(trip, incident, $"The owner agreed to euthanasia for {horseName}",
                        $"Incident {incident.IncidentCode}: carry out the vet's advice and attach the death certificate to the incident.");
                    return;
                }

                incident.Status = IncidentStatuses.Rejected;
                incident.RequiresEuthanasiaConsent = false;
                incident.ApprovedByUserId = null;
                incident.ApprovedAt = null;
                incident.RejectionReason = $"The owner declined euthanasia for {horseName}. Propose another plan (for example OffloadHorse with WaitRecover at a vet clinic); further treatment costs are borne by the owner (service policy 9.6).";
                await _incidentRepo.UpdateAsync(incident);
                await NotifyStaffAsync(trip, incident, $"The owner declined euthanasia for {horseName}", incident.RejectionReason);
                await _notificationService.SendAsync(booking.CustomerUserId,
                    $"We recorded your decision about {horseName}",
                    $"You declined euthanasia. {horseName} stays under veterinary treatment and our coordinator will tell you the next step. As set out in service policy 9.6, treatment costs from now on are yours.",
                    NotificationReferenceTypes.Incident, incident.IncidentId, emailCustomers: true);
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await GetIncidentByIdAsync(incident.IncidentId, currentUser);
        }

        // Quá hạn mà chủ ngựa chưa trả lời: áp dụng điều khoản ủy quyền khẩn cấp (Chính sách 9.6), cần biên bản thú y
        public async Task<IncidentModel> EmergencyAuthorizeAsync(int incidentId, string note, CurrentUser currentUser)
        {
            var incident = await GetTrackedIncidentAsync(incidentId);
            var policy = await _settingService.GetPolicySettingsAsync();
            var now = DateTimeOffset.UtcNow;
            EuthanasiaConsentRules.EnsureCanEmergencyAuthorize(incident, now, policy.EuthanasiaConsentMinutes);

            var trip = incident.Trip!;
            var horse = trip.TripHorses.FirstOrDefault(th => th.TripHorseId == incident.AffectedTripHorseId);
            if (horse == null || !OffloadRules.CanBeOffloaded(horse, incident))
            {
                throw new BusinessException("The horse can no longer be recorded on this plan. Reject the plan so the coordinator can propose a new one.");
            }
            string horseName = horse.BookingHorse!.Horse!.Name;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                incident.EmergencyAuthorizedAt = now;
                incident.EmergencyAuthorizedByUserId = currentUser.UserId;
                incident.EmergencyAuthorizationNote = note.Trim();
                incident.PendingConsentType = null;
                incident.Status = IncidentStatuses.Approved;
                incident.Severity = IncidentSeverityRules.EffectiveSeverityOnApproval(incident.Severity, incident.OffloadOutcome);
                await ApplyOffloadAsync(trip, incident, horse);
                await _incidentRepo.UpdateAsync(incident);
                await NotifyStaffAsync(trip, incident, $"Emergency authorisation applied for {horseName}",
                    $"Incident {incident.IncidentCode}: the owner did not answer within {policy.EuthanasiaConsentMinutes} minutes. Note: {incident.EmergencyAuthorizationNote}");
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return await GetIncidentByIdAsync(incidentId, currentUser);
        }

        // Ngựa đang điều trị tại phòng khám (WaitRecover) thì chết: ghi nhận tử vong, lưu giấy chứng tử vào sự cố gốc,
        // tự mở hồ sơ bảo hiểm tử vong (bổ sung nếu đã có hồ sơ thương tích đã duyệt)
        public async Task<TripHorseModel> RecordPostOffloadDeathAsync(int tripId, int tripHorseId, PostOffloadDeathModel model, CurrentUser currentUser)
        {
            var (incident, tripHorse) = await FindClinicHorseAsync(tripId, tripHorseId);
            if (tripHorse.Status != TripHorseStatuses.Offloaded)
            {
                throw new BusinessException($"{tripHorse.BookingHorse!.Horse!.Name} is {tripHorse.Status}; only a horse recovering at a vet clinic can be recorded here.");
            }
            var trip = incident.Trip!;
            var bookingHorse = tripHorse.BookingHorse!;
            string horseName = bookingHorse.Horse!.Name;

            var stored = await _fileStorage.UploadAsync(model.DeathCertificate.Content, model.DeathCertificate.FileName, model.DeathCertificate.SizeBytes, FileCategories.Documents);
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await AddDocumentAsync(incident, stored.Path, IncidentDocumentTypes.DeathCertificate, model.Note, currentUser.UserId);
                tripHorse.Status = TripHorseStatuses.Deceased;
                bookingHorse.Status = BookingHorseStatuses.Deceased;
                await _tripRepo.SaveChangesAsync();

                await _claimService.OpenAutomaticallyAsync(bookingHorse.BookingHorseId, ClaimTypes.Death,
                    $"{horseName} died at {tripHorse.VetClinicName ?? "the vet clinic"} after leaving trip {trip.TripCode} ({incident.IncidentCode}).", incident.IncidentId);

                await _notificationService.SendAsync(bookingHorse.Booking!.CustomerUserId,
                    $"We are deeply sorry: {horseName} has died",
                    $"{horseName} died while under veterinary care at {tripHorse.VetClinicName ?? "the vet clinic"}. The death certificate is in the app and our team will contact you about the insurance claim.",
                    NotificationReferenceTypes.Booking, bookingHorse.BookingId, emailCustomers: true);
                await NotifyStaffAsync(trip, incident, $"{horseName} died at the vet clinic",
                    $"Incident {incident.IncidentCode}: the death certificate was recorded and a death claim was opened.");
            });

            await _trackingService.InvalidateTripAsync(trip.TripId);
            return tripHorse.ToBusinessModel();
        }

        // Viện phí của con ngựa ở lại phòng khám; ai chịu theo nguyên nhân đã chốt khi đóng sự cố (Chính sách 9.6)
        public async Task<TripHorseModel> RecordVetExpenseAsync(int tripId, int tripHorseId, RecordVetExpenseModel model, CurrentUser currentUser)
        {
            var (incident, tripHorse) = await FindClinicHorseAsync(tripId, tripHorseId);
            if (incident.Status != IncidentStatuses.Resolved || incident.FaultParty == null)
            {
                throw new BusinessException($"Resolve incident {incident.IncidentCode} first (choose the cause): it decides who bears the vet costs.");
            }
            var bookingHorse = tripHorse.BookingHorse!;
            bool euthanasiaDeclined = await _consentRepo.GetQueryable().AnyAsync(c =>
                c.BookingId == bookingHorse.BookingId && c.ConsentType == ConsentTypes.Euthanasia && !c.Accepted
                && c.Incident!.AffectedTripHorseId == tripHorse.TripHorseId);
            string bearer = VetExpenseRules.Bearer(incident.FaultParty, euthanasiaDeclined);

            tripHorse.VetExpenseAmount = model.Amount;
            tripHorse.VetExpenseBearer = bearer;
            tripHorse.VetExpenseNote = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
            await _tripRepo.SaveChangesAsync();

            string horseName = bookingHorse.Horse!.Name;
            string who = bearer == VetExpenseBearers.Company
                ? "The incident was our fault, so the company covers this bill."
                : euthanasiaDeclined
                    ? "You declined the vet's euthanasia advice, so treatment costs are yours (service policy 9.6). Please settle the bill with the clinic."
                    : "The incident was not our fault, so treatment costs are yours (service policy 9.6). Please settle the bill with the clinic.";
            await _notificationService.SendAsync(bookingHorse.Booking!.CustomerUserId,
                $"Vet bill for {horseName}: {model.Amount:N2} USD",
                $"{tripHorse.VetClinicName ?? "The vet clinic"} billed {model.Amount:N2} USD for {horseName}. {who}",
                NotificationReferenceTypes.Booking, bookingHorse.BookingId, emailCustomers: true);
            return tripHorse.ToBusinessModel();
        }

        // Ngựa ở lại phòng khám đã khỏe (có giấy FitToTravel): tạo đơn nối tiếp từ phòng khám tới đích cũ, cùng khách, cùng người nhận.
        // Đơn đi theo luồng thường (duyệt, lập tuyến, báo giá, hồ sơ, xếp chuyến); lỗi công ty thì báo giá chốt bằng 0.
        public async Task<int> CreateContinuationBookingAsync(int tripId, int tripHorseId, CreateContinuationBookingModel model, CurrentUser currentUser)
        {
            var (incident, tripHorse) = await FindClinicHorseAsync(tripId, tripHorseId);
            var bookingHorse = tripHorse.BookingHorse!;
            string horseName = bookingHorse.Horse!.Name;
            ContinuationRules.EnsureCanContinue(incident, tripHorse);

            bool exists = await _bookingRepo.GetQueryable().AnyAsync(b =>
                b.ParentBookingId == bookingHorse.BookingId
                && b.Status != BookingStatuses.Cancelled && b.Status != BookingStatuses.Rejected
                && b.Status != BookingStatuses.Expired && b.Status != BookingStatuses.QuoteExpired
                && b.BookingHorses.Any(bh => bh.HorseId == bookingHorse.HorseId && bh.Status != BookingHorseStatuses.Cancelled));
            if (exists)
            {
                throw new BusinessException($"{horseName} already has a continuation booking.");
            }

            var parent = await _bookingRepo.GetQueryable().AsNoTracking().FirstAsync(b => b.BookingId == bookingHorse.BookingId);
            bool free = ContinuationRules.IsFree(incident.FaultParty);
            var quote = await _pricingService.CalculateQuoteRangeAsync(new QuoteInputModel
            {
                TransportMode = parent.TransportMode,
                PickupCountryCode = model.PickupCountryCode,
                PickupAddress = tripHorse.VetClinicAddress,
                DropoffCountryCode = parent.DropoffCountryCode,
                DropoffAddress = parent.DropoffAddress,
                RequiresClimateControl = parent.RequiresClimateControl,
                DeclaredValue = bookingHorse.DeclaredValue,
                PickupWindowStart = model.PickupWindowStart,
                PickupWindowEnd = model.PickupWindowEnd,
                DeliveryDeadline = model.DeliveryDeadline,
                PickupLat = tripHorse.VetClinicLat,
                PickupLng = tripHorse.VetClinicLng,
                DropoffLat = parent.DropoffLat,
                DropoffLng = parent.DropoffLng,
                StallClasses = new List<string> { bookingHorse.StallClass }
            });

            var policy = await _settingService.GetPolicySettingsAsync();
            int bookingId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var booking = new BookingRequest
                {
                    BookingCode = await _codeGenerator.NextAsync(CodePrefixes.Booking),
                    ParentBookingId = parent.BookingId,
                    IsFreeOfCharge = free,
                    CustomerUserId = parent.CustomerUserId,
                    PickupAddress = $"{tripHorse.VetClinicName}, {tripHorse.VetClinicAddress}",
                    PickupCountryCode = model.PickupCountryCode,
                    PickupLat = tripHorse.VetClinicLat,
                    PickupLng = tripHorse.VetClinicLng,
                    DropoffAddress = parent.DropoffAddress,
                    DropoffCountryCode = parent.DropoffCountryCode,
                    DropoffLat = parent.DropoffLat,
                    DropoffLng = parent.DropoffLng,
                    PickupWindowStart = model.PickupWindowStart,
                    PickupWindowEnd = model.PickupWindowEnd,
                    DeliveryDeadline = model.DeliveryDeadline,
                    EstimatedTransitHours = quote.EstimatedHours,
                    TotalHorses = 1,
                    SpecialInstructions = model.SpecialInstructions
                        ?? $"Continuation of {parent.BookingCode}: {horseName} recovered at {tripHorse.VetClinicName} ({incident.IncidentCode}).",
                    TransportMode = parent.TransportMode,
                    DistanceKm = quote.DistanceKm,
                    IsExpress = quote.IsExpress,
                    RequiresClimateControl = parent.RequiresClimateControl,
                    DeclaredValue = bookingHorse.DeclaredValue,
                    QuoteMin = free ? 0 : quote.QuoteMin,
                    QuoteMax = free ? 0 : quote.QuoteMax,
                    EstimatedCost = 0,
                    // Khách đã đồng ý chính sách ở đơn gốc (điều khoản 9.6 về đơn nối tiếp)
                    PolicyVersion = policy.PolicyVersion,
                    PolicyAcceptedAt = parent.PolicyAcceptedAt ?? DateTimeOffset.UtcNow,
                    CurrencyCode = quote.CurrencyCode,
                    QuoteBreakdown = _pricingService.SerializeLines(quote.Lines),
                    Status = BookingStatuses.Submitted,
                    RecipientName = parent.RecipientName,
                    RecipientPhone = parent.RecipientPhone,
                    RecipientEmail = parent.RecipientEmail,
                    RecipientIdNumber = parent.RecipientIdNumber,
                    RecipientIsCustomer = parent.RecipientIsCustomer,
                    TransitCountries = quote.TransitCountryCodes.Select((code, i) => new BookingTransitCountry { CountryCode = code, SequenceOrder = i + 1 }).ToList(),
                    BookingHorses = new List<BookingHorse>
                    {
                        new()
                        {
                            HorseId = bookingHorse.HorseId,
                            StallClass = bookingHorse.StallClass,
                            Notes = bookingHorse.Notes,
                            Status = BookingHorseStatuses.Pending,
                            DeclaredValue = bookingHorse.DeclaredValue,
                            AgreedValue = bookingHorse.AgreedValue,
                            ValueProofPath = bookingHorse.ValueProofPath,
                            InsurancePlan = bookingHorse.InsurancePlan
                        }
                    }
                };
                await _bookingRepo.AddAsync(booking);

                string priceText = free
                    ? "It is free of charge because the incident was our fault."
                    : "You will receive the final quote for this leg once the route is planned.";
                await _notificationService.SendAsync(parent.CustomerUserId,
                    $"Continuation booking {booking.BookingCode} for {horseName}",
                    $"{horseName} is fit to travel again. We created booking {booking.BookingCode} from {tripHorse.VetClinicName} to {parent.DropoffAddress}, linked to {parent.BookingCode}. {priceText}",
                    NotificationReferenceTypes.Booking, booking.BookingId, emailCustomers: true);
                await _notificationService.SendToRoleAsync(UserRoles.LogisticsManager,
                    $"Continuation booking {booking.BookingCode} waits for review",
                    $"{horseName} (from {parent.BookingCode}, incident {incident.IncidentCode}) goes from {tripHorse.VetClinicName} to {parent.DropoffAddress}.{(free ? " Free of charge (company fault)." : string.Empty)}",
                    NotificationReferenceTypes.Booking, booking.BookingId);
                return booking.BookingId;
            });
            return bookingId;
        }

        // Con ngựa của chuyến đã rời chuyến để ở lại phòng khám, kèm sự cố gốc
        private async Task<(TripIncident Incident, TripHorse TripHorse)> FindClinicHorseAsync(int tripId, int tripHorseId)
        {
            int? incidentId = await _incidentRepo.GetQueryable()
                .Where(i => i.TripId == tripId && i.AffectedTripHorseId == tripHorseId
                    && i.ResponseAction == IncidentResponseActions.OffloadHorse && i.OffloadOutcome == OffloadOutcomes.WaitRecover
                    && (i.Status == IncidentStatuses.Approved || i.Status == IncidentStatuses.Resolved))
                .OrderByDescending(i => i.IncidentId)
                .Select(i => (int?)i.IncidentId)
                .FirstOrDefaultAsync();
            if (incidentId == null)
            {
                throw new BusinessException($"Trip horse {tripHorseId} did not stay at a vet clinic on trip {tripId} (no approved WaitRecover plan).");
            }
            var incident = await GetTrackedIncidentAsync(incidentId.Value);
            var tripHorse = incident.Trip!.TripHorses.First(th => th.TripHorseId == tripHorseId);
            return (incident, tripHorse);
        }

        // Đoàn xe, người lập chuyến và quản lý (không gửi khách)
        private async Task NotifyStaffAsync(Trip trip, TripIncident incident, string title, string message)
        {
            await _notificationService.SendToUsersAsync(WithPlanner(CrewIds(trip), trip), title, message, NotificationReferenceTypes.Incident, incident.IncidentId);
            if (!trip.PlannedByUserId.HasValue)
            {
                await _notificationService.SendToRoleAsync(UserRoles.FleetCoordinator, title, message, NotificationReferenceTypes.Incident, incident.IncidentId);
            }
            await _notificationService.SendToRoleAsync(UserRoles.LogisticsManager, title, message, NotificationReferenceTypes.Incident, incident.IncidentId);
        }

        // Dựng tuyến về (gọi trong transaction): bỏ mọi mốc chưa tới, thêm các mốc đã đi theo chiều ngược, kết thúc ở điểm xuất phát
        // (thành điểm giao cuối, e-POD ký ở đó). Ngựa chưa được đón rời chuyến. Lưu tỉ lệ quãng đường chưa đi của từng đơn để hoàn tiền.
        private async Task ApplyReturnToOriginAsync(Trip trip, TripIncident incident)
        {
            var route = trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
            var returnPlan = ReturnRouteRules.Build(route, DateTimeOffset.UtcNow);

            // Tỉ lệ chưa đi tính trên lộ trình trước khi đổi
            var ratios = trip.TripHorses
                .Select(th => th.BookingHorse!.BookingId)
                .Distinct()
                .ToDictionary(
                    bookingId => bookingId,
                    bookingId => ReturnRouteRules.UnusedRatio(route,
                        TripRouteRules.PickupCheckpoint(route, bookingId),
                        TripRouteRules.DropoffCheckpoint(route, bookingId),
                        returnPlan.TurnAround));

            var added = returnPlan.NewStops.Select(stop => new RouteCheckpoint
            {
                TripId = trip.TripId,
                CheckpointName = stop.Name.Length > 100 ? stop.Name[..100] : stop.Name,
                CheckpointType = stop.CheckpointType,
                Address = stop.Address,
                Latitude = stop.Latitude,
                Longitude = stop.Longitude,
                DistanceKmFromPrevious = stop.DistanceKmFromPrevious,
                PlannedTime = stop.PlannedTime,
                MandatoryRestMinutes = stop.MandatoryRestMinutes,
                Status = CheckpointStatuses.Pending,
                IsActive = true
            }).ToList();
            var newRoute = route.Except(returnPlan.Deactivate).ToList();
            newRoute.InsertRange(newRoute.IndexOf(returnPlan.TurnAround) + 1, added);
            var plan = new RoutePlan { Removed = returnPlan.Deactivate, Added = added, Anchor = returnPlan.TurnAround, NewRoute = newRoute };

            await ApplyRoutePlanAsync(trip, plan);

            // Ngựa chưa lên xe (mốc đón của đơn đã bị bỏ) rời chuyến, được xếp chuyến khác
            foreach (var tripHorse in trip.TripHorses.Where(th => th.Status == TripHorseStatuses.Planned))
            {
                tripHorse.Status = TripHorseStatuses.Cancelled;
            }
            await _tripRepo.SaveChangesAsync();

            var change = ToRouteChange(plan, returnPlan.TurnAround.CheckpointId);
            change.AppliedAt = DateTimeOffset.UtcNow;
            change.ReturnUnusedRatios = ratios;
            change.TurnAroundCheckpointId = returnPlan.TurnAround.CheckpointId;
            incident.RouteChange = IncidentMapper.SerializeRouteChange(change);
        }

        private async Task RefundReturnedBookingsAsync(TripIncident incident, string faultParty, CurrentUser currentUser)
        {
            var ratios = IncidentMapper.ParseRouteChange(incident.RouteChange)?.ReturnUnusedRatios;
            if (ratios == null || ratios.Count == 0)
            {
                return;
            }
            foreach (var (bookingId, ratio) in ratios)
            {
                var booking = await _bookingRepo.GetQueryable().FirstOrDefaultAsync(b => b.BookingId == bookingId);
                if (booking == null)
                {
                    continue;
                }
                decimal refundable = await _paymentService.GetRefundableAmountAsync(bookingId);
                decimal freight = DelayCompensationRules.FreightAmount(_pricingService.ParseLines(booking.QuoteBreakdown), booking.EstimatedCost);
                decimal amount = ReturnRouteRules.RefundAmount(faultParty, freight, refundable, ratio);
                if (amount > 0)
                {
                    await _paymentService.RefundAmountAsync(booking, amount,
                        $"Return to origin (incident {incident.IncidentCode}, {faultParty}): {ratio:P0} of the journey not travelled", currentUser);
                }
            }
        }

        private async Task RefundOffloadedHorseAsync(Trip trip, TripIncident incident, string faultParty, CurrentUser currentUser)
        {
            var tripHorse = trip.TripHorses.FirstOrDefault(th => th.TripHorseId == incident.AffectedTripHorseId);
            if (tripHorse?.OffloadedAt == null)
            {
                return;
            }
            var booking = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses)
                .FirstAsync(b => b.BookingId == tripHorse.BookingHorse!.BookingId);
            var route = trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
            // Nơi dỡ ngựa: mốc gần nhất đoàn xe đã tới lúc dỡ
            var offloadedAt = route.LastOrDefault(c => c.ActualArrivalTime != null && c.ActualArrivalTime <= tripHorse.OffloadedAt) ?? route.FirstOrDefault();
            if (offloadedAt == null)
            {
                return;
            }
            decimal ratio = ReturnRouteRules.UnusedRatio(route,
                TripRouteRules.PickupCheckpoint(route, booking.BookingId),
                TripRouteRules.DropoffCheckpoint(route, booking.BookingId),
                offloadedAt);
            decimal freight = DelayCompensationRules.FreightAmount(_pricingService.ParseLines(booking.QuoteBreakdown), booking.EstimatedCost);
            int horses = booking.BookingHorses.Count(bh => bh.Status != BookingHorseStatuses.Cancelled || bh.BookingHorseId == tripHorse.BookingHorseId);
            decimal refundable = await _paymentService.GetRefundableAmountAsync(booking.BookingId);
            decimal amount = ReturnRouteRules.OffloadRefundAmount(faultParty, freight, horses, ratio, refundable);
            if (amount > 0)
            {
                await _paymentService.RefundAmountAsync(booking, amount,
                    $"Unused fare of {tripHorse.BookingHorse!.Horse?.Name} (incident {incident.IncidentCode}, {faultParty}): {ratio:P0} of the journey not travelled", currentUser);
            }
        }

        private async Task NotifyEveryoneAsync(Trip trip, TripIncident incident, string title, string message)
        {
            await _notificationService.SendToUsersAsync(WithPlanner(CrewIds(trip), trip), title, message, NotificationReferenceTypes.Incident, incident.IncidentId);
            await _notificationService.SendToUsersAsync(await GetCustomerIdsAsync(trip.TripId), title, message, NotificationReferenceTypes.Incident, incident.IncidentId, emailCustomers: true);
            await _notificationService.SendToRoleAsync(UserRoles.LogisticsManager, title, message, NotificationReferenceTypes.Incident, incident.IncidentId);
        }

        private static List<int> OnBoardCustomerIds(Trip trip) =>
            trip.TripHorses.Where(th => TripHorseStatuses.OnBoard.Contains(th.Status))
                .Select(th => th.BookingHorse!.Booking!.CustomerUserId)
                .Distinct()
                .ToList();

        // =============================================================================================
        // Ngựa rời chuyến qua sự cố (R-12)
        // =============================================================================================

        private const int OffloadReasonMaxLength = 300; // độ dài cột trip_horses.offload_reason

        // Con ngựa của phương án OffloadHorse: chọn trong request, không có thì lấy con đã chọn lúc báo sự cố
        private static TripHorse ResolveOffloadHorse(TripIncident incident, int? requestedTripHorseId)
        {
            var trip = incident.Trip!;
            int? tripHorseId = requestedTripHorseId ?? incident.AffectedTripHorseId;
            if (!tripHorseId.HasValue)
            {
                throw new BusinessException("Choose the horse that leaves the trip (AffectedTripHorseId).");
            }
            if (incident.EmergencyOffload && tripHorseId != incident.AffectedTripHorseId)
            {
                throw new BusinessException("This incident is about the horse offloaded in an emergency; the plan must be for that horse.");
            }

            var tripHorse = trip.TripHorses.FirstOrDefault(th => th.TripHorseId == tripHorseId.Value)
                ?? throw new BusinessException($"Trip horse with ID {tripHorseId} is not on trip {trip.TripCode}.");
            if (!OffloadRules.CanBeOffloaded(tripHorse, incident))
            {
                throw new BusinessException($"{tripHorse.BookingHorse!.Horse!.Name} cannot leave the trip (status: {tripHorse.Status}).");
            }
            return tripHorse;
        }

        // Duyệt phương án OffloadHorse (gọi trong transaction): đổi trạng thái con ngựa theo hướng xử lý và báo các bên
        private async Task ApplyOffloadAsync(Trip trip, TripIncident incident, TripHorse tripHorse)
        {
            string outcome = incident.OffloadOutcome!;
            var bookingHorse = tripHorse.BookingHorse!;
            var booking = bookingHorse.Booking!;
            string horseName = bookingHorse.Horse!.Name;

            tripHorse.Status = OffloadRules.StatusFor(outcome);
            tripHorse.OffloadOutcome = outcome;
            tripHorse.OffloadedAt ??= DateTimeOffset.UtcNow;
            tripHorse.OffloadReason ??= Truncate($"{incident.IncidentCode}: {incident.Description}", OffloadReasonMaxLength);
            var clinic = IncidentMapper.ParseActionPayload(incident.ActionPayload)?.VetClinic;
            if (outcome == OffloadOutcomes.WaitRecover && clinic != null)
            {
                tripHorse.VetClinicName = clinic.Name;
                tripHorse.VetClinicAddress = clinic.Address;
                tripHorse.VetClinicPhone = clinic.Phone;
                tripHorse.AttendingVetName = clinic.AttendingVetName;
                tripHorse.VetClinicLat = clinic.Latitude;
                tripHorse.VetClinicLng = clinic.Longitude;
            }
            if (outcome == OffloadOutcomes.Deceased)
            {
                bookingHorse.Status = BookingHorseStatuses.Deceased;
            }
            // R-13: ngựa chết, hoặc phải rời chuyến vì sức khỏe -> tự mở hồ sơ bồi thường
            if (outcome == OffloadOutcomes.Deceased)
            {
                await _claimService.OpenAutomaticallyAsync(bookingHorse.BookingHorseId, ClaimTypes.Death,
                    $"{horseName} died during trip {trip.TripCode} ({incident.IncidentCode}).", incident.IncidentId);
            }
            else if (incident.IncidentType == IncidentTypes.EquineHealthIssue)
            {
                await _claimService.OpenAutomaticallyAsync(bookingHorse.BookingHorseId, ClaimTypes.Injury,
                    $"{horseName} left trip {trip.TripCode} for health reasons ({incident.IncidentCode}): {incident.Description}", incident.IncidentId);
            }
            // R-14: phần cước chưa đi của con ngựa được hoàn khi đóng sự cố (biết nguyên nhân)

            var (title, message) = outcome switch
            {
                OffloadOutcomes.WaitRecover => (
                    $"{horseName} left trip {trip.TripCode} to recover",
                    $"The horse stays under veterinary care at {(clinic == null ? incident.Location : $"{clinic.Name}, {clinic.Address}{(clinic.Phone == null ? string.Empty : $" (phone {clinic.Phone})")}{(clinic.AttendingVetName == null ? string.Empty : $", vet {clinic.AttendingVetName}")}")}. "
                    + "Booking " + booking.BookingCode + " ends for this horse at the clinic and the unused part of the fare is refunded according to the cause (service policy 9.6). "
                    + "Once the horse is fit to travel, our coordinator creates a continuation booking from the clinic to the destination (free of charge if the incident was our fault), or you may collect the horse at the clinic."),
                OffloadOutcomes.ReturnToOrigin => (
                    $"{horseName} will be returned to the pickup point",
                    $"The horse left trip {trip.TripCode} at {incident.Location} and our team will bring it back to {booking.PickupAddress}. Refunds and costs follow section 9.1 of the service policy."),
                OffloadOutcomes.HandOverHere => (
                    $"{horseName} can be collected at {incident.Location}",
                    "Please send the recipient there to sign the handover (e-POD) with our crew. The unused part of the fare follows section 9.1 of the service policy."),
                _ when incident.EmergencyAuthorizedAt != null => (
                    $"We are deeply sorry: {horseName} has died",
                    $"We could not reach you in time, so under the emergency authorisation clause (service policy 9.6) {horseName} was put down on the veterinarian's advice to spare it further suffering (trip {trip.TripCode}). The vet report is in the app and our team will contact you about the insurance claim.")
                ,
                _ when incident.RequiresEuthanasiaConsent => (
                    $"We are deeply sorry: {horseName} has died",
                    $"As you agreed, {horseName} was put down on the veterinarian's advice (trip {trip.TripCode}). The vet report is in the app and our team will contact you about the insurance claim.")
                ,
                _ => (
                    $"We are deeply sorry: {horseName} has died",
                    $"The horse died during trip {trip.TripCode}. A veterinary report has been recorded and our team will contact you about the insurance claim.")
            };
            await _notificationService.SendAsync(
                booking.CustomerUserId, title, message, NotificationReferenceTypes.Booking, booking.BookingId, emailCustomers: true);

            if (outcome == OffloadOutcomes.WaitRecover)
            {
                string planTitle = $"{horseName} stays at a vet clinic";
                string planMessage = $"Booking {booking.BookingCode}: the horse left trip {trip.TripCode} to recover at {clinic?.Name ?? incident.Location}. Create a continuation booking once it is fit to travel (attach the FitToTravel certificate to the incident).";
                if (trip.PlannedByUserId.HasValue)
                {
                    await _notificationService.SendAsync(trip.PlannedByUserId.Value, planTitle, planMessage, NotificationReferenceTypes.Trip, trip.TripId);
                }
                else
                {
                    await _notificationService.SendToRoleAsync(UserRoles.FleetCoordinator, planTitle, planMessage, NotificationReferenceTypes.Trip, trip.TripId);
                }
            }
        }

        // Khách: phần phí đi vòng của mình = phí cả xe × số ngựa của mình còn trên xe / tổng số ngựa còn trên xe
        private async Task FillDetourShareAsync(IEnumerable<IncidentModel> models, CurrentUser currentUser)
        {
            if (!currentUser.IsCustomer)
            {
                return;
            }
            var withCost = models.Where(m => m.ExtraCostForCustomer > 0).ToList();
            if (withCost.Count == 0)
            {
                return;
            }
            var tripIds = withCost.Select(m => m.TripId).Distinct().ToList();
            var counts = await _tripHorseRepo.GetQueryable().AsNoTracking()
                .Where(th => tripIds.Contains(th.TripId) && TripHorseStatuses.OnBoard.Contains(th.Status))
                .GroupBy(th => th.TripId)
                .Select(g => new
                {
                    TripId = g.Key,
                    Total = g.Count(),
                    Mine = g.Count(th => th.BookingHorse!.Booking!.CustomerUserId == currentUser.UserId)
                })
                .ToListAsync();
            foreach (var m in withCost)
            {
                var c = counts.FirstOrDefault(x => x.TripId == m.TripId);
                if (c == null)
                {
                    continue;
                }
                m.MyHorsesOnBoard = c.Mine;
                m.HorsesOnBoard = c.Total;
                m.MyExtraCostShare = DetourShareRules.Share(m.ExtraCostForCustomer, c.Mine, c.Total);
            }
        }

        private async Task ResolveVetReportUrlsAsync(IEnumerable<IncidentModel> models)
        {
            // An tử: hạn chờ chủ ngựa trả lời (sau mốc này mới được ủy quyền khẩn cấp)
            var waiting = models.Where(m => m.PendingConsentType == ConsentTypes.Euthanasia && m.ConsentRequestedAt.HasValue).ToList();
            if (waiting.Count > 0)
            {
                int minutes = (await _settingService.GetPolicySettingsAsync()).EuthanasiaConsentMinutes;
                waiting.ForEach(m => m.ConsentDeadline = EuthanasiaConsentRules.Deadline(m.ConsentRequestedAt!.Value, minutes));
            }
            await _fileStorage.ResolveViewUrlsAsync(models, m => m.VetReportPath, (m, url) => m.VetReportUrl = url);
            var docs = models.SelectMany(m => m.Documents).ToList();
            await _fileStorage.ResolveViewUrlsAsync(docs, d => d.FilePath, (d, url) => d.FileUrl = url);
        }

        // Cùng người báo, cùng chuyến, cùng loại / mức độ / ngựa / mô tả trong vài phút = một lần báo bị gửi lặp.
        // Không so vị trí vì GPS lấy lại mỗi lần mở form nên lệch vài mét.
        private static readonly TimeSpan DuplicateReportWindow = TimeSpan.FromMinutes(5);

        private async Task<int?> FindDuplicateReportAsync(int tripId, ReportIncidentModel model, CurrentUser currentUser)
        {
            var since = DateTimeOffset.UtcNow - DuplicateReportWindow;
            var description = model.Description.Trim();
            return await _incidentRepo.GetQueryable().AsNoTracking()
                .Where(i => i.TripId == tripId
                    && i.ReportedByUserId == currentUser.UserId
                    && i.ReportedAt >= since
                    && i.IncidentType == model.IncidentType
                    && i.InitialSeverity == model.Severity
                    && i.AffectedTripHorseId == model.AffectedTripHorseId
                    && i.EmergencyOffload == model.EmergencyOffload
                    && i.Description.Trim() == description)
                .OrderByDescending(i => i.IncidentId)
                .Select(i => (int?)i.IncidentId)
                .FirstOrDefaultAsync();
        }

        private static string Truncate(string value, int maxLength)
        {
            value = value.Trim();
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        private static IncidentActionPayloadModel? BuildActionPayload(ProposeIncidentPlanModel model)
        {
            bool hasVehicle = model.ReplacementVehicleId.HasValue;
            bool hasCrew = model.RemoveUserId.HasValue || model.AddUserId.HasValue || !string.IsNullOrWhiteSpace(model.CrewRole);

            if (model.ResponseAction == IncidentResponseActions.ReplaceVehicle)
            {
                if (hasCrew)
                {
                    throw new BusinessException("ReplaceVehicle does not accept RemoveUserId, AddUserId, or CrewRole.");
                }
                return new IncidentActionPayloadModel { ReplacementVehicleId = model.ReplacementVehicleId };
            }
            if (model.ResponseAction == IncidentResponseActions.ReplaceCrew)
            {
                if (hasVehicle)
                {
                    throw new BusinessException("ReplaceCrew does not accept ReplacementVehicleId.");
                }
                return new IncidentActionPayloadModel
                {
                    RemoveUserId = model.RemoveUserId,
                    AddUserId = model.AddUserId,
                    CrewRole = model.CrewRole
                };
            }
            if (hasVehicle || hasCrew)
            {
                throw new BusinessException("ReplacementVehicleId, RemoveUserId, AddUserId, and CrewRole are only used with ReplaceVehicle or ReplaceCrew.");
            }
            return null;
        }

        private async Task<ReplacementPlan?> ValidateReplacementPlanAsync(
            Trip trip,
            string responseAction,
            IncidentActionPayloadModel? payload)
        {
            if (responseAction == IncidentResponseActions.ReplaceVehicle)
            {
                int replacementId = payload?.ReplacementVehicleId
                    ?? throw new BusinessException("ReplacementVehicleId is required for a ReplaceVehicle plan.");
                if (payload.RemoveUserId.HasValue || payload.AddUserId.HasValue || !string.IsNullOrWhiteSpace(payload.CrewRole))
                {
                    throw new BusinessException("The stored ReplaceVehicle payload contains crew replacement fields. Propose the plan again.");
                }

                var current = trip.Vehicle ?? await _assetRepo.GetByIdAsync(trip.VehicleId)
                    ?? throw new BusinessException($"Current vehicle with ID {trip.VehicleId} was not found.");
                var replacement = await _assetRepo.GetByIdAsync(replacementId)
                    ?? throw new BusinessException($"Replacement vehicle with ID {replacementId} was not found.");
                var horsesToCarry = trip.TripHorses.Where(th => TripHorseStatuses.OnBoard.Contains(th.Status)).ToList();
                bool requiresClimate = horsesToCarry.Any(th => th.BookingHorse?.Booking?.RequiresClimateControl == true);
                IncidentReplacementRules.ValidateVehicle(current, replacement, horsesToCarry.Select(StallClassOf).ToList(), requiresClimate);

                string? conflictingTrip = await _tripRepo.GetQueryable()
                    .Where(t => t.TripId != trip.TripId
                        && t.VehicleId == replacementId
                        && t.OverallStatus != TripStatuses.Completed
                        && t.OverallStatus != TripStatuses.Cancelled
                        && t.PlannedStartDate < trip.PlannedEndDate
                        && t.PlannedEndDate > trip.PlannedStartDate)
                    .Select(t => t.TripCode)
                    .FirstOrDefaultAsync();
                if (conflictingTrip != null)
                {
                    throw new BusinessException($"Replacement vehicle {replacement.AssetCode} is already assigned to trip {conflictingTrip} during this period.");
                }

                return new ReplacementPlan(payload, replacement, null, null);
            }

            if (responseAction == IncidentResponseActions.ReplaceCrew)
            {
                int removeUserId = payload?.RemoveUserId
                    ?? throw new BusinessException("RemoveUserId is required for a ReplaceCrew plan.");
                int addUserId = payload.AddUserId
                    ?? throw new BusinessException("AddUserId is required for a ReplaceCrew plan.");
                string crewRole = string.IsNullOrWhiteSpace(payload.CrewRole)
                    ? throw new BusinessException("CrewRole is required for a ReplaceCrew plan.")
                    : payload.CrewRole;
                if (payload.ReplacementVehicleId.HasValue)
                {
                    throw new BusinessException("The stored ReplaceCrew payload contains ReplacementVehicleId. Propose the plan again.");
                }

                var replacement = await _userRepo.GetByIdAsync(addUserId)
                    ?? throw new BusinessException($"Replacement crew member with user ID {addUserId} was not found.");
                string? conflictingTrip = await _tripRepo.GetQueryable()
                    .Where(t => t.TripId != trip.TripId
                        && t.OverallStatus != TripStatuses.Completed
                        && t.OverallStatus != TripStatuses.Cancelled
                        && t.PlannedStartDate < trip.PlannedEndDate
                        && t.PlannedEndDate > trip.PlannedStartDate
                        && t.Crew.Any(c => c.UserId == addUserId))
                    .Select(t => t.TripCode)
                    .FirstOrDefaultAsync();
                var removed = IncidentReplacementRules.ValidateCrew(trip.Crew.ToList(), removeUserId, replacement, crewRole, conflictingTrip);
                return new ReplacementPlan(payload, null, removed, replacement);
            }

            // Payload chỉ chứa phòng khám (OffloadHorse + WaitRecover) thì không phải phương án đổi xe / đổi người
            if (payload != null && (payload.ReplacementVehicleId.HasValue || payload.RemoveUserId.HasValue || payload.AddUserId.HasValue || payload.CrewRole != null))
            {
                throw new BusinessException("This incident action must not contain an action payload. Propose the plan again.");
            }
            return null;
        }

        private static string StallClassOf(TripHorse tripHorse) => tripHorse.BookingHorse?.StallClass ?? StallClasses.Shared;

        private async Task ApplyReplacementAsync(Trip trip, TripIncident incident, ReplacementPlan plan)
        {
            if (plan.Vehicle != null)
            {
                var oldVehicle = trip.Vehicle ?? await _assetRepo.GetByIdAsync(trip.VehicleId)
                    ?? throw new BusinessException($"Current vehicle with ID {trip.VehicleId} was not found.");
                var horsesToCarry = trip.TripHorses
                    .Where(th => TripHorseStatuses.OnBoard.Contains(th.Status))
                    .OrderBy(th => th.StallSlotNumber)
                    .ThenBy(th => th.TripHorseId)
                    .ToList();

                // Xe mới ít khoang hơn: xếp lại ngựa còn trên xe theo hạng chuồng từ khoang 1,
                // ngựa đã rời xe xếp tiếp phía sau chỉ để giữ ô chuồng không trùng.
                if (horsesToCarry.Any(th => th.CompartmentNo > plan.Vehicle.CompartmentCount))
                {
                    var remaining = trip.TripHorses.Except(horsesToCarry)
                        .OrderBy(th => th.StallSlotNumber)
                        .ThenBy(th => th.TripHorseId)
                        .ToList();
                    var onBoardPlaces = CompartmentRules.Pack(horsesToCarry.Select(StallClassOf).ToList());
                    int nextCompartment = onBoardPlaces.Count == 0 ? 1 : onBoardPlaces.Max(p => p.CompartmentNo) + 1;
                    var places = onBoardPlaces.Concat(CompartmentRules.Pack(remaining.Select(StallClassOf).ToList(), nextCompartment)).ToList();
                    var all = horsesToCarry.Concat(remaining).ToList();
                    foreach (var horse in all)
                    {
                        horse.StallSlotNumber = 100000 + horse.TripHorseId;
                        await _tripHorseRepo.UpdateAsync(horse);
                    }
                    for (int i = 0; i < all.Count; i++)
                    {
                        all[i].CompartmentNo = places[i].CompartmentNo;
                        all[i].PositionNo = places[i].PositionNo;
                        all[i].StallSlotNumber = CompartmentRules.ToStallSlot(places[i].CompartmentNo, places[i].PositionNo);
                        await _tripHorseRepo.UpdateAsync(all[i]);
                    }
                }

                oldVehicle.Status = AssetStatuses.Maintenance;
                plan.Vehicle.Status = AssetStatuses.InTransit;
                trip.VehicleId = plan.Vehicle.AssetId;
                trip.Vehicle = plan.Vehicle;
                await _assetRepo.UpdateAsync(oldVehicle);
                await _assetRepo.UpdateAsync(plan.Vehicle);
                await _tripRepo.UpdateAsync(trip);
            }

            if (plan.RemovedCrew != null && plan.AddedUser != null)
            {
                var removed = plan.RemovedCrew;
                var added = new TripCrew
                {
                    TripId = trip.TripId,
                    UserId = plan.AddedUser.UserId,
                    CrewRole = removed.CrewRole,
                    IsLead = removed.IsLead,
                    Duty = removed.Duty
                };

                if (removed.CrewRole == CrewRoles.Escort)
                {
                    foreach (var horse in trip.TripHorses.Where(th => th.EscortUserId == removed.UserId))
                    {
                        horse.EscortUserId = plan.AddedUser.UserId;
                        await _tripHorseRepo.UpdateAsync(horse);
                    }
                }

                // Thông báo ghi tên người cũ, không ghi mã số
                string removedName = (await _userRepo.GetByIdAsync(removed.UserId))?.FullName ?? $"user {removed.UserId}";
                await _crewRepo.DeleteAsync(removed);
                await _crewRepo.AddAsync(added);
                await _notificationService.SendToUsersAsync(
                    new[] { removed.UserId, plan.AddedUser.UserId },
                    $"Crew replacement on trip {trip.TripCode}",
                    $"Incident {incident.IncidentCode}: {removed.CrewRole} assignment changed from {removedName} to {plan.AddedUser.FullName}.",
                    NotificationReferenceTypes.Incident,
                    incident.IncidentId);
            }
        }

        private sealed record ReplacementPlan(
            IncidentActionPayloadModel Payload,
            TransportAsset? Vehicle,
            TripCrew? RemovedCrew,
            User? AddedUser);

        // ---------------------------------------------------------------------------------------------

        private IQueryable<TripIncident> QueryIncidents()
        {
            return _incidentRepo.GetQueryable()
                .AsNoTracking()
                .Include(i => i.Trip)
                .Include(i => i.ReportedBy)
                .Include(i => i.ApprovedBy)
                .Include(i => i.EmergencyAuthorizedBy)
                .Include(i => i.AffectedTripHorse!).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(i => i.Consents).ThenInclude(c => c.Booking)
                .Include(i => i.Documents).ThenInclude(d => d.UploadedBy);
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
                .Include(i => i.Trip!).ThenInclude(t => t.Vehicle)
                .Include(i => i.Trip!).ThenInclude(t => t.Crew)
                .Include(i => i.Trip!).ThenInclude(t => t.Checkpoints)
                .Include(i => i.Trip!).ThenInclude(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(i => i.Trip!).ThenInclude(t => t.TripHorses).ThenInclude(th => th.BookingHorse!).ThenInclude(bh => bh.Booking)
                .Include(i => i.Consents)
                .Include(i => i.Documents).ThenInclude(d => d.UploadedBy)
                .AsSplitQuery()
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

        // Email gửi khách viết tiếng Việt
        private static string SeverityLabel(string severity) => severity switch
        {
            IncidentSeverities.Minor => "Nhẹ",
            IncidentSeverities.Moderate => "Trung bình",
            IncidentSeverities.Major => "Nghiêm trọng",
            IncidentSeverities.Critical => "Rất nghiêm trọng",
            _ => severity
        };

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
