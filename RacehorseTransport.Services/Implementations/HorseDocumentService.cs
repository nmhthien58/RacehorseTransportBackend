using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    // Hồ sơ thú y lưu theo ngựa. Chủ ngựa bổ sung dần (hộ chiếu, xét nghiệm, tiêm phòng...),
    // không qua duyệt; việc duyệt giấy tờ cho từng chuyến vẫn nằm ở hồ sơ thông quan (DossierService).
    public class HorseDocumentService : IHorseDocumentService
    {
        private readonly IRepository<Horse> _horseRepo;
        private readonly IRepository<HorseDocument> _documentRepo;
        private readonly IRepository<DocumentType> _docTypeRepo;
        private readonly IFileStorageService _fileStorage;

        public HorseDocumentService(
            IRepository<Horse> horseRepo,
            IRepository<HorseDocument> documentRepo,
            IRepository<DocumentType> docTypeRepo,
            IFileStorageService fileStorage)
        {
            _horseRepo = horseRepo;
            _documentRepo = documentRepo;
            _docTypeRepo = docTypeRepo;
            _fileStorage = fileStorage;
        }

        public async Task<List<HorseDocumentModel>> GetDocumentsAsync(int horseId, CurrentUser currentUser)
        {
            var horse = await _horseRepo.GetByIdAsync(horseId);
            if (horse == null)
            {
                throw new NotFoundException($"Horse with ID {horseId} was not found.");
            }

            if (currentUser.IsCustomer && horse.OwnerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only view your own horses.");
            }

            var documents = await _documentRepo.GetQueryable()
                .AsNoTracking()
                .Include(d => d.DocumentType)
                .Include(d => d.UploadedBy)
                .Where(d => d.HorseId == horseId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var models = documents.Select(d => d.ToBusinessModel(today)).ToList();
            await _fileStorage.ResolveViewUrlsAsync(models, d => d.FileUrl, (d, url) => d.FileUrl = url ?? string.Empty);
            return models;
        }

        public async Task<HorseDocumentModel> AddDocumentAsync(int horseId, CreateHorseDocumentModel model, CurrentUser currentUser)
        {
            await GetOwnedHorseAsync(horseId, currentUser);

            if (!await _docTypeRepo.ExistsAsync(t => t.DocTypeId == model.DocTypeId))
            {
                throw new BusinessException($"Document type with ID {model.DocTypeId} does not exist.");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (model.IssueDate.HasValue && model.IssueDate.Value > today)
            {
                throw new BusinessException("IssueDate cannot be in the future.");
            }

            if (model.IssueDate.HasValue && model.ExpiryDate.HasValue && model.ExpiryDate.Value < model.IssueDate.Value)
            {
                throw new BusinessException("ExpiryDate must be on or after IssueDate.");
            }

            // Mọi kiểm tra nghiệp vụ đã qua mới đẩy file lên storage
            var stored = await _fileStorage.UploadAsync(
                model.File.Content, model.File.FileName, model.File.SizeBytes, FileCategories.Documents);

            var document = new HorseDocument
            {
                HorseId = horseId,
                DocTypeId = model.DocTypeId,
                DocumentNumber = model.DocumentNumber,
                IssuedBy = model.IssuedBy,
                IssueDate = model.IssueDate,
                ExpiryDate = model.ExpiryDate,
                FileUrl = stored.Path,
                Notes = model.Notes,
                UploadedByUserId = currentUser.UserId
            };
            await _documentRepo.AddAsync(document);

            var created = await _documentRepo.GetQueryable()
                .AsNoTracking()
                .Include(d => d.DocumentType)
                .Include(d => d.UploadedBy)
                .FirstAsync(d => d.DocumentId == document.DocumentId);

            var result = created.ToBusinessModel(today);
            await _fileStorage.ResolveViewUrlsAsync(new[] { result }, d => d.FileUrl, (d, url) => d.FileUrl = url ?? string.Empty);
            return result;
        }

        public async Task DeleteDocumentAsync(int horseId, int documentId, CurrentUser currentUser)
        {
            await GetOwnedHorseAsync(horseId, currentUser);

            var document = await _documentRepo.GetByIdAsync(documentId);
            if (document == null || document.HorseId != horseId)
            {
                throw new NotFoundException($"Document with ID {documentId} was not found for horse {horseId}.");
            }

            await _documentRepo.DeleteAsync(document);
        }

        // Thêm / xóa hồ sơ: chỉ chủ ngựa, và ngựa chưa bị xóa
        private async Task<Horse> GetOwnedHorseAsync(int horseId, CurrentUser currentUser)
        {
            var horse = await _horseRepo.GetByIdAsync(horseId);
            if (horse == null || !horse.IsActive)
            {
                throw new NotFoundException($"Horse with ID {horseId} was not found.");
            }

            if (horse.OwnerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only modify your own horses.");
            }

            return horse;
        }
    }
}
