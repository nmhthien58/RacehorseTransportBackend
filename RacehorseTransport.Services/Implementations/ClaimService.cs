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
    // R-13 (BE-12, Chính sách mục 8): hồ sơ bồi thường bảo hiểm.
    //   Tự mở: ngựa chết, ngựa rời chuyến vì sức khỏe, sự cố Critical gắn với một con ngựa, e-POD ghi Injured,
    //   người nhận từ chối vì ngựa bị thương / nhầm ngựa. Khách tự mở trong 24 giờ sau khi nhận ngựa.
    //   Open -> UnderReview -> Approved (≤ trần của gói) / Rejected -> Paid (VNPay tới mức khách đã trả, phần vượt chuyển khoản thủ công).
    public class ClaimService : IClaimService
    {
        private readonly IRepository<InsuranceClaim> _claimRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<BookingHorse> _bookingHorseRepo;
        private readonly IRepository<TripIncident> _incidentRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IPaymentService _paymentService;
        private readonly ISystemSettingService _settingService;
        private readonly IFileStorageService _fileStorage;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;

        public ClaimService(
            IRepository<InsuranceClaim> claimRepo,
            IRepository<BookingRequest> bookingRepo,
            IRepository<BookingHorse> bookingHorseRepo,
            IRepository<TripIncident> incidentRepo,
            IRepository<TripHorse> tripHorseRepo,
            IPaymentService paymentService,
            ISystemSettingService settingService,
            IFileStorageService fileStorage,
            ICodeGenerator codeGenerator,
            INotificationService notificationService)
        {
            _claimRepo = claimRepo;
            _bookingRepo = bookingRepo;
            _bookingHorseRepo = bookingHorseRepo;
            _incidentRepo = incidentRepo;
            _tripHorseRepo = tripHorseRepo;
            _paymentService = paymentService;
            _settingService = settingService;
            _fileStorage = fileStorage;
            _codeGenerator = codeGenerator;
            _notificationService = notificationService;
        }

        // =============================================================================================
        // Mở hồ sơ
        // =============================================================================================

        public async Task OpenAutomaticallyAsync(int bookingHorseId, string claimType, string description, int? incidentId = null, int? deliveryFailureId = null)
        {
            // Một sự cố / một lần không giao được chỉ mở một hồ sơ cho mỗi con ngựa
            var existing = await _claimRepo.GetQueryable()
                .Where(c => c.BookingHorseId == bookingHorseId
                    && c.Status != ClaimStatuses.Rejected
                    && ((incidentId != null && c.IncidentId == incidentId)
                        || (deliveryFailureId != null && c.DeliveryFailureId == deliveryFailureId)
                        || (incidentId == null && deliveryFailureId == null && c.ClaimType == claimType)))
                .FirstOrDefaultAsync();
            if (existing != null)
            {
                // Sự cố Critical đã mở hồ sơ thương tích, sau đó ngựa chết -> nâng thành hồ sơ tử vong nếu chưa quyết định
                if (claimType == ClaimTypes.Death && existing.ClaimType == ClaimTypes.Injury && ClaimStatuses.Reviewable.Contains(existing.Status))
                {
                    existing.ClaimType = ClaimTypes.Death;
                    string merged = $"{existing.Description} | {description}";
                    existing.Description = merged.Length > 2000 ? merged[..2000] : merged;
                    await _claimRepo.SaveChangesAsync();
                }
                return;
            }
            var bookingHorse = await LoadBookingHorseAsync(bookingHorseId);
            var claim = await CreateAsync(bookingHorse, claimType, description, null, null, incidentId, deliveryFailureId, new List<string>());
            await _notificationService.SendAsync(
                bookingHorse.Booking!.CustomerUserId,
                $"Insurance claim {claim.ClaimCode} opened for {bookingHorse.Horse?.Name}",
                $"A claim was opened automatically ({claimType}). Upload the vet report, photos and vet invoices in the app; we answer within 7 working days.",
                NotificationReferenceTypes.Booking,
                bookingHorse.BookingId,
                emailCustomers: true);
        }

        public async Task<ClaimModel> OpenByCustomerAsync(int bookingId, OpenClaimModel model, CurrentUser currentUser)
        {
            var bookingHorse = await _bookingHorseRepo.GetQueryable()
                .Include(bh => bh.Booking)
                .Include(bh => bh.Horse)
                .FirstOrDefaultAsync(bh => bh.BookingHorseId == model.BookingHorseId && bh.BookingId == bookingId)
                ?? throw new BusinessException($"Booking horse {model.BookingHorseId} is not part of booking {bookingId}.");
            if (bookingHorse.Booking!.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only open a claim for your own bookings.");
            }
            if (!ClaimTypes.All.Contains(model.ClaimType))
            {
                throw new BusinessException($"ClaimType '{model.ClaimType}' is invalid. Allowed values: {string.Join(", ", ClaimTypes.All)}.");
            }
            var deliveredAt = await _tripHorseRepo.GetQueryable()
                .Where(th => th.BookingHorseId == bookingHorse.BookingHorseId && th.DeliveredAt != null)
                .MaxAsync(th => th.DeliveredAt);
            if (!ClaimRules.WithinCustomerWindow(deliveredAt, DateTimeOffset.UtcNow))
            {
                throw new BusinessException(deliveredAt == null
                    ? $"{bookingHorse.Horse?.Name} has not been delivered yet. Claims during the trip are opened by the company."
                    : $"Claims must be opened within 24 hours after the horse is received ({deliveredAt.Value.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy}, Vietnam time).");
            }
            var evidence = ValidEvidence(model.EvidencePaths);
            var claim = await CreateAsync(bookingHorse, model.ClaimType, model.Description.Trim(), model.RequestedAmount, currentUser.UserId, null, null, evidence);

            await _notificationService.SendToRoleAsync(
                UserRoles.LogisticsManager,
                $"New insurance claim {claim.ClaimCode}",
                $"Booking {bookingHorse.Booking.BookingCode}, {bookingHorse.Horse?.Name}: {model.ClaimType}. Review it within 7 working days.",
                NotificationReferenceTypes.Booking,
                bookingId);
            return await GetAsync(claim.ClaimId, currentUser);
        }

        public async Task<ClaimModel> AddEvidenceAsync(int claimId, List<string> paths, CurrentUser currentUser)
        {
            var claim = await GetTrackedAsync(claimId);
            EnsureCanView(claim, currentUser);
            if (claim.Status is ClaimStatuses.Paid or ClaimStatuses.Rejected)
            {
                throw new BusinessException($"The claim is already {claim.Status}.");
            }
            var current = JsonSerializer.Deserialize<List<string>>(claim.EvidencePaths) ?? new List<string>();
            current.AddRange(ValidEvidence(paths).Where(p => !current.Contains(p)));
            claim.EvidencePaths = JsonSerializer.Serialize(current);
            await _claimRepo.SaveChangesAsync();
            return await GetAsync(claimId, currentUser);
        }

        // =============================================================================================
        // Xem
        // =============================================================================================

        public async Task<ClaimModel> GetAsync(int claimId, CurrentUser currentUser)
        {
            var claim = await Query().FirstOrDefaultAsync(c => c.ClaimId == claimId)
                ?? throw new NotFoundException($"Claim with ID {claimId} was not found.");
            EnsureCanView(claim, currentUser);
            var model = await ToModelAsync(claim);
            var urls = await _fileStorage.GetViewUrlsAsync(model.EvidencePaths);
            model.EvidenceUrls = model.EvidencePaths.Select(p => urls.TryGetValue(p, out var u) ? u : null).Where(u => u != null).Select(u => u!).ToList();
            return model;
        }

        public async Task<List<ClaimModel>> ListAsync(string? status, int? bookingId, CurrentUser currentUser)
        {
            var query = Query();
            if (currentUser.IsCustomer)
            {
                query = query.Where(c => c.Booking!.CustomerUserId == currentUser.UserId);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }
            if (bookingId.HasValue)
            {
                query = query.Where(c => c.BookingId == bookingId);
            }
            var claims = await query.OrderByDescending(c => c.OpenedAt).Take(200).ToListAsync();
            var result = new List<ClaimModel>();
            foreach (var claim in claims)
            {
                result.Add(await ToModelAsync(claim));
            }
            return result;
        }

        // =============================================================================================
        // Quản lý xét duyệt, chi trả
        // =============================================================================================

        public async Task<ClaimModel> StartReviewAsync(int claimId, CurrentUser currentUser)
        {
            var claim = await GetTrackedAsync(claimId);
            if (claim.Status != ClaimStatuses.Open)
            {
                throw new BusinessException($"Only an open claim can be taken into review (current status: {claim.Status}).");
            }
            claim.Status = ClaimStatuses.UnderReview;
            claim.ReviewedByUserId = currentUser.UserId;
            await _claimRepo.SaveChangesAsync();
            return await GetAsync(claimId, currentUser);
        }

        public async Task<ClaimModel> ApproveAsync(int claimId, decimal approvedAmount, string? faultParty, string? reason, CurrentUser currentUser)
        {
            var claim = await GetTrackedAsync(claimId);
            if (!ClaimStatuses.Reviewable.Contains(claim.Status))
            {
                throw new BusinessException($"Only an open or reviewed claim can be decided (current status: {claim.Status}).");
            }
            if (faultParty != null && !FaultParties.All.Contains(faultParty))
            {
                throw new BusinessException($"FaultParty '{faultParty}' is invalid. Allowed values: {string.Join(", ", FaultParties.All)}.");
            }
            // Nguyên nhân: Manager chọn, nếu không thì lấy theo sự cố đã đóng
            string? fault = faultParty ?? claim.FaultParty;
            if (fault == null && claim.IncidentId.HasValue)
            {
                fault = await _incidentRepo.GetQueryable().Where(i => i.IncidentId == claim.IncidentId).Select(i => i.FaultParty).FirstOrDefaultAsync();
            }
            var policy = await _settingService.GetPolicySettingsAsync();
            ClaimRules.EnsureApprovable(claim.InsurancePlan, claim.ClaimType, claim.AgreedValue, fault, approvedAmount, policy);

            claim.FaultParty = fault;
            claim.ApprovedAmount = approvedAmount;
            claim.DecisionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            claim.Status = ClaimStatuses.Approved;
            claim.ReviewedByUserId = currentUser.UserId;
            claim.ReviewedAt = DateTimeOffset.UtcNow;
            await _claimRepo.SaveChangesAsync();
            await EmailDecisionAsync(claim, "Chấp nhận bồi thường");
            return await GetAsync(claimId, currentUser);
        }

        public async Task<ClaimModel> RejectAsync(int claimId, string reason, CurrentUser currentUser)
        {
            var claim = await GetTrackedAsync(claimId);
            if (!ClaimStatuses.Reviewable.Contains(claim.Status))
            {
                throw new BusinessException($"Only an open or reviewed claim can be decided (current status: {claim.Status}).");
            }
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new BusinessException("A reason is required to reject a claim (see the exclusions in section 8.4 of the service policy).");
            }
            claim.DecisionReason = reason.Trim();
            claim.Status = ClaimStatuses.Rejected;
            claim.ReviewedByUserId = currentUser.UserId;
            claim.ReviewedAt = DateTimeOffset.UtcNow;
            await _claimRepo.SaveChangesAsync();
            await EmailDecisionAsync(claim, "Từ chối bồi thường");
            return await GetAsync(claimId, currentUser);
        }

        public async Task<ClaimModel> PayAsync(int claimId, string? manualReference, CurrentUser currentUser)
        {
            var claim = await GetTrackedAsync(claimId);
            if (claim.Status != ClaimStatuses.Approved)
            {
                throw new BusinessException($"Only an approved claim can be paid (current status: {claim.Status}).");
            }
            var booking = await _bookingRepo.GetQueryable().FirstAsync(b => b.BookingId == claim.BookingId);
            await _paymentService.PayCompensationAsync(booking, claim.ApprovedAmount!.Value, manualReference,
                $"Insurance claim {claim.ClaimCode} ({claim.ClaimType})", currentUser);

            claim.Status = ClaimStatuses.Paid;
            claim.PaidAt = DateTimeOffset.UtcNow;
            claim.PaidReference = string.IsNullOrWhiteSpace(manualReference) ? null : manualReference.Trim();
            await _claimRepo.SaveChangesAsync();
            await _notificationService.SendAsync(
                booking.CustomerUserId,
                $"Compensation {claim.ClaimCode} has been paid",
                $"{claim.ApprovedAmount:N2} {booking.CurrencyCode} was sent to you (VNPay refund to your original payment, any amount above it by bank transfer).",
                NotificationReferenceTypes.Booking,
                booking.BookingId,
                emailCustomers: true);
            return await GetAsync(claimId, currentUser);
        }

        // ---------------------------------------------------------------------------------------------

        private async Task<InsuranceClaim> CreateAsync(BookingHorse bookingHorse, string claimType, string description, decimal? requested,
            int? openedBy, int? incidentId, int? deliveryFailureId, List<string> evidence)
        {
            var claim = new InsuranceClaim
            {
                ClaimCode = await _codeGenerator.NextAsync(CodePrefixes.Claim),
                BookingId = bookingHorse.BookingId,
                BookingHorseId = bookingHorse.BookingHorseId,
                IncidentId = incidentId,
                DeliveryFailureId = deliveryFailureId,
                ClaimType = claimType,
                InsurancePlan = string.IsNullOrWhiteSpace(bookingHorse.InsurancePlan) ? InsurancePlans.None : bookingHorse.InsurancePlan,
                AgreedValue = HorseValueRules.EffectiveValue(bookingHorse.AgreedValue, bookingHorse.DeclaredValue),
                Description = description.Length > 2000 ? description[..2000] : description,
                RequestedAmount = requested,
                Status = ClaimStatuses.Open,
                EvidencePaths = JsonSerializer.Serialize(evidence),
                OpenedByUserId = openedBy,
                OpenedAt = DateTimeOffset.UtcNow
            };
            await _claimRepo.AddAsync(claim);
            await _claimRepo.SaveChangesAsync();
            return claim;
        }

        private async Task EmailDecisionAsync(InsuranceClaim claim, string decision)
        {
            var bookingHorse = await LoadBookingHorseAsync(claim.BookingHorseId);
            await _notificationService.EmailCustomerAsync(
                bookingHorse.Booking!.CustomerUserId,
                EmailTemplateNames.ClaimDecision,
                CustomerPages.Booking(claim.BookingId),
                new Dictionary<string, string>
                {
                    ["ClaimCode"] = claim.ClaimCode,
                    ["BookingCode"] = bookingHorse.Booking.BookingCode,
                    ["HorseName"] = bookingHorse.Horse?.Name ?? string.Empty,
                    ["Decision"] = decision,
                    ["Amount"] = claim.ApprovedAmount.HasValue ? $"{claim.ApprovedAmount.Value.ToString("N2", CultureInfo.InvariantCulture)} USD" : "0 USD",
                    ["Reason"] = claim.DecisionReason ?? string.Empty
                });
            await _notificationService.SendAsync(
                bookingHorse.Booking.CustomerUserId,
                $"Decision on claim {claim.ClaimCode}",
                claim.Status == ClaimStatuses.Approved
                    ? $"Approved: {claim.ApprovedAmount:N2} USD. {claim.DecisionReason}"
                    : $"Rejected. Reason: {claim.DecisionReason}",
                NotificationReferenceTypes.Booking,
                claim.BookingId);
        }

        private List<string> ValidEvidence(IEnumerable<string>? paths)
        {
            var list = (paths ?? Enumerable.Empty<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).Distinct().ToList();
            if (list.Any(p => !_fileStorage.IsValidPath(p)))
            {
                throw new BusinessException("Each evidence path must come from POST /api/files (vet report, photos, vet invoices).");
            }
            return list;
        }

        private static void EnsureCanView(InsuranceClaim claim, CurrentUser currentUser)
        {
            if (currentUser.IsCustomer && claim.Booking!.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only see claims of your own bookings.");
            }
            if (currentUser.Role == UserRoles.DriverEscort)
            {
                throw new ForbiddenException("You cannot view insurance claims.");
            }
        }

        private IQueryable<InsuranceClaim> Query() =>
            _claimRepo.GetQueryable().AsNoTracking()
                .Include(c => c.Booking)
                .Include(c => c.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(c => c.ReviewedBy);

        private async Task<InsuranceClaim> GetTrackedAsync(int claimId) =>
            await _claimRepo.GetQueryable().Include(c => c.Booking).FirstOrDefaultAsync(c => c.ClaimId == claimId)
                ?? throw new NotFoundException($"Claim with ID {claimId} was not found.");

        private Task<BookingHorse> LoadBookingHorseAsync(int bookingHorseId) =>
            _bookingHorseRepo.GetQueryable().AsNoTracking()
                .Include(bh => bh.Booking)
                .Include(bh => bh.Horse)
                .FirstAsync(bh => bh.BookingHorseId == bookingHorseId);

        private async Task<ClaimModel> ToModelAsync(InsuranceClaim c)
        {
            var policy = await _settingService.GetPolicySettingsAsync();
            return new ClaimModel
            {
                ClaimId = c.ClaimId,
                ClaimCode = c.ClaimCode,
                BookingId = c.BookingId,
                BookingCode = c.Booking?.BookingCode ?? string.Empty,
                BookingHorseId = c.BookingHorseId,
                HorseName = c.BookingHorse?.Horse?.Name ?? string.Empty,
                IncidentId = c.IncidentId,
                DeliveryFailureId = c.DeliveryFailureId,
                ClaimType = c.ClaimType,
                InsurancePlan = c.InsurancePlan,
                AgreedValue = c.AgreedValue,
                FaultParty = c.FaultParty,
                // Trần ước tính: gói không mua mà chưa rõ lỗi công ty thì hiện trần khi lỗi công ty
                Cap = ClaimRules.Cap(c.InsurancePlan, c.ClaimType, c.AgreedValue, c.InsurancePlan == InsurancePlans.Full ? c.FaultParty : FaultParties.Company, policy),
                Description = c.Description,
                RequestedAmount = c.RequestedAmount,
                ApprovedAmount = c.ApprovedAmount,
                Status = c.Status,
                DecisionReason = c.DecisionReason,
                EvidencePaths = JsonSerializer.Deserialize<List<string>>(c.EvidencePaths) ?? new List<string>(),
                OpenedBySystem = c.OpenedByUserId == null,
                OpenedAt = c.OpenedAt,
                ReviewedByName = c.ReviewedBy?.FullName,
                ReviewedAt = c.ReviewedAt,
                PaidAt = c.PaidAt,
                PaidReference = c.PaidReference
            };
        }
    }
}
