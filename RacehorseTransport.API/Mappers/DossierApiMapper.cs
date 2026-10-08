using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class DossierApiMapper
    {
        public static DossierFilterOptions ToBusinessOptions(this DossierQueryRequest request)
        {
            return new DossierFilterOptions
            {
                Search = request.Search,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size,
                Status = request.Status,
                BookingId = request.BookingId
            };
        }

        // Stream của file do controller giữ và giải phóng (request.File.ToUploadModel())
        public static UploadDocumentModel ToBusinessModel(this UploadDocumentRequest request, FileUploadModel file)
        {
            return new UploadDocumentModel
            {
                DocTypeId = request.DocTypeId,
                DocumentNumber = string.IsNullOrWhiteSpace(request.DocumentNumber) ? null : request.DocumentNumber.Trim(),
                File = file,
                ExpiryDate = request.ExpiryDate
            };
        }

        public static DossierSummaryResponse ToResponse(this DossierSummaryModel model)
        {
            var response = new DossierSummaryResponse();
            FillSummary(response, model);
            return response;
        }

        public static DossierDetailResponse ToResponse(this DossierDetailModel model)
        {
            var response = new DossierDetailResponse
            {
                PassportNumber = model.PassportNumber,
                IssueReason = model.IssueReason,
                RequiredCount = model.RequiredCount,
                ApprovedCount = model.ApprovedCount,
                IsReadyToSubmit = model.IsReadyToSubmit,
                Checklist = model.Checklist.Select(c => new DossierChecklistItemResponse
                {
                    DocTypeId = c.DocTypeId,
                    DocTypeCode = c.DocTypeCode,
                    DocTypeName = c.DocTypeName,
                    IsMandatory = c.IsMandatory,
                    ValidityDays = c.ValidityDays,
                    RegulationNote = c.RegulationNote,
                    RequiredBy = c.RequiredBy,
                    State = c.State,
                    LatestDocument = c.LatestDocument?.ToResponse()
                }).ToList(),
                Documents = model.Documents.Select(d => d.ToResponse()).ToList()
            };

            FillSummary(response, model);
            return response;
        }

        public static DossierDocumentResponse ToResponse(this DossierDocumentModel model)
        {
            return new DossierDocumentResponse
            {
                DocumentId = model.DocumentId,
                DocTypeId = model.DocTypeId,
                DocTypeCode = model.DocTypeCode,
                DocTypeName = model.DocTypeName,
                DocumentNumber = model.DocumentNumber,
                FileUrl = model.FileUrl,
                Status = model.Status,
                CorrectionNote = model.CorrectionNote,
                ExpiryDate = model.ExpiryDate,
                RejectionCount = model.RejectionCount,
                UploadedByUserId = model.UploadedByUserId,
                UploadedByName = model.UploadedByName,
                UploadedAt = model.UploadedAt,
                ReviewedByName = model.ReviewedByName,
                ReviewedAt = model.ReviewedAt
            };
        }

        private static void FillSummary(DossierSummaryResponse response, DossierSummaryModel model)
        {
            response.DossierId = model.DossierId;
            response.DossierCode = model.DossierCode;
            response.Status = model.Status;
            response.BookingHorseId = model.BookingHorseId;
            response.BookingId = model.BookingId;
            response.BookingCode = model.BookingCode;
            response.CustomerName = model.CustomerName;
            response.HorseId = model.HorseId;
            response.HorseName = model.HorseName;
            response.MicrochipNumber = model.MicrochipNumber;
            response.PickupCountryCode = model.PickupCountryCode;
            response.DropoffCountryCode = model.DropoffCountryCode;
            response.DepartureDate = model.DepartureDate;
            response.SpecialistUserId = model.SpecialistUserId;
            response.SpecialistName = model.SpecialistName;
            response.ClearanceNumber = model.ClearanceNumber;
            response.ClearedAt = model.ClearedAt;
            response.CreatedAt = model.CreatedAt;
        }
    }
}
