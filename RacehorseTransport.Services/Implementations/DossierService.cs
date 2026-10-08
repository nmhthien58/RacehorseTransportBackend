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
    // Phần xem nằm ở file này; phần nộp, thẩm định và thông quan nằm ở DossierService.Workflow.cs
    public partial class DossierService : IDossierService
    {
        private readonly IRepository<DigitalDossier> _dossierRepo;
        private readonly IRepository<DossierDocument> _documentRepo;
        private readonly IRepository<CountryDocRequirement> _requirementRepo;
        private readonly IRepository<DocumentType> _docTypeRepo;
        private readonly IFileStorageService _fileStorage;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public DossierService(
            IRepository<DigitalDossier> dossierRepo,
            IRepository<DossierDocument> documentRepo,
            IRepository<CountryDocRequirement> requirementRepo,
            IRepository<DocumentType> docTypeRepo,
            IFileStorageService fileStorage,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _dossierRepo = dossierRepo;
            _documentRepo = documentRepo;
            _requirementRepo = requirementRepo;
            _docTypeRepo = docTypeRepo;
            _fileStorage = fileStorage;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedBusinessResult<DossierSummaryModel>> GetDossiersAsync(DossierFilterOptions options, CurrentUser currentUser)
        {
            var query = ApplyVisibility(_dossierRepo.GetQueryable().AsNoTracking(), currentUser);

            // Filtering
            if (!string.IsNullOrWhiteSpace(options.Status))
            {
                if (!DossierStatuses.All.Contains(options.Status))
                {
                    throw new BusinessException($"Status '{options.Status}' is invalid. Allowed values: {string.Join(", ", DossierStatuses.All)}.");
                }
                query = query.Where(d => d.Status == options.Status);
            }

            if (options.BookingId.HasValue)
            {
                query = query.Where(d => d.BookingHorse!.BookingId == options.BookingId.Value);
            }

            // Searching
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                string search = options.Search.Trim().ToLower();
                query = query.Where(d => d.DossierCode.ToLower().Contains(search)
                    || d.BookingHorse!.Horse!.Name.ToLower().Contains(search)
                    || d.BookingHorse.Horse.MicrochipNumber.ToLower().Contains(search)
                    || d.BookingHorse.Booking!.BookingCode.ToLower().Contains(search));
            }

            query = query
                .Include(d => d.Specialist)
                .Include(d => d.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(d => d.BookingHorse!).ThenInclude(bh => bh.Booking!).ThenInclude(b => b.Customer);

            // Sorting: mặc định hồ sơ mới nhất lên đầu
            query = query.ApplySort(string.IsNullOrWhiteSpace(options.Sort) ? "-createdAt" : options.Sort, nameof(DigitalDossier.DossierId));

            // Paging
            return await query.ToPagedResultAsync(options, d => d.ToSummaryModel());
        }

        public async Task<DossierDetailModel> GetDossierByIdAsync(int id, CurrentUser currentUser)
        {
            var dossier = await _dossierRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(d => d.Specialist)
                .Include(d => d.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(d => d.BookingHorse!).ThenInclude(bh => bh.Booking!).ThenInclude(b => b.Customer)
                .Include(d => d.Documents).ThenInclude(doc => doc.DocumentType)
                .Include(d => d.Documents).ThenInclude(doc => doc.UploadedBy)
                .Include(d => d.Documents).ThenInclude(doc => doc.ReviewedBy)
                .FirstOrDefaultAsync(d => d.DossierId == id);

            if (dossier == null)
            {
                throw new NotFoundException($"Dossier with ID {id} was not found.");
            }

            if (!CanView(dossier, currentUser))
            {
                throw new ForbiddenException("You do not have access to this dossier.");
            }

            var model = dossier.ToDetailModel();
            var booking = dossier.BookingHorse!.Booking!;

            model.Checklist = await BuildChecklistAsync(booking.PickupCountryCode, booking.DropoffCountryCode, model.Documents);

            var mandatory = model.Checklist.Where(c => c.IsMandatory).ToList();
            model.RequiredCount = mandatory.Count;
            model.ApprovedCount = mandatory.Count(c => c.State == DocumentStatuses.Approved);
            model.IsReadyToSubmit = mandatory.Count > 0 && model.ApprovedCount == mandatory.Count;

            // Checklist dùng lại chính các object trong model.Documents nên chỉ cần đổi một lần
            await _fileStorage.ResolveViewUrlsAsync(model.Documents, d => d.FileUrl, (d, url) => d.FileUrl = url ?? string.Empty);

            return model;
        }

        // Danh sách giấy tờ cần có = quy định xuất khẩu của nước đi + quy định nhập khẩu của nước đến.
        // Loại giấy được cả hai nước yêu cầu thì gộp thành một dòng, lấy thời hạn ngặt hơn.
        // Nếu chưa có quy định nào cho cặp nước này thì dùng các loại giấy bắt buộc mặc định trong danh mục.
        private async Task<List<DossierChecklistItemModel>> BuildChecklistAsync(
            string pickupCountry, string dropoffCountry, List<DossierDocumentModel> documents)
        {
            var requirements = await _requirementRepo.GetQueryable()
                .AsNoTracking()
                .Include(r => r.DocumentType)
                .Where(r => (r.CountryCode == pickupCountry && r.Direction == RequirementDirections.Export)
                         || (r.CountryCode == dropoffCountry && r.Direction == RequirementDirections.Import))
                .ToListAsync();

            List<DossierChecklistItemModel> checklist;

            if (requirements.Count > 0)
            {
                checklist = requirements
                    .GroupBy(r => r.DocTypeId)
                    .Select(g => new DossierChecklistItemModel
                    {
                        DocTypeId = g.Key,
                        DocTypeCode = g.First().DocumentType!.Code,
                        DocTypeName = g.First().DocumentType!.Name,
                        IsMandatory = g.Any(r => r.IsMandatory),
                        ValidityDays = g.Min(r => r.ValidityDays),
                        RegulationNote = string.Join("; ", g.Where(r => !string.IsNullOrWhiteSpace(r.RegulationNote)).Select(r => r.RegulationNote)),
                        RequiredBy = g.OrderBy(r => r.Direction).Select(r => $"{r.CountryCode} {r.Direction}").ToList()
                    })
                    .ToList();
            }
            else
            {
                var defaults = await _docTypeRepo.GetQueryable().AsNoTracking().Where(t => t.IsMandatory).ToListAsync();
                checklist = defaults
                    .Select(t => new DossierChecklistItemModel
                    {
                        DocTypeId = t.DocTypeId,
                        DocTypeCode = t.Code,
                        DocTypeName = t.Name,
                        IsMandatory = true
                    })
                    .ToList();
            }

            // Trạng thái từng dòng lấy theo bản nộp mới nhất của loại giấy đó (documents đã sắp mới nhất trước)
            foreach (var item in checklist)
            {
                item.LatestDocument = documents.FirstOrDefault(d => d.DocTypeId == item.DocTypeId);
                item.State = item.LatestDocument?.Status ?? ChecklistStates.Missing;
            }

            return checklist.OrderByDescending(c => c.IsMandatory).ThenBy(c => c.DocTypeCode).ToList();
        }

        // Ai thấy hồ sơ nào:
        //   Customer            -> hồ sơ của ngựa trong đơn của mình
        //   TransportSpecialist -> hồ sơ mình phụ trách
        //   Manager, Coordinator, Admin -> tất cả
        //   DriverEscort        -> không xem hồ sơ (chỉ thấy trạng thái thông quan trong chi tiết chuyến)
        private static IQueryable<DigitalDossier> ApplyVisibility(IQueryable<DigitalDossier> query, CurrentUser currentUser)
        {
            if (currentUser.IsCustomer)
            {
                return query.Where(d => d.BookingHorse!.Booking!.CustomerUserId == currentUser.UserId);
            }

            if (currentUser.Role == UserRoles.TransportSpecialist)
            {
                return query.Where(d => d.SpecialistUserId == currentUser.UserId);
            }

            if (currentUser.Role == UserRoles.DriverEscort)
            {
                throw new ForbiddenException("Drivers and escorts do not have access to clearance dossiers.");
            }

            return query;
        }

        private static bool CanView(DigitalDossier dossier, CurrentUser currentUser)
        {
            if (currentUser.IsCustomer)
            {
                return dossier.BookingHorse?.Booking?.CustomerUserId == currentUser.UserId;
            }

            if (currentUser.Role == UserRoles.TransportSpecialist)
            {
                return dossier.SpecialistUserId == currentUser.UserId;
            }

            return currentUser.Role != UserRoles.DriverEscort;
        }
    }
}
