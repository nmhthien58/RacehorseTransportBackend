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
    public class CountryDocRequirementService : ICountryDocRequirementService
    {
        private readonly IRepository<CountryDocRequirement> _requirementRepo;
        private readonly IRepository<DocumentType> _docTypeRepo;

        public CountryDocRequirementService(
            IRepository<CountryDocRequirement> requirementRepo,
            IRepository<DocumentType> docTypeRepo)
        {
            _requirementRepo = requirementRepo;
            _docTypeRepo = docTypeRepo;
        }

        public async Task<PagedBusinessResult<CountryDocRequirementModel>> GetCountryDocRequirementsAsync(
            CountryDocRequirementQueryOptions options)
        {
            var query = _requirementRepo.GetQueryable()
                .AsNoTracking()
                .Include(r => r.DocumentType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(options.CountryCode))
            {
                string countryCode = NormalizeCountryCode(options.CountryCode);
                ValidateCountryCode(countryCode);
                query = query.Where(r => r.CountryCode == countryCode);
            }

            if (!string.IsNullOrWhiteSpace(options.Direction))
            {
                string direction = options.Direction.Trim();
                ValidateDirection(direction);
                query = query.Where(r => r.Direction == direction);
            }

            query = query.ApplySort(options.Sort, nameof(CountryDocRequirement.RequirementId));
            return await query.ToPagedResultAsync(options, r => r.ToBusinessModel());
        }

        public async Task<CountryDocRequirementModel> GetCountryDocRequirementByIdAsync(int id)
        {
            var entity = await _requirementRepo.GetQueryable()
                .AsNoTracking()
                .Include(r => r.DocumentType)
                .FirstOrDefaultAsync(r => r.RequirementId == id);

            if (entity == null)
            {
                throw new NotFoundException($"Country document requirement with ID {id} was not found.");
            }

            return entity.ToBusinessModel();
        }

        public async Task<CountryDocRequirementModel> CreateCountryDocRequirementAsync(
            CountryDocRequirementModel model)
        {
            NormalizeAndValidate(model);
            var documentType = await GetDocumentTypeAsync(model.DocTypeId);
            await EnsureUniqueAsync(model, excludeRequirementId: null);

            var entity = model.ToEntity();
            try
            {
                await _requirementRepo.AddAsync(entity);
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation())
            {
                throw DuplicateRequirementException(model);
            }

            return entity.ToBusinessModel(documentType);
        }

        public async Task<CountryDocRequirementModel> UpdateCountryDocRequirementAsync(
            int id,
            CountryDocRequirementModel model)
        {
            var existing = await _requirementRepo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException($"Country document requirement with ID {id} was not found.");
            }

            NormalizeAndValidate(model);
            var documentType = await GetDocumentTypeAsync(model.DocTypeId);
            await EnsureUniqueAsync(model, excludeRequirementId: id);

            existing.CountryCode = model.CountryCode;
            existing.Direction = model.Direction;
            existing.DocTypeId = model.DocTypeId;
            existing.ValidityDays = model.ValidityDays;
            existing.ProcessingDays = model.ProcessingDays;
            existing.IsMandatory = model.IsMandatory;
            existing.RegulationNote = model.RegulationNote;

            try
            {
                await _requirementRepo.UpdateAsync(existing);
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation())
            {
                throw DuplicateRequirementException(model);
            }

            return existing.ToBusinessModel(documentType);
        }

        public async Task DeleteCountryDocRequirementAsync(int id)
        {
            var existing = await _requirementRepo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException($"Country document requirement with ID {id} was not found.");
            }

            await _requirementRepo.DeleteAsync(existing);
        }

        private async Task<DocumentType> GetDocumentTypeAsync(int docTypeId)
        {
            var documentType = await _docTypeRepo.GetByIdAsync(docTypeId);
            if (documentType == null)
            {
                throw new BusinessException($"Document type with ID {docTypeId} does not exist.");
            }

            return documentType;
        }

        private async Task EnsureUniqueAsync(CountryDocRequirementModel model, int? excludeRequirementId)
        {
            bool duplicate = await _requirementRepo.ExistsAsync(r =>
                (!excludeRequirementId.HasValue || r.RequirementId != excludeRequirementId.Value)
                && r.CountryCode == model.CountryCode
                && r.Direction == model.Direction
                && r.DocTypeId == model.DocTypeId);

            if (duplicate)
            {
                throw DuplicateRequirementException(model);
            }
        }

        private static BusinessException DuplicateRequirementException(CountryDocRequirementModel model)
        {
            return new BusinessException(
                $"A document requirement for {model.CountryCode}, {model.Direction}, document type ID {model.DocTypeId} already exists.");
        }

        private static void NormalizeAndValidate(CountryDocRequirementModel model)
        {
            model.CountryCode = NormalizeCountryCode(model.CountryCode);
            model.Direction = model.Direction.Trim();
            model.RegulationNote = string.IsNullOrWhiteSpace(model.RegulationNote)
                ? null
                : model.RegulationNote.Trim();

            ValidateCountryCode(model.CountryCode);
            ValidateDirection(model.Direction);

            if (model.DocTypeId <= 0)
            {
                throw new BusinessException("DocTypeId must be a positive integer.");
            }

            if (model.ValidityDays.HasValue && model.ValidityDays.Value <= 0)
            {
                throw new BusinessException("ValidityDays must be greater than 0 when provided.");
            }

            if (model.ProcessingDays.HasValue && model.ProcessingDays.Value < 0)
            {
                throw new BusinessException("ProcessingDays cannot be negative.");
            }

            if (model.RegulationNote?.Length > 255)
            {
                throw new BusinessException("RegulationNote cannot exceed 255 characters.");
            }
        }

        private static string NormalizeCountryCode(string countryCode)
        {
            return countryCode.Trim().ToUpperInvariant();
        }

        private static void ValidateCountryCode(string countryCode)
        {
            if (countryCode.Length != 2 || countryCode.Any(c => c is < 'A' or > 'Z'))
            {
                throw new BusinessException("CountryCode must contain exactly two letters.");
            }
        }

        private static void ValidateDirection(string direction)
        {
            if (!RequirementDirections.All.Contains(direction))
            {
                throw new BusinessException(
                    $"Direction '{direction}' is invalid. Allowed values: {string.Join(", ", RequirementDirections.All)}.");
            }
        }
    }
}
