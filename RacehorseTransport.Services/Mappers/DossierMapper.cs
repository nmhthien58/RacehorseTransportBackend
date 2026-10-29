using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class DossierMapper
    {
        // Cần Include: Specialist, BookingHorse -> Horse, BookingHorse -> Booking -> Customer
        public static DossierSummaryModel ToSummaryModel(this DigitalDossier entity)
        {
            var model = new DossierSummaryModel();
            FillSummary(model, entity);
            return model;
        }

        // Cần Include thêm: Documents -> DocumentType / UploadedBy / ReviewedBy
        public static DossierDetailModel ToDetailModel(this DigitalDossier entity)
        {
            var model = new DossierDetailModel
            {
                PassportNumber = entity.BookingHorse?.Horse?.PassportNumber ?? string.Empty,
                IssueReason = entity.IssueReason,
                Documents = entity.Documents
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => d.ToBusinessModel())
                    .ToList()
            };

            FillSummary(model, entity);
            return model;
        }

        private static bool IsFitExam(DossierDocument entity) => entity.DocumentType?.Code == DocumentTypeCodes.FitExam;

        public static DossierDocumentModel ToBusinessModel(this DossierDocument entity)
        {
            return new DossierDocumentModel
            {
                DocumentId = entity.DocumentId,
                DocTypeId = entity.DocTypeId,
                DocTypeCode = entity.DocumentType?.Code ?? string.Empty,
                DocTypeName = entity.DocumentType?.Name ?? string.Empty,
                DocumentNumber = entity.DocumentNumber,
                FileUrl = entity.FileUrl,
                Status = entity.Status,
                CorrectionNote = entity.CorrectionNote,
                // FIT_EXAM lưu ngày cấp ở cột expiry_date (xem FitExamRules.IssueDateOf)
                ExpiryDate = IsFitExam(entity) ? null : entity.ExpiryDate,
                IssueDate = IsFitExam(entity) ? entity.ExpiryDate : null,
                RejectionCount = entity.RejectionCount,
                UploadedByUserId = entity.UploadedByUserId,
                UploadedByName = entity.UploadedBy?.FullName,
                UploadedAt = entity.UploadedAt,
                ReviewedByName = entity.ReviewedBy?.FullName,
                ReviewedAt = entity.ReviewedAt
            };
        }

        private static void FillSummary(DossierSummaryModel model, DigitalDossier entity)
        {
            var bookingHorse = entity.BookingHorse;
            var booking = bookingHorse?.Booking;
            var horse = bookingHorse?.Horse;

            model.DossierId = entity.DossierId;
            model.DossierCode = entity.DossierCode;
            model.Status = entity.Status;
            model.BookingHorseId = entity.BookingHorseId;
            model.BookingId = bookingHorse?.BookingId ?? 0;
            model.BookingCode = booking?.BookingCode ?? string.Empty;
            model.CustomerName = booking?.Customer?.FullName;
            model.HorseId = bookingHorse?.HorseId ?? 0;
            model.HorseName = horse?.Name ?? string.Empty;
            model.MicrochipNumber = horse?.MicrochipNumber ?? string.Empty;
            model.PickupCountryCode = booking?.PickupCountryCode ?? string.Empty;
            model.DropoffCountryCode = booking?.DropoffCountryCode ?? string.Empty;
            model.TransitCountryCodes = booking?.TransitCountries
                .OrderBy(t => t.SequenceOrder)
                .Select(t => t.CountryCode)
                .ToList() ?? new List<string>();
            model.PickupWindowStart = booking?.PickupWindowStart ?? default;
            model.PickupWindowEnd = booking?.PickupWindowEnd ?? default;
            model.SpecialistUserId = entity.SpecialistUserId;
            model.SpecialistName = entity.Specialist?.FullName;
            model.ClearanceNumber = entity.ClearanceNumber;
            model.ClearedAt = entity.ClearedAt;
            model.CreatedAt = entity.CreatedAt;
        }
    }
}
