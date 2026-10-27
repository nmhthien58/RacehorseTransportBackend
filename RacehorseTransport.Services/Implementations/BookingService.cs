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
        // Đơn ở các trạng thái này còn đang chạy: ngựa trong đơn chưa thể vào đơn khác trùng thời gian
        private static readonly string[] OpenStatuses =
        {
            BookingStatuses.Submitted, BookingStatuses.PreApproved, BookingStatuses.RoutePlanned, BookingStatuses.Quoted,
            BookingStatuses.AwaitingDeposit, BookingStatuses.Approved, BookingStatuses.Assigned
        };

        // BE-09: Manager chốt giá trị thỏa thuận trước khi gửi báo giá
        private static readonly string[] HorseValueEditableStatuses =
        {
            BookingStatuses.Submitted, BookingStatuses.PreApproved, BookingStatuses.RoutePlanned
        };

        // R-10: Manager từ chối được đơn cả sau khi duyệt sơ bộ (ví dụ không lập được tuyến khả thi), trước khi báo giá
        private static readonly string[] RejectableStatuses =
        {
            BookingStatuses.Submitted, BookingStatuses.PreApproved, BookingStatuses.RoutePlanned
        };

        // Khách tự hủy được khi đơn còn ở các trạng thái này (và ngựa chưa nằm trên chuyến nào)
        private static readonly string[] CancellableStatuses =
        {
            BookingStatuses.Submitted, BookingStatuses.PreApproved, BookingStatuses.RoutePlanned, BookingStatuses.Quoted,
            BookingStatuses.AwaitingDeposit, BookingStatuses.Approved, BookingStatuses.Assigned
        };

        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<BookingHorse> _bookingHorseRepo;
        private readonly IRepository<Horse> _horseRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<DigitalDossier> _dossierRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IPricingService _pricingService;
        private readonly IClearanceScheduleService _clearanceSchedule;
        private readonly IPaymentService _paymentService;
        private readonly ISystemSettingService _settingService;
        private readonly IFileStorageService _fileStorage;
        private readonly IBookingActivator _bookingActivator;
        private readonly INotificationService _notificationService;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly BookingOptions _options;

        public BookingService(
            IRepository<BookingRequest> bookingRepo,
            IRepository<BookingHorse> bookingHorseRepo,
            IRepository<Horse> horseRepo,
            IRepository<User> userRepo,
            IRepository<DigitalDossier> dossierRepo,
            IRepository<TripHorse> tripHorseRepo,
            IPricingService pricingService,
            IClearanceScheduleService clearanceSchedule,
            IPaymentService paymentService,
            ISystemSettingService settingService,
            IFileStorageService fileStorage,
            IBookingActivator bookingActivator,
            INotificationService notificationService,
            ICodeGenerator codeGenerator,
            IUnitOfWork unitOfWork,
            IOptions<BookingOptions> options)
        {
            _bookingRepo = bookingRepo;
            _bookingHorseRepo = bookingHorseRepo;
            _horseRepo = horseRepo;
            _userRepo = userRepo;
            _dossierRepo = dossierRepo;
            _tripHorseRepo = tripHorseRepo;
            _pricingService = pricingService;
            _clearanceSchedule = clearanceSchedule;
            _paymentService = paymentService;
            _settingService = settingService;
            _fileStorage = fileStorage;
            _bookingActivator = bookingActivator;
            _notificationService = notificationService;
            _codeGenerator = codeGenerator;
            _unitOfWork = unitOfWork;
            _options = options.Value;
        }

        public async Task<PagedBusinessResult<BookingSummaryModel>> GetBookingsAsync(BookingFilterOptions options, CurrentUser currentUser)
        {
            var query = ApplyVisibility(_bookingRepo.GetQueryable().AsNoTracking(), currentUser)
                .Include(b => b.Customer)
                .Include(b => b.AssignedSpecialist)
                .Include(b => b.TransitCountries)
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
            if (options.PickupFrom.HasValue)
            {
                var from = options.PickupFrom.Value.ToUniversalTime();
                query = query.Where(b => b.PickupWindowStart >= from);
            }

            if (options.PickupTo.HasValue)
            {
                var to = options.PickupTo.Value.ToUniversalTime();
                query = query.Where(b => b.PickupWindowStart <= to);
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
            query = query.ApplySort(string.IsNullOrWhiteSpace(options.Sort) ? "-createdAt" : options.Sort, nameof(BookingRequest.BookingId));

            PagedBusinessResult<BookingSummaryModel> result;
            if (string.IsNullOrWhiteSpace(options.Stage))
            {
                result = await query.ToPagedResultAsync(options, b => b.ToSummaryModel());
                var stages = await ResolveStagesAsync(result.Items.Select(b => b.BookingId).ToList());
                result.Items.ForEach(b => b.CustomerStage = stages[b.BookingId]);
                return result;
            }

            // Giai đoạn không phải cột DB: tính cho mọi đơn khớp bộ lọc rồi mới lọc và cắt trang
            if (!CustomerStages.All.Contains(options.Stage))
            {
                throw new BusinessException($"Stage '{options.Stage}' is invalid. Allowed values: {string.Join(", ", CustomerStages.All)}.");
            }
            var all = (await query.ToListAsync()).Select(b => b.ToSummaryModel()).ToList();
            var allStages = await ResolveStagesAsync(all.Select(b => b.BookingId).ToList());
            all.ForEach(b => b.CustomerStage = allStages[b.BookingId]);
            var matched = all.Where(b => b.CustomerStage == options.Stage).ToList();

            int page = options.Page > 0 ? options.Page : 1;
            int size = options.Size > 0 ? Math.Min(options.Size, BusinessQueryOptions.MaxSize) : 10;
            return new PagedBusinessResult<BookingSummaryModel>
            {
                Items = matched.Skip((page - 1) * size).Take(size).ToList(),
                Page = page,
                PageSize = size,
                TotalItems = matched.Count
            };
        }

        // Giai đoạn theo góc nhìn khách của các đơn (R-07), xem CustomerStageRules
        private async Task<Dictionary<int, string>> ResolveStagesAsync(List<int> bookingIds)
        {
            if (bookingIds.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            var bookings = await _bookingRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Dossier)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.TripHorses).ThenInclude(th => th.Trip!).ThenInclude(t => t.Checkpoints)
                .Include(b => b.Payments)
                .Where(b => bookingIds.Contains(b.BookingId))
                .ToListAsync();

            var settings = await _settingService.GetPaymentSettingsAsync();
            return bookings.ToDictionary(
                b => b.BookingId,
                b => CustomerStageRules.Resolve(b, _paymentService.BuildSummary(b, settings)));
        }

        public async Task<BookingDetailModel> GetBookingByIdAsync(int id, CurrentUser currentUser)
        {
            var booking = await _bookingRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.Customer)
                .Include(b => b.AssignedSpecialist)
                .Include(b => b.ReviewedBy)
                .Include(b => b.TransitCountries)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Horse)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.Dossier)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.TripHorses).ThenInclude(th => th.Trip!).ThenInclude(t => t.Checkpoints)
                .Include(b => b.Payments).ThenInclude(p => p.CreatedBy)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {id} was not found.");
            }

            if (!CanView(booking, currentUser))
            {
                throw new ForbiddenException("You do not have access to this booking.");
            }

            var model = booking.ToDetailModel(_pricingService.ParseLines(booking.QuoteBreakdown));
            model.Payment = _paymentService.BuildSummary(booking, await _settingService.GetPaymentSettingsAsync());
            model.CustomerStage = CustomerStageRules.Resolve(booking, model.Payment);
            await _fileStorage.ResolveViewUrlsAsync(model.Horses, h => h.ValueProofUrl, (h, url) => h.ValueProofUrl = url);
            return model;
        }

        // ---------------------------------------------------------------------------------------------
        // Flow 1: khách tạo đơn -> Manager duyệt (kèm chọn Specialist) hoặc từ chối; khách hủy khi chưa duyệt
        // ---------------------------------------------------------------------------------------------

        public async Task<BookingDetailModel> CreateBookingAsync(CreateBookingModel model, CurrentUser currentUser)
        {
            // R-02: còn chi phí phát sinh quá hạn chưa trả thì chưa được đặt đơn mới
            if (await _paymentService.HasOverdueDebtAsync(currentUser.UserId))
            {
                throw new BusinessException("You have overdue extra charges on a previous booking. Please pay them before creating a new booking.");
            }

            var horseIds = model.Horses.Select(h => h.HorseId).ToList();
            if (horseIds.Count != horseIds.Distinct().Count())
            {
                throw new BusinessException("The same horse appears more than once in this booking.");
            }

            // Ngựa phải là của chính khách và còn hoạt động
            var ownedHorses = await _horseRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => horseIds.Contains(h.HorseId) && h.OwnerUserId == currentUser.UserId && h.IsActive)
                .ToListAsync();

            var missingIds = horseIds.Except(ownedHorses.Select(h => h.HorseId)).ToList();
            if (missingIds.Count > 0)
            {
                throw new BusinessException($"Horse(s) with ID {string.Join(", ", missingIds)} were not found among your active horses.");
            }

            // R-12: tuổi tối thiểu, mang thai gần ngày đẻ, vừa đẻ (ngày theo lịch của khách)
            var policy = await _settingService.GetPolicySettingsAsync();

            // R-10: khách phải đồng ý Chính sách dịch vụ đúng phiên bản hiện hành
            if (!model.AcceptPolicy)
            {
                throw new BusinessException("You must accept the service policy to send a request.");
            }
            if (string.IsNullOrWhiteSpace(model.PolicyVersion) || model.PolicyVersion != policy.PolicyVersion)
            {
                throw new BusinessException($"The service policy has changed (current version {policy.PolicyVersion}). Please read the latest policy and accept it again.");
            }
            string? ineligible = HorseEligibilityRules.Describe(
                ownedHorses,
                DateOnly.FromDateTime(model.PickupWindowStart.DateTime),
                DateOnly.FromDateTime(model.PickupWindowEnd.DateTime),
                policy);
            if (ineligible != null)
            {
                throw new BusinessException(ineligible);
            }

            // R-12 / R-13: giá trị từng con. Khai theo từng con thì giá trị của đơn = tổng các con
            // (client cũ chỉ gửi một giá trị cho cả đơn vẫn chạy như trước)
            var horseNames = ownedHorses.ToDictionary(h => h.HorseId, h => h.Name);
            var valueErrors = HorseValueRules.Validate(
                model.Horses.Select(h => new HorseValueRules.HorseValue(horseNames[h.HorseId], h.DeclaredValue, h.ValueProofPath, h.InsurancePlan)).ToList(),
                policy.InsuranceProofThresholdUsd,
                path => _fileStorage.IsValidPath(path, FileCategories.Documents));
            if (valueErrors.Count > 0)
            {
                throw new BusinessException(string.Join(" ", valueErrors));
            }
            if (model.Horses.All(h => h.DeclaredValue.HasValue))
            {
                model.DeclaredValue = model.Horses.Sum(h => h.DeclaredValue!.Value);
            }

            // Báo giá trước: đồng thời kiểm tra phương thức, hạng chuồng, tuyến có giá, DistanceKm,
            // khoảng pickup (đủ xa, không quá dài) và hạn giao (đủ thời gian vận chuyển)
            // R-10: khoảng giá ước tính; quãng đường + nước đi qua lấy từ bản đồ khi có tọa độ
            var quote = await _pricingService.CalculateQuoteRangeAsync(new QuoteInputModel
            {
                TransportMode = model.TransportMode,
                DropoffCountryCode = model.DropoffCountryCode,
                PickupCountryCode = model.PickupCountryCode,
                PickupCity = model.PickupCity,
                PickupAddress = model.PickupAddress,
                DropoffCity = model.DropoffCity,
                DropoffAddress = model.DropoffAddress,
                DistanceKm = model.DistanceKm,
                RequiresClimateControl = model.RequiresClimateControl,
                DeclaredValue = model.DeclaredValue,
                PickupWindowStart = model.PickupWindowStart,
                PickupWindowEnd = model.PickupWindowEnd,
                DeliveryDeadline = model.DeliveryDeadline,
                PickupLat = model.PickupLat,
                PickupLng = model.PickupLng,
                DropoffLat = model.DropoffLat,
                DropoffLng = model.DropoffLng,
                StallClasses = model.Horses.Select(h => h.StallClass).ToList()
            });

            // Một con ngựa không thể nằm trong hai đơn đang chạy có thời gian giao nhau
            // (thời gian của đơn: từ đầu khoảng pickup tới hạn giao)
            var startUtc = model.PickupWindowStart.ToUniversalTime();
            var deadlineUtc = model.DeliveryDeadline.ToUniversalTime();

            var busyHorseNames = await _bookingHorseRepo.GetQueryable()
                .Where(bh => horseIds.Contains(bh.HorseId)
                    && bh.Status != BookingHorseStatuses.Cancelled
                    && OpenStatuses.Contains(bh.Booking!.Status)
                    && bh.Booking.PickupWindowStart < deadlineUtc
                    && bh.Booking.DeliveryDeadline > startUtc)
                .Select(bh => bh.Horse!.Name)
                .Distinct()
                .ToListAsync();

            if (busyHorseNames.Count > 0)
            {
                throw new BusinessException($"These horses are already in another booking for an overlapping period: {string.Join(", ", busyHorseNames)}.");
            }

            int bookingId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var booking = new BookingRequest
                {
                    BookingCode = await _codeGenerator.NextAsync(CodePrefixes.Booking),
                    CustomerUserId = currentUser.UserId,
                    PickupAddress = model.PickupAddress,
                    PickupCountryCode = model.PickupCountryCode,
                    PickupLat = model.PickupLat,
                    PickupLng = model.PickupLng,
                    DropoffAddress = model.DropoffAddress,
                    DropoffCountryCode = model.DropoffCountryCode,
                    DropoffLat = model.DropoffLat,
                    DropoffLng = model.DropoffLng,
                    PickupWindowStart = model.PickupWindowStart,
                    PickupWindowEnd = model.PickupWindowEnd,
                    DeliveryDeadline = model.DeliveryDeadline,
                    EstimatedTransitHours = quote.EstimatedHours,
                    TotalHorses = model.Horses.Count,
                    SpecialInstructions = model.SpecialInstructions,
                    TransportMode = model.TransportMode,
                    DistanceKm = quote.DistanceKm ?? model.DistanceKm,
                    IsExpress = quote.IsExpress,
                    RequiresClimateControl = model.RequiresClimateControl,
                    DeclaredValue = model.DeclaredValue,
                    QuoteMin = quote.QuoteMin,
                    QuoteMax = quote.QuoteMax,
                    EstimatedCost = 0, // R-10: giá chốt có sau khi duyệt tuyến (BookingQuoteService)
                    PolicyVersion = model.PolicyVersion,
                    PolicyAcceptedAt = DateTimeOffset.UtcNow,
                    CurrencyCode = quote.CurrencyCode,
                    QuoteBreakdown = _pricingService.SerializeLines(quote.Lines),
                    Status = BookingStatuses.Submitted,
                    TransitCountries = ToTransitRows(quote.TransitCountryCodes),
                    BookingHorses = model.Horses.Select(h => new BookingHorse
                    {
                        HorseId = h.HorseId,
                        StallClass = h.StallClass,
                        Notes = h.Notes,
                        Status = BookingHorseStatuses.Pending,
                        DeclaredValue = h.DeclaredValue,
                        AgreedValue = h.DeclaredValue, // mặc định = giá trị khai báo, Manager chốt lại trước khi báo giá
                        ValueProofPath = string.IsNullOrWhiteSpace(h.ValueProofPath) ? null : h.ValueProofPath.Trim(),
                        InsurancePlan = string.IsNullOrWhiteSpace(h.InsurancePlan) ? InsurancePlans.None : h.InsurancePlan
                    }).ToList()
                };

                await _bookingRepo.AddAsync(booking);

                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"New booking {booking.BookingCode}",
                    $"A transport request for {booking.TotalHorses} horse(s) from {booking.PickupCountryCode} to {booking.DropoffCountryCode} is waiting for review.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);

                return booking.BookingId;
            });

            return await GetBookingByIdAsync(bookingId, currentUser);
        }

        // Duyệt đơn và chọn luôn Specialist phụ trách; mỗi con ngựa trong đơn được mở một bộ hồ sơ kiểm dịch.
        public async Task<BookingDetailModel> ApproveBookingAsync(int id, int specialistUserId, CurrentUser currentUser)
        {
            var booking = await GetTrackedBookingAsync(id);
            EnsureStatus(booking, BookingStatuses.Submitted, "approved");

            // Duyệt chậm thì khoảng pickup có thể không còn đủ thời gian cho đặt cọc, hồ sơ (theo tuyến) và xếp chuyến
            int clearanceDays = await _clearanceSchedule.GetClearanceDaysAsync(
                booking.PickupCountryCode, TransitCodes(booking), booking.DropoffCountryCode);
            int approvalLeadDays = _options.ApprovalLeadDaysFor(clearanceDays);
            var latestPickupDay = DateOnly.FromDateTime(booking.PickupWindowEnd.UtcDateTime);
            var neededDay = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(approvalLeadDays);
            if (latestPickupDay < neededDay)
            {
                throw new BusinessException(
                    $"This booking can no longer be approved: its pickup window ends on {latestPickupDay:yyyy-MM-dd}, but deposit, quarantine clearance and trip planning on this route need about {approvalLeadDays} days (until {neededDay:yyyy-MM-dd}). Reject it and ask the customer to book a later pickup window.");
            }

            var specialist = await GetActiveSpecialistAsync(specialistUserId);

            // R-10: đây là bước DUYỆT SƠ BỘ (đơn khả thi). Chưa thu cọc: Coordinator lập tuyến, Manager duyệt tuyến,
            // rồi khách nhận báo giá chốt và đặt cọc khi chấp nhận (BE-05).
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                booking.Status = BookingStatuses.PreApproved;
                booking.ReviewedByUserId = currentUser.UserId;
                booking.ReviewedAt = DateTimeOffset.UtcNow;
                booking.AssignedSpecialistId = specialist.UserId;
                booking.RejectionReason = null;
                await _bookingRepo.UpdateAsync(booking);

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Booking {booking.BookingCode} passed the first review",
                    "We are planning the route on the map. You will receive the final quote to accept; no payment is needed yet.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);

                await _notificationService.SendToRoleAsync(
                    UserRoles.FleetCoordinator,
                    $"Plan the route for booking {booking.BookingCode}",
                    $"{booking.TotalHorses} horse(s) from {booking.PickupCountryCode} to {booking.DropoffCountryCode}, pickup {booking.PickupWindowStart:yyyy-MM-dd} – {booking.PickupWindowEnd:yyyy-MM-dd}. Draw the planned route and submit it for approval.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            return await GetBookingByIdAsync(id, currentUser);
        }

        // R-03: Manager xác nhận / sửa các nước quá cảnh khi xét duyệt (khách có thể đã gợi ý lúc đặt).
        // Đổi nước quá cảnh làm đổi giá (phí quá cảnh, thời gian qua cửa khẩu) nên tính lại báo giá,
        // lấy mốc "hôm nay" là ngày tạo đơn để không bắt khách đổi lịch chỉ vì đơn đã chờ duyệt vài ngày.
        // BE-09: Manager chốt giá trị thỏa thuận từng con trước khi gửi báo giá.
        // TODO(BE-04): khi có duyệt sơ bộ / duyệt tuyến thì cho sửa thêm ở PreApproved, RoutePlanned (trước Quoted).
        public async Task<BookingDetailModel> UpdateHorseValuesAsync(int id, UpdateHorseValuesModel model, CurrentUser currentUser)
        {
            var booking = await GetTrackedBookingAsync(id);
            if (!HorseValueEditableStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"Agreed values can only be set before the quote is sent (current status: {booking.Status}).");
            }
            if (model.Horses.Count == 0)
            {
                throw new BusinessException("Send at least one horse.");
            }

            var activeHorses = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).ToList();
            var unknown = model.Horses.Select(h => h.BookingHorseId).Except(activeHorses.Select(bh => bh.BookingHorseId)).ToList();
            if (unknown.Count > 0)
            {
                throw new BusinessException($"Booking horse(s) with ID {string.Join(", ", unknown)} are not in booking {booking.BookingCode}.");
            }
            if (model.Horses.Select(h => h.BookingHorseId).Distinct().Count() != model.Horses.Count)
            {
                throw new BusinessException("The same horse appears more than once.");
            }
            if (model.Horses.Any(h => h.AgreedValue < 0))
            {
                throw new BusinessException("AgreedValue cannot be negative.");
            }

            var horseIds = activeHorses.Select(bh => bh.HorseId).ToList();
            var horseNames = await _horseRepo.GetQueryable().AsNoTracking()
                .Where(h => horseIds.Contains(h.HorseId))
                .ToDictionaryAsync(h => h.HorseId, h => h.Name);

            var changes = new List<string>();
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var input in model.Horses)
                {
                    var bookingHorse = activeHorses.First(bh => bh.BookingHorseId == input.BookingHorseId);
                    if (bookingHorse.AgreedValue != input.AgreedValue)
                    {
                        changes.Add($"{horseNames[bookingHorse.HorseId]}: {input.AgreedValue:N2} USD");
                        bookingHorse.AgreedValue = input.AgreedValue;
                    }
                }
                await _bookingRepo.SaveChangesAsync();

                if (changes.Count > 0)
                {
                    string note = string.IsNullOrWhiteSpace(model.Note) ? string.Empty : $" Note: {model.Note.Trim()}";
                    await _notificationService.SendAsync(
                        booking.CustomerUserId,
                        $"Agreed horse values for booking {booking.BookingCode}",
                        $"Insurance and compensation will use these values: {string.Join("; ", changes)}.{note}",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId);
                }
            });

            return await GetBookingByIdAsync(id, currentUser);
        }

        public async Task<BookingDetailModel> UpdateTransitCountriesAsync(int id, List<string> transitCountryCodes, CurrentUser currentUser)
        {
            var booking = await GetTrackedBookingAsync(id);
            if (booking.Status != BookingStatuses.Submitted)
            {
                throw new BusinessException($"Transit countries can only be changed before the booking is reviewed (current status: {booking.Status}).");
            }

            var activeHorses = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).ToList();
            var quote = await _pricingService.CalculateQuoteAsync(new QuoteInputModel
            {
                TransportMode = booking.TransportMode,
                PickupCountryCode = booking.PickupCountryCode,
                PickupAddress = booking.PickupAddress,
                DropoffCountryCode = booking.DropoffCountryCode,
                DropoffAddress = booking.DropoffAddress,
                DistanceKm = booking.DistanceKm,
                RequiresClimateControl = booking.RequiresClimateControl,
                DeclaredValue = booking.DeclaredValue,
                PickupWindowStart = booking.PickupWindowStart,
                PickupWindowEnd = booking.PickupWindowEnd,
                DeliveryDeadline = booking.DeliveryDeadline,
                TransitCountryCodes = transitCountryCodes,
                QuotedAt = booking.CreatedAt,
                StallClasses = activeHorses.Select(bh => bh.StallClass).ToList()
            });

            decimal previousCost = booking.EstimatedCost;
            var oldRows = booking.TransitCountries.ToList();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Xóa trước rồi thêm lại để không vướng khóa (booking_id, sequence_order)
                foreach (var row in oldRows)
                {
                    booking.TransitCountries.Remove(row);
                }
                await _bookingRepo.SaveChangesAsync();

                foreach (var row in ToTransitRows(quote.TransitCountryCodes))
                {
                    booking.TransitCountries.Add(row);
                }

                booking.EstimatedCost = quote.Total;
                booking.CurrencyCode = quote.CurrencyCode;
                booking.QuoteBreakdown = _pricingService.SerializeLines(quote.Lines);
                booking.IsExpress = quote.IsExpress;
                booking.EstimatedTransitHours = quote.EstimatedHours;
                await _bookingRepo.UpdateAsync(booking);

                string route = quote.TransitCountryCodes.Count > 0
                    ? $"{booking.PickupCountryCode} → {string.Join(" → ", quote.TransitCountryCodes)} → {booking.DropoffCountryCode}"
                    : $"{booking.PickupCountryCode} → {booking.DropoffCountryCode} (no transit country)";

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Route of booking {booking.BookingCode} updated",
                    previousCost == quote.Total
                        ? $"The route was confirmed as {route}."
                        : $"The route was confirmed as {route}. The estimated cost changed from {previousCost:N2} to {quote.Total:N2} {quote.CurrencyCode}.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            return await GetBookingByIdAsync(id, currentUser);
        }

        public async Task<BookingDetailModel> RejectBookingAsync(int id, string reason, CurrentUser currentUser)
        {
            var booking = await GetTrackedBookingAsync(id);
            if (!RejectableStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"Only a booking that has not been quoted yet can be rejected (current status: {booking.Status}).");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                booking.Status = BookingStatuses.Rejected;
                booking.RejectionReason = reason.Trim();
                booking.ReviewedByUserId = currentUser.UserId;
                booking.ReviewedAt = DateTimeOffset.UtcNow;
                CancelHorses(booking);

                await _bookingRepo.UpdateAsync(booking);

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Booking {booking.BookingCode} rejected",
                    $"Your transport request was not accepted. Reason: {booking.RejectionReason}",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);

                await _notificationService.EmailCustomerAsync(
                    booking.CustomerUserId,
                    EmailTemplateNames.BookingRejected,
                    CustomerPages.Booking(booking.BookingId),
                    new Dictionary<string, string>
                    {
                        ["BookingCode"] = booking.BookingCode,
                        ["RejectionReason"] = booking.RejectionReason
                    });
            });

            return await GetBookingByIdAsync(id, currentUser);
        }

        // Đổi Specialist của đơn đã duyệt: chuyển cả các bộ hồ sơ chưa thông quan sang người mới
        public async Task<BookingDetailModel> ReassignSpecialistAsync(int id, int specialistUserId, CurrentUser currentUser)
        {
            var booking = await GetTrackedBookingAsync(id);
            if (booking.Status != BookingStatuses.PreApproved
                && booking.Status != BookingStatuses.RoutePlanned
                && booking.Status != BookingStatuses.Quoted
                && booking.Status != BookingStatuses.AwaitingDeposit
                && booking.Status != BookingStatuses.Approved
                && booking.Status != BookingStatuses.Assigned)
            {
                throw new BusinessException($"The specialist can only be changed on an approved booking (current status: {booking.Status}).");
            }

            var specialist = await GetActiveSpecialistAsync(specialistUserId);
            int? previousSpecialistId = booking.AssignedSpecialistId;
            if (previousSpecialistId == specialist.UserId)
            {
                throw new BusinessException($"{specialist.FullName} is already the specialist of this booking.");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                booking.AssignedSpecialistId = specialist.UserId;

                var bookingHorseIds = booking.BookingHorses.Select(bh => bh.BookingHorseId).ToList();
                var openDossiers = await _dossierRepo.GetQueryable()
                    .Where(d => bookingHorseIds.Contains(d.BookingHorseId) && d.Status != DossierStatuses.Cleared)
                    .ToListAsync();

                foreach (var dossier in openDossiers)
                {
                    dossier.SpecialistUserId = specialist.UserId;
                }

                await _bookingRepo.UpdateAsync(booking);

                await _notificationService.SendAsync(
                    specialist.UserId,
                    $"Booking {booking.BookingCode} assigned to you",
                    "You are now responsible for the clearance dossiers of this booking.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);

                if (previousSpecialistId.HasValue)
                {
                    await _notificationService.SendAsync(
                        previousSpecialistId.Value,
                        $"Booking {booking.BookingCode} reassigned",
                        $"This booking has been handed over to {specialist.FullName}.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId);
                }
            });

            return await GetBookingByIdAsync(id, currentUser);
        }

        // Khách tự hủy đơn của mình trước khi ngựa được xếp lên chuyến (R-02):
        //   Submitted / AwaitingDeposit -> không mất gì; Approved (đã cọc) -> hoàn % cọc theo cấu hình + phần còn lại đã trả.
        // Ngựa đã nằm trên một chuyến thì phải nhờ điều phối / quản lý gỡ khỏi chuyến trước.
        public async Task<BookingDetailModel> CancelBookingAsync(int id, CurrentUser currentUser)
        {
            var booking = await GetTrackedBookingAsync(id);
            if (booking.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only cancel your own bookings.");
            }
            if (!CancellableStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"A booking can only be cancelled before departure (current status: {booking.Status}).");
            }

            var bookingHorseIds = booking.BookingHorses.Select(bh => bh.BookingHorseId).ToList();
            var onTrip = await _tripHorseRepo.GetQueryable()
                .Where(th => bookingHorseIds.Contains(th.BookingHorseId)
                    && th.Trip!.OverallStatus != TripStatuses.Cancelled
                    && TripHorseStatuses.Holding.Contains(th.Status))
                .Select(th => th.Trip!.TripCode)
                .FirstOrDefaultAsync();
            if (onTrip != null)
            {
                throw new BusinessException($"Horses of this booking are already planned on trip {onTrip}. Please contact the logistics team to cancel.");
            }

            string previousStatus = booking.Status;
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                booking.Status = BookingStatuses.Cancelled;
                CancelHorses(booking);

                await _bookingRepo.UpdateAsync(booking);

                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Booking {booking.BookingCode} cancelled",
                    previousStatus == BookingStatuses.Submitted
                        ? "The customer cancelled this transport request before it was reviewed."
                        : $"The customer cancelled this booking (was {previousStatus}). Any refund is processed automatically.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            // Hoàn tiền sau khi đã chốt hủy (gọi cổng thanh toán nên không giữ transaction DB trong lúc chờ)
            if (previousStatus != BookingStatuses.Submitted)
            {
                await _paymentService.RefundForCancellationAsync(booking, currentUser);
            }

            return await GetBookingByIdAsync(id, currentUser);
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

            // R-02: đã duyệt mà quá hạn chưa đặt cọc -> hết hạn, trả ngựa về cho khách đặt đơn khác
            var depositCutoff = DateTimeOffset.UtcNow.AddDays(-_options.DepositDays);
            var unpaid = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses)
                // Đơn theo luồng mới (có hạn báo giá) do BookingQuoteService xử lý (-> QuoteExpired)
                .Where(b => b.Status == BookingStatuses.AwaitingDeposit && b.QuoteExpiresAt == null && b.ReviewedAt != null && b.ReviewedAt < depositCutoff)
                .ToListAsync();

            foreach (var booking in unpaid)
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    booking.Status = BookingStatuses.Expired;
                    CancelHorses(booking);
                    await _bookingRepo.UpdateAsync(booking);

                    await _notificationService.SendAsync(
                        booking.CustomerUserId,
                        $"Booking {booking.BookingCode} has expired",
                        $"The deposit was not paid within {_options.DepositDays} days after approval, so the booking has expired. Please submit a new request.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId,
                        emailCustomers: true);
                });
            }
            overdue.AddRange(unpaid);

            return overdue.Count;
        }

        private static List<string> TransitCodes(BookingRequest booking)
        {
            return booking.TransitCountries.OrderBy(t => t.SequenceOrder).Select(t => t.CountryCode).ToList();
        }

        private static List<BookingTransitCountry> ToTransitRows(IEnumerable<string> codes)
        {
            return codes.Select((code, index) => new BookingTransitCountry
            {
                SequenceOrder = index + 1,
                CountryCode = code
            }).ToList();
        }

        private async Task<BookingRequest> GetTrackedBookingAsync(int id)
        {
            var booking = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses)
                .Include(b => b.TransitCountries)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                throw new NotFoundException($"Booking with ID {id} was not found.");
            }
            return booking;
        }

        private async Task<User> GetActiveSpecialistAsync(int specialistUserId)
        {
            var specialist = await _userRepo.GetByIdAsync(specialistUserId);
            if (specialist == null || !specialist.IsActive || specialist.Role != UserRoles.TransportSpecialist)
            {
                throw new BusinessException($"User with ID {specialistUserId} is not an active transport specialist.");
            }
            return specialist;
        }

        private static void EnsureStatus(BookingRequest booking, string requiredStatus, string action)
        {
            if (booking.Status != requiredStatus)
            {
                throw new BusinessException($"Only a {requiredStatus} booking can be {action} (current status: {booking.Status}).");
            }
        }

        private static void CancelHorses(BookingRequest booking)
        {
            foreach (var bookingHorse in booking.BookingHorses)
            {
                bookingHorse.Status = BookingHorseStatuses.Cancelled;
            }
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

        // Dùng chung với ContractService (R-17)
        internal static bool CanView(BookingRequest booking, CurrentUser currentUser)
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
