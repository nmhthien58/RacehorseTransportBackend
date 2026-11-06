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
    public class TransportAssetService : ITransportAssetService
    {
        private readonly IRepository<TransportAsset> _assetRepo;
        private readonly IRepository<Trip> _tripRepo;

        public TransportAssetService(
            IRepository<TransportAsset> assetRepo,
            IRepository<Trip> tripRepo)
        {
            _assetRepo = assetRepo;
            _tripRepo = tripRepo;
        }

        public async Task<PagedBusinessResult<TransportAssetModel>> GetTransportAssetsAsync(
            TransportAssetQueryOptions options)
        {
            var query = _assetRepo.GetQueryable().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                string search = options.Search.Trim().ToLower();
                query = query.Where(a => a.AssetCode.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(options.Status))
            {
                string status = options.Status.Trim();
                EnsureAllowedValue(status, AssetStatuses.All, "Status");
                query = query.Where(a => a.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(options.AssetType))
            {
                string assetType = options.AssetType.Trim();
                EnsureAllowedValue(assetType, AssetTypes.All, "AssetType");
                query = query.Where(a => a.AssetType == assetType);
            }

            query = query.ApplySort(options.Sort, nameof(TransportAsset.AssetId));
            return await query.ToPagedResultAsync(options, a => a.ToBusinessModel());
        }

        public async Task<TransportAssetModel> GetTransportAssetByIdAsync(int id)
        {
            var entity = await _assetRepo.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AssetId == id);

            if (entity == null)
            {
                throw new NotFoundException($"Transport asset with ID {id} was not found.");
            }

            return entity.ToBusinessModel();
        }

        public async Task<TransportAssetModel> CreateTransportAssetAsync(TransportAssetModel model)
        {
            NormalizeAndValidate(model);

            if (await _assetRepo.ExistsAsync(a => a.AssetCode == model.AssetCode))
            {
                throw new BusinessException($"Transport asset code '{model.AssetCode}' already exists.");
            }

            var entity = model.ToEntity();
            try
            {
                await _assetRepo.AddAsync(entity);
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation())
            {
                throw new BusinessException($"Transport asset code '{model.AssetCode}' already exists.");
            }

            return entity.ToBusinessModel();
        }

        public async Task<TransportAssetModel> UpdateTransportAssetAsync(int id, TransportAssetModel model)
        {
            var existing = await _assetRepo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException($"Transport asset with ID {id} was not found.");
            }

            NormalizeAndValidate(model);

            if (await _assetRepo.ExistsAsync(a => a.AssetId != id && a.AssetCode == model.AssetCode))
            {
                throw new BusinessException($"Transport asset code '{model.AssetCode}' already exists.");
            }

            existing.AssetCode = model.AssetCode;
            existing.AssetType = model.AssetType;
            existing.CapacityHorses = model.CapacityHorses;
            existing.CompartmentCount = model.CompartmentCount;
            existing.HasClimateControl = model.HasClimateControl;
            existing.HasGpsTracker = model.HasGpsTracker;
            existing.Status = model.Status;
            existing.LastSanitizationDate = model.LastSanitizationDate;

            try
            {
                await _assetRepo.UpdateAsync(existing);
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation())
            {
                throw new BusinessException($"Transport asset code '{model.AssetCode}' already exists.");
            }

            return existing.ToBusinessModel();
        }

        public async Task DeleteTransportAssetAsync(int id)
        {
            var existing = await _assetRepo.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException($"Transport asset with ID {id} was not found.");
            }

            if (await _tripRepo.ExistsAsync(t => t.VehicleId == id))
            {
                throw new BusinessException("This transport asset has been assigned to a trip and cannot be deleted.");
            }

            try
            {
                await _assetRepo.DeleteAsync(existing);
            }
            catch (DbUpdateException exception) when (exception.IsForeignKeyViolation())
            {
                throw new BusinessException("This transport asset has been assigned to a trip and cannot be deleted.");
            }
        }

        private static void NormalizeAndValidate(TransportAssetModel model)
        {
            model.AssetCode = model.AssetCode.Trim().ToUpperInvariant();
            model.AssetType = model.AssetType.Trim();
            model.Status = model.Status.Trim();

            if (string.IsNullOrWhiteSpace(model.AssetCode) || model.AssetCode.Length > 30)
            {
                throw new BusinessException("AssetCode is required and cannot exceed 30 characters.");
            }

            EnsureAllowedValue(model.AssetType, AssetTypes.All, "AssetType");
            EnsureAllowedValue(model.Status, AssetStatuses.All, "Status");

            if (model.CapacityHorses is < 1 or > 12)
            {
                throw new BusinessException("CapacityHorses must be between 1 and 12.");
            }

            // Mỗi khoang 3 vị trí: số khoang phải đủ chỗ cho sức chứa của xe
            if (model.CompartmentCount == 0)
            {
                model.CompartmentCount = CompartmentRules.DefaultCompartmentCount(model.CapacityHorses);
            }
            if (model.CompartmentCount < 1 || model.CompartmentCount > VehicleCompartments.MaxCompartments)
            {
                throw new BusinessException($"CompartmentCount must be between 1 and {VehicleCompartments.MaxCompartments}.");
            }
            if (model.CompartmentCount * VehicleCompartments.PositionsPerCompartment < model.CapacityHorses)
            {
                throw new BusinessException(
                    $"{model.CompartmentCount} compartment(s) hold at most {model.CompartmentCount * VehicleCompartments.PositionsPerCompartment} horses; increase CompartmentCount or lower CapacityHorses.");
            }
        }

        private static void EnsureAllowedValue(string value, IReadOnlyCollection<string> allowedValues, string fieldName)
        {
            if (!allowedValues.Contains(value))
            {
                throw new BusinessException(
                    $"{fieldName} '{value}' is invalid. Allowed values: {string.Join(", ", allowedValues)}.");
            }
        }
    }
}
