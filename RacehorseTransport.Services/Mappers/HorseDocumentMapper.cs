using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class HorseDocumentMapper
    {
        // Cần Include DocumentType và UploadedBy
        public static HorseDocumentModel ToBusinessModel(this HorseDocument entity, DateOnly today)
        {
            return new HorseDocumentModel
            {
                DocumentId = entity.DocumentId,
                HorseId = entity.HorseId,
                DocTypeId = entity.DocTypeId,
                DocTypeCode = entity.DocumentType?.Code ?? string.Empty,
                DocTypeName = entity.DocumentType?.Name ?? string.Empty,
                DocumentNumber = entity.DocumentNumber,
                IssuedBy = entity.IssuedBy,
                IssueDate = entity.IssueDate,
                ExpiryDate = entity.ExpiryDate,
                Validity = GetValidity(entity.ExpiryDate, today),
                FileUrl = entity.FileUrl,
                Notes = entity.Notes,
                UploadedByUserId = entity.UploadedByUserId,
                UploadedByName = entity.UploadedBy?.FullName,
                UploadedAt = entity.UploadedAt
            };
        }

        private static string GetValidity(DateOnly? expiryDate, DateOnly today)
        {
            if (!expiryDate.HasValue)
            {
                return HorseDocumentValidity.NoExpiry;
            }

            if (expiryDate.Value < today)
            {
                return HorseDocumentValidity.Expired;
            }

            return expiryDate.Value <= today.AddDays(HorseDocumentValidity.ExpiringSoonDays)
                ? HorseDocumentValidity.ExpiringSoon
                : HorseDocumentValidity.Valid;
        }
    }
}
