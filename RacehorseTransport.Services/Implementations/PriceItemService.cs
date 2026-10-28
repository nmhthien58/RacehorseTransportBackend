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
    public class PriceItemService : IPriceItemService
    {
        private readonly IRepository<PriceItem> _priceItemRepo;
        private readonly ICacheService _cache;

        public PriceItemService(IRepository<PriceItem> priceItemRepo, ICacheService cache)
        {
            _priceItemRepo = priceItemRepo;
            _cache = cache;
        }

        public async Task<PagedBusinessResult<PriceItemModel>> GetPriceItemsAsync(PriceItemQueryOptions options)
        {
            var query = _priceItemRepo.GetQueryable().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                string search = options.Search.Trim().ToLower();
                query = query.Where(p => p.ItemCode.ToLower().Contains(search)
                                      || p.ItemName.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(options.Category))
            {
                string category = options.Category.Trim();
                ValidateAllowedValue(nameof(options.Category), category, PriceCategories.All);
                query = query.Where(p => p.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(options.TransportMode))
            {
                string transportMode = options.TransportMode.Trim();
                ValidateAllowedValue(nameof(options.TransportMode), transportMode, TransportModes.All);
                query = query.Where(p => p.TransportMode == transportMode);
            }

            if (options.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == options.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(options.DestCountryCode))
            {
                query = query.Where(p => p.DestCountryCode == options.DestCountryCode);
            }

            if (options.GlobalOnly == true)
            {
                query = query.Where(p => p.DestCountryCode == null);
            }

            query = query.ApplySort(options.Sort, nameof(PriceItem.PriceItemId));
            return await query.ToPagedResultAsync(options, p => p.ToBusinessModel());
        }

        public async Task<PriceItemModel> GetPriceItemByIdAsync(int id)
        {
            var entity = await _priceItemRepo.GetByIdAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Price item with ID {id} was not found.");
            }

            return entity.ToBusinessModel();
        }

        public async Task<PriceItemModel> CreatePriceItemAsync(PriceItemModel model)
        {
            ValidateModel(model);
            await EnsureUniqueAsync(model.ItemCode, model.TransportMode, model.DestCountryCode);

            var entity = model.ToEntity();
            entity.PriceItemId = 0;
            entity.IsActive = true;

            try
            {
                await _priceItemRepo.AddAsync(entity);
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation())
            {
                throw DuplicatePriceItemException(model.ItemCode, model.TransportMode, model.DestCountryCode);
            }

            await _cache.RemoveAsync(CacheKeys.PriceItemsAll);

            return entity.ToBusinessModel();
        }

        public async Task<PriceItemModel> UpdatePriceItemAsync(int id, PriceItemModel model)
        {
            var existing = await _priceItemRepo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException($"Price item with ID {id} was not found.");
            }

            ValidateModel(model);
            await EnsureUniqueAsync(model.ItemCode, model.TransportMode, model.DestCountryCode, id);

            existing.Category = model.Category;
            existing.ItemCode = model.ItemCode;
            existing.ItemName = model.ItemName;
            existing.TransportMode = model.TransportMode;
            existing.DestCountryCode = model.DestCountryCode;
            existing.CalcType = model.CalcType;
            existing.Value = model.Value;
            existing.MinHorses = model.MinHorses;

            try
            {
                await _priceItemRepo.UpdateAsync(existing);
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation())
            {
                throw DuplicatePriceItemException(model.ItemCode, model.TransportMode, model.DestCountryCode);
            }

            await _cache.RemoveAsync(CacheKeys.PriceItemsAll);

            return existing.ToBusinessModel();
        }

        public async Task<PriceItemModel> SetActiveAsync(int id, bool isActive)
        {
            var existing = await _priceItemRepo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException($"Price item with ID {id} was not found.");
            }

            existing.IsActive = isActive;
            await _priceItemRepo.UpdateAsync(existing);
            await _cache.RemoveAsync(CacheKeys.PriceItemsAll);

            return existing.ToBusinessModel();
        }

        private async Task EnsureUniqueAsync(
            string itemCode,
            string? transportMode,
            string? destCountryCode,
            int? excludedId = null)
        {
            bool exists = await _priceItemRepo.ExistsAsync(p =>
                (!excludedId.HasValue || p.PriceItemId != excludedId.Value)
                && p.ItemCode == itemCode
                && p.TransportMode == transportMode
                && p.DestCountryCode == destCountryCode);

            if (exists)
            {
                throw DuplicatePriceItemException(itemCode, transportMode, destCountryCode);
            }
        }

        private static BusinessException DuplicatePriceItemException(
            string itemCode,
            string? transportMode,
            string? destCountryCode)
        {
            return new BusinessException(
                $"A price item with code '{itemCode}', transport mode '{transportMode ?? "Any"}' " +
                $"and destination country '{destCountryCode ?? "Any"}' already exists.");
        }

        private static void ValidateModel(PriceItemModel model)
        {
            ValidateAllowedValue(nameof(model.Category), model.Category, PriceCategories.All);
            ValidateAllowedValue(nameof(model.CalcType), model.CalcType, PriceCalcTypes.All);

            if (model.TransportMode != null)
            {
                ValidateAllowedValue(nameof(model.TransportMode), model.TransportMode, TransportModes.All);
            }

            if (model.DestCountryCode != null
                && (model.DestCountryCode.Length != 2
                    || model.DestCountryCode.Any(character => character < 'A' || character > 'Z')))
            {
                throw new BusinessException("DestCountryCode must contain exactly two letters when provided.");
            }

            if (model.Value is < 0 or > 99999999999999.9999m)
            {
                throw new BusinessException("Value must fit PostgreSQL numeric(18,4) and cannot be negative.");
            }

            if (model.MinHorses.HasValue && model.MinHorses.Value <= 0)
            {
                throw new BusinessException("MinHorses must be greater than zero when provided.");
            }
        }

        private static void ValidateAllowedValue(string fieldName, string value, IReadOnlyCollection<string> allowedValues)
        {
            if (!allowedValues.Contains(value))
            {
                throw new BusinessException(
                    $"{fieldName} '{value}' is invalid. Allowed values: {string.Join(", ", allowedValues)}.");
            }
        }
    }
}
