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
    // R-17 (Chính sách mục 12): hợp đồng vận chuyển PDF.
    //   Đơn -> Quoted: sinh bản nháp (version 1, 2, ... mỗi lần báo giá đổi), khách xem PDF + mã băm
    //   -> accept-quote gửi contractId + contractSha256: bản nháp -> Accepted, lưu bằng chứng (người bấm, thời điểm, IP, User-Agent, phiên bản chính sách)
    //   -> đổi điều khoản sau đó: CreateAmendmentAsync sinh bản nháp mới, khách chấp nhận qua POST /api/contracts/{id}/accept; bản Accepted cũ -> Superseded
    public class ContractService : IContractService
    {
        private const int IpMaxLength = 64;
        private const int UserAgentMaxLength = 500;

        private readonly IRepository<BookingContract> _contractRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<RoutePlan> _planRepo;
        private readonly IPricingService _pricingService;
        private readonly ISystemSettingService _settingService;
        private readonly IFileStorageService _storage;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;

        public ContractService(
            IRepository<BookingContract> contractRepo,
            IRepository<BookingRequest> bookingRepo,
            IRepository<RoutePlan> planRepo,
            IPricingService pricingService,
            ISystemSettingService settingService,
            IFileStorageService storage,
            ICodeGenerator codeGenerator,
            INotificationService notificationService)
        {
            _contractRepo = contractRepo;
            _bookingRepo = bookingRepo;
            _planRepo = planRepo;
            _pricingService = pricingService;
            _settingService = settingService;
            _storage = storage;
            _codeGenerator = codeGenerator;
            _notificationService = notificationService;
        }

        // =============================================================================================
        // Sinh bản hợp đồng
        // =============================================================================================

        public Task<BookingContractModel> CreateDraftAsync(int bookingId, string reason) =>
            CreateVersionAsync(bookingId, reason, requireAccepted: false);

        public Task<BookingContractModel> CreateAmendmentAsync(int bookingId, string reason) =>
            CreateVersionAsync(bookingId, reason, requireAccepted: true);

        private async Task<BookingContractModel> CreateVersionAsync(int bookingId, string reason, bool requireAccepted)
        {
            if (!ContractReasons.All.Contains(reason))
            {
                throw new BusinessException($"Reason '{reason}' is invalid. Allowed values: {string.Join(", ", ContractReasons.All)}.");
            }

            var booking = await _bookingRepo.GetQueryable().AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Horse)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId)
                ?? throw new NotFoundException($"Booking with ID {bookingId} was not found.");
            if (booking.QuotedAt == null || booking.EstimatedCost <= 0)
            {
                throw new BusinessException($"Booking {booking.BookingCode} has no final quote yet, so no contract can be created.");
            }

            var existing = await _contractRepo.GetQueryable()
                .Where(c => c.BookingId == bookingId)
                .ToListAsync();
            if (requireAccepted && !existing.Any(c => c.Status == ContractStatuses.Accepted))
            {
                throw new BusinessException($"Booking {booking.BookingCode} has no accepted contract to amend.");
            }

            var plan = await _planRepo.GetQueryable().AsNoTracking()
                .Include(p => p.Stops)
                .Include(p => p.ReviewedBy)
                .Where(p => p.BookingId == bookingId && p.Status == RoutePlanStatuses.Approved)
                .FirstOrDefaultAsync();

            var payment = await _settingService.GetPaymentSettingsAsync();
            var policy = await _settingService.GetPolicySettingsAsync();
            var lines = _pricingService.ParseLines(booking.QuoteBreakdown);
            decimal depositPercent = booking.QuoteAcceptedAt != null ? booking.DepositPercent ?? payment.DepositPercent : payment.DepositPercent;
            decimal deposit = booking.QuoteAcceptedAt != null ? booking.DepositAmount : QuoteRules.DepositAmount(booking.EstimatedCost, depositPercent);

            int version = ContractRules.NextVersion(existing.Select(c => c.Version));
            string code = await _codeGenerator.NextAsync(CodePrefixes.Contract);
            var document = new ContractDocumentModel
            {
                ContractCode = code,
                Version = version,
                Reason = reason,
                // Bản đầu ghi ngày báo giá; bản sửa đổi ghi ngày phát sinh thay đổi
                IssuedAt = reason == ContractReasons.Initial ? booking.QuotedAt.Value : DateTimeOffset.UtcNow,
                PolicyVersion = policy.PolicyVersion,
                BookingCode = booking.BookingCode,
                CustomerName = booking.Customer?.FullName ?? string.Empty,
                CustomerEmail = booking.Customer?.Email ?? string.Empty,
                CustomerPhone = booking.Customer?.PhoneNumber,
                RecipientInfo = null, // TODO(BE-15): thông tin người nhận trên đơn
                TransportMode = booking.TransportMode,
                PickupAddress = booking.PickupAddress,
                PickupCountryCode = booking.PickupCountryCode,
                DropoffAddress = booking.DropoffAddress,
                DropoffCountryCode = booking.DropoffCountryCode,
                TransitCountryCodes = plan == null
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(plan.TransitCountryCodes) ?? new List<string>(),
                TotalDistanceKm = plan?.TotalDistanceKm ?? booking.DistanceKm ?? 0,
                Stops = plan?.Stops.OrderBy(s => s.SequenceOrder).Select(s => new ContractStopModel
                {
                    Name = s.StopName,
                    StopType = s.StopType,
                    Address = s.Address,
                    CountryCode = s.CountryCode
                }).ToList() ?? new List<ContractStopModel>(),
                PickupWindowStart = booking.PickupWindowStart,
                PickupWindowEnd = booking.PickupWindowEnd,
                DeliveryDeadline = booking.DeliveryDeadline,
                Horses = booking.BookingHorses
                    .Where(bh => bh.Status != BookingHorseStatuses.Cancelled)
                    .OrderBy(bh => bh.BookingHorseId)
                    .Select(bh => new ContractHorseModel
                    {
                        Name = bh.Horse?.Name ?? $"#{bh.HorseId}",
                        MicrochipNumber = bh.Horse?.MicrochipNumber ?? string.Empty,
                        StallClass = bh.StallClass,
                        AgreedValue = HorseValueRules.EffectiveValue(bh.AgreedValue, bh.DeclaredValue),
                        InsurancePlan = bh.InsurancePlan
                    }).ToList(),
                CurrencyCode = booking.CurrencyCode,
                Lines = lines,
                Total = booking.EstimatedCost,
                DepositPercent = depositPercent,
                DepositAmount = deposit,
                BalanceAmount = booking.EstimatedCost - deposit,
                DepositDueAt = booking.QuoteExpiresAt,
                BalanceDueDaysBeforeDeparture = payment.BalanceDueDaysBeforeDeparture,
                ExtraPaymentDueDays = payment.ExtraPaymentDueDays,
                ApprovedByName = plan?.ReviewedBy?.FullName,
                ApprovedAt = plan?.ReviewedAt
            };

            string policyText = PolicyDocumentText.Prepare(
                PolicyDocumentText.Load(), SystemSettingService.ConfigValues(payment, policy), policy.PolicyVersion);
            byte[] pdf = ContractPdfRenderer.Render(document, policyText);

            StoredFileModel stored;
            using (var stream = new MemoryStream(pdf))
            {
                stored = await _storage.UploadAsync(stream, $"{code}-v{version}.pdf", pdf.Length, FileCategories.Documents);
            }

            // Bản nháp cũ (nếu có) bị thay; lưu trước để không đụng chỉ mục "một bản nháp / đơn"
            foreach (var oldDraft in existing.Where(c => c.Status == ContractStatuses.Draft))
            {
                oldDraft.Status = ContractStatuses.Superseded;
            }
            await _contractRepo.SaveChangesAsync();

            var contract = new BookingContract
            {
                ContractCode = code,
                BookingId = bookingId,
                Version = version,
                Status = ContractStatuses.Draft,
                Reason = reason,
                FilePath = stored.Path,
                FileSha256 = ContractPdfRenderer.Sha256(pdf),
                PolicyVersion = policy.PolicyVersion,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _contractRepo.AddAsync(contract);
            await _contractRepo.SaveChangesAsync();

            if (requireAccepted)
            {
                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"New contract version for booking {booking.BookingCode}",
                    $"Contract {code} (version {version}) reflects the requested change. Please review and accept it; the change only applies after you accept.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            }

            return ToModel(contract, booking.BookingCode);
        }

        // =============================================================================================
        // Xem / tải
        // =============================================================================================

        public async Task<BookingContractModel> GetCurrentAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await GetViewableBookingAsync(bookingId, currentUser);
            var contracts = await LoadContractsAsync(bookingId);

            // Bản đang chờ khách chấp nhận được ưu tiên; không có thì là bản đã giao kết
            var current = contracts.FirstOrDefault(c => c.Status == ContractStatuses.Draft)
                ?? contracts.FirstOrDefault(c => c.Status == ContractStatuses.Accepted)
                ?? throw new NotFoundException($"Booking {booking.BookingCode} has no contract yet. It is created together with the final quote.");

            var model = ToModel(current, booking.BookingCode);
            var url = await _storage.CreateSignedUrlAsync(current.FilePath);
            model.FileUrl = url.Url;
            model.FileUrlExpiresAt = url.UrlExpiresAt;
            return model;
        }

        public async Task<List<BookingContractModel>> GetHistoryAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await GetViewableBookingAsync(bookingId, currentUser);
            var contracts = await LoadContractsAsync(bookingId);
            return contracts.OrderByDescending(c => c.Version).Select(c => ToModel(c, booking.BookingCode)).ToList();
        }

        public async Task<SignedFileUrlModel> GetFileUrlAsync(int contractId, CurrentUser currentUser)
        {
            var contract = await _contractRepo.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContractId == contractId)
                ?? throw new NotFoundException($"Contract with ID {contractId} was not found.");
            await GetViewableBookingAsync(contract.BookingId, currentUser);
            return await _storage.CreateSignedUrlAsync(contract.FilePath);
        }

        // =============================================================================================
        // Chấp nhận
        // =============================================================================================

        public async Task AcceptInitialAsync(int bookingId, ContractAcceptanceModel acceptance, CurrentUser currentUser)
        {
            var contracts = await _contractRepo.GetQueryable()
                .Where(c => c.BookingId == bookingId)
                .ToListAsync();
            var contract = contracts.FirstOrDefault(c => c.ContractId == acceptance.ContractId)
                ?? throw new BusinessException(ContractRules.ChangedMessage);
            await AcceptAsync(contract, contracts, acceptance, currentUser);
        }

        public async Task<BookingContractModel> AcceptAmendmentAsync(ContractAcceptanceModel acceptance, CurrentUser currentUser)
        {
            var target = await _contractRepo.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContractId == acceptance.ContractId)
                ?? throw new NotFoundException($"Contract with ID {acceptance.ContractId} was not found.");
            var booking = await _bookingRepo.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == target.BookingId)
                ?? throw new NotFoundException($"Booking with ID {target.BookingId} was not found.");
            if (booking.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only accept the contract of your own booking.");
            }

            var contracts = await _contractRepo.GetQueryable()
                .Where(c => c.BookingId == target.BookingId)
                .ToListAsync();
            if (!contracts.Any(c => c.Status == ContractStatuses.Accepted))
            {
                // Bản đầu đi cùng báo giá: chấp nhận bằng accept-quote để đơn chuyển sang chờ cọc
                throw new BusinessException("The first contract is accepted together with the quote: use POST /api/bookings/{id}/accept-quote.");
            }
            await EnsurePolicyAcceptedAsync(acceptance);

            var contract = contracts.First(c => c.ContractId == acceptance.ContractId);
            await AcceptAsync(contract, contracts, acceptance, currentUser);
            // TODO(BE-06 / BE-14b / BE-15): áp dụng thay đổi của bản sửa đổi khi khách chấp nhận

            return ToModel(contract, booking.BookingCode);
        }

        private async Task AcceptAsync(BookingContract contract, List<BookingContract> contracts, ContractAcceptanceModel acceptance, CurrentUser currentUser)
        {
            var currentDraft = contracts.FirstOrDefault(c => c.Status == ContractStatuses.Draft);
            ContractRules.EnsureAcceptable(contract.Status, currentDraft?.ContractId == contract.ContractId, contract.FileSha256, acceptance.ContractSha256);

            // Bản đã giao kết cũ -> Superseded (lưu trước để không đụng chỉ mục "một bản Accepted / đơn")
            foreach (var old in contracts.Where(c => c.Status == ContractStatuses.Accepted))
            {
                old.Status = ContractStatuses.Superseded;
            }
            await _contractRepo.SaveChangesAsync();

            contract.Status = ContractStatuses.Accepted;
            contract.AcceptedByUserId = currentUser.UserId;
            contract.AcceptedAt = DateTimeOffset.UtcNow;
            contract.AcceptedIp = ContractRules.Truncate(acceptance.IpAddress, IpMaxLength);
            contract.AcceptedUserAgent = ContractRules.Truncate(acceptance.UserAgent, UserAgentMaxLength);
            contract.AcceptedPolicyVersion = acceptance.PolicyVersion.Trim();
            await _contractRepo.SaveChangesAsync();

            var booking = await _bookingRepo.GetQueryable().AsNoTracking().FirstAsync(b => b.BookingId == contract.BookingId);
            await _notificationService.EmailCustomerAsync(
                booking.CustomerUserId,
                EmailTemplateNames.ContractAccepted,
                CustomerPages.Booking(booking.BookingId),
                new Dictionary<string, string>
                {
                    ["BookingCode"] = booking.BookingCode,
                    ["ContractCode"] = contract.ContractCode,
                    ["Version"] = contract.Version.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        // ---------------------------------------------------------------------------------------------

        private async Task EnsurePolicyAcceptedAsync(ContractAcceptanceModel acceptance)
        {
            if (!acceptance.AcceptPolicy)
            {
                throw new BusinessException("You must accept the service policy to continue.");
            }
            string current = (await _settingService.GetPolicySettingsAsync()).PolicyVersion;
            if (string.IsNullOrWhiteSpace(acceptance.PolicyVersion) || acceptance.PolicyVersion.Trim() != current)
            {
                throw new BusinessException($"The service policy has changed (current version {current}). Please read the latest policy and accept it again.");
            }
        }

        private async Task<BookingRequest> GetViewableBookingAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await _bookingRepo.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == bookingId)
                ?? throw new NotFoundException($"Booking with ID {bookingId} was not found.");
            if (!BookingService.CanView(booking, currentUser))
            {
                throw new ForbiddenException("You do not have access to this booking.");
            }
            return booking;
        }

        private Task<List<BookingContract>> LoadContractsAsync(int bookingId) =>
            _contractRepo.GetQueryable().AsNoTracking()
                .Include(c => c.AcceptedBy)
                .Where(c => c.BookingId == bookingId)
                .ToListAsync();

        private static BookingContractModel ToModel(BookingContract c, string bookingCode) => new()
        {
            ContractId = c.ContractId,
            ContractCode = c.ContractCode,
            BookingId = c.BookingId,
            BookingCode = bookingCode,
            Version = c.Version,
            Status = c.Status,
            Reason = c.Reason,
            FileSha256 = c.FileSha256,
            PolicyVersion = c.PolicyVersion,
            AcceptedByUserId = c.AcceptedByUserId,
            AcceptedByName = c.AcceptedBy?.FullName,
            AcceptedAt = c.AcceptedAt,
            AcceptedIp = c.AcceptedIp,
            AcceptedUserAgent = c.AcceptedUserAgent,
            AcceptedPolicyVersion = c.AcceptedPolicyVersion,
            CreatedAt = c.CreatedAt
        };
    }
}
