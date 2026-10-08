using Microsoft.EntityFrameworkCore;
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
    // SERVICE MẪU cho các chức năng CRUD danh mục. Khi làm bảng mới, copy file này rồi đổi tên/trường.
    public class DocumentTypeService : IDocumentTypeService
    {
        private static readonly TimeSpan LookupCacheTtl = TimeSpan.FromMinutes(30);

        private readonly IRepository<DocumentType> _docTypeRepo;
        private readonly IRepository<DossierDocument> _documentRepo;
        private readonly IRepository<CountryDocRequirement> _requirementRepo;
        private readonly ICacheService _cache;

        public DocumentTypeService(
            IRepository<DocumentType> docTypeRepo,
            IRepository<DossierDocument> documentRepo,
            IRepository<CountryDocRequirement> requirementRepo,
            ICacheService cache)
        {
            _docTypeRepo = docTypeRepo;
            _documentRepo = documentRepo;
            _requirementRepo = requirementRepo;
            _cache = cache;
        }

        public async Task<PagedBusinessResult<DocumentTypeModel>> GetDocumentTypesAsync(BusinessQueryOptions options)
        {
            var query = _docTypeRepo.GetQueryable().AsNoTracking();

            // Searching
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                // PostgreSQL so sánh chuỗi có phân biệt hoa thường -> đưa cả hai vế về chữ thường
                string search = options.Search.Trim().ToLower();
                query = query.Where(d => d.Code.ToLower().Contains(search) || d.Name.ToLower().Contains(search));
            }

            // Sorting
            query = query.ApplySort(options.Sort, nameof(DocumentType.DocTypeId));

            // Paging
            return await query.ToPagedResultAsync(options, d => d.ToBusinessModel());
        }

        // Danh sách đầy đủ cho dropdown ở FE: ít thay đổi, đọc nhiều -> cache lại.
        public async Task<IReadOnlyList<DocumentTypeModel>> GetLookupAsync()
        {
            return await _cache.GetOrSetAsync(CacheKeys.DocumentTypesAll, async () =>
            {
                var entities = await _docTypeRepo.GetQueryable().AsNoTracking().OrderBy(d => d.Name).ToListAsync();
                return entities.Select(d => d.ToBusinessModel()).ToList();
            }, LookupCacheTtl);
        }

        public async Task<DocumentTypeModel> GetDocumentTypeByIdAsync(int id)
        {
            var entity = await _docTypeRepo.GetByIdAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Document type with ID {id} was not found.");
            }

            return entity.ToBusinessModel();
        }

        public async Task<DocumentTypeModel> CreateDocumentTypeAsync(DocumentTypeModel model)
        {
            if (await _docTypeRepo.ExistsAsync(d => d.Code == model.Code))
            {
                throw new BusinessException($"Document type code '{model.Code}' already exists.");
            }

            var entity = model.ToEntity();
            await _docTypeRepo.AddAsync(entity);
            await _cache.RemoveAsync(CacheKeys.DocumentTypesAll);

            return entity.ToBusinessModel();
        }

        public async Task<DocumentTypeModel> UpdateDocumentTypeAsync(int id, DocumentTypeModel model)
        {
            var existing = await _docTypeRepo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException($"Document type with ID {id} was not found.");
            }

            if (await _docTypeRepo.ExistsAsync(d => d.DocTypeId != id && d.Code == model.Code))
            {
                throw new BusinessException($"Document type code '{model.Code}' already exists.");
            }

            existing.Code = model.Code;
            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.IsMandatory = model.IsMandatory;

            await _docTypeRepo.UpdateAsync(existing);
            await _cache.RemoveAsync(CacheKeys.DocumentTypesAll);

            return existing.ToBusinessModel();
        }

        public async Task DeleteDocumentTypeAsync(int id)
        {
            var existing = await _docTypeRepo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException($"Document type with ID {id} was not found.");
            }

            // Không xóa được loại giấy tờ đang được dùng (DB sẽ báo lỗi khóa ngoại) -> báo lỗi rõ ràng trước.
            bool inUse = await _documentRepo.ExistsAsync(d => d.DocTypeId == id)
                      || await _requirementRepo.ExistsAsync(r => r.DocTypeId == id);
            if (inUse)
            {
                throw new BusinessException("This document type is in use and cannot be deleted.");
            }

            await _docTypeRepo.DeleteAsync(existing);
            await _cache.RemoveAsync(CacheKeys.DocumentTypesAll);
        }
    }
}
