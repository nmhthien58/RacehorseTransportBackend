using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    // Flow 2. Vòng đời hồ sơ:
    //   Draft -> AwaitingDocs (đã yêu cầu khách nộp) <-> Reviewing (khách đã nộp, chờ thẩm định)
    //         -> SubmittedToAuthorities -> Cleared
    //   Issue: có vấn đề (giấy bị từ chối 3 lần, hoặc cơ quan chức năng không chấp nhận); Specialist yêu cầu nộp lại để tiếp tục.
    public partial class DossierService
    {
        // Hồ sơ còn nhận và thẩm định giấy tờ ở các trạng thái này
        private static readonly string[] OpenForDocuments =
        {
            DossierStatuses.Draft, DossierStatuses.AwaitingDocs, DossierStatuses.Reviewing
        };

        public async Task<DossierDetailModel> RequestDocumentsAsync(int dossierId, string? note, CurrentUser currentUser)
        {
            var dossier = await GetTrackedDossierAsync(dossierId);
            EnsureSpecialistInCharge(dossier, currentUser);

            if (dossier.Status == DossierStatuses.SubmittedToAuthorities || dossier.Status == DossierStatuses.Cleared)
            {
                throw new BusinessException($"Documents can no longer be requested for a dossier in status {dossier.Status}.");
            }

            var booking = dossier.BookingHorse!.Booking!;
            var checklist = await BuildChecklistAsync(
                booking.PickupCountryCode, booking.DropoffCountryCode, ToDocumentModels(dossier));

            var outstanding = checklist
                .Where(c => c.IsMandatory && c.State != DocumentStatuses.Approved && c.State != DocumentStatuses.Pending)
                .Select(c => c.DocTypeName)
                .ToList();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                dossier.Status = DossierStatuses.AwaitingDocs;
                await _dossierRepo.UpdateAsync(dossier);

                string message = outstanding.Count > 0
                    ? $"Please upload: {string.Join("; ", outstanding)}."
                    : "Please check the dossier and upload the requested documents.";
                if (!string.IsNullOrWhiteSpace(note))
                {
                    message += $" Note from the specialist: {note.Trim()}";
                }

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Documents needed for {dossier.BookingHorse.Horse!.Name}",
                    message,
                    NotificationReferenceTypes.Dossier,
                    dossier.DossierId);
            });

            return await GetDossierByIdAsync(dossierId, currentUser);
        }

        // Mỗi loại giấy trong một hồ sơ chỉ có một bản: nộp lại là thay file của bản đó.
        public async Task<DossierDetailModel> UploadDocumentAsync(int dossierId, UploadDocumentModel model, CurrentUser currentUser)
        {
            var dossier = await GetTrackedDossierAsync(dossierId);
            var booking = dossier.BookingHorse!.Booking!;

            // Chủ ngựa nộp; Specialist phụ trách cũng có thể nộp hộ khi khách gửi giấy qua kênh khác
            bool isOwner = currentUser.IsCustomer && booking.CustomerUserId == currentUser.UserId;
            if (!isOwner && !IsSpecialistInCharge(dossier, currentUser))
            {
                throw new ForbiddenException("You cannot upload documents to this dossier.");
            }

            if (!OpenForDocuments.Contains(dossier.Status))
            {
                throw new BusinessException($"Documents cannot be uploaded while the dossier is in status {dossier.Status}.");
            }

            var docType = await _docTypeRepo.GetByIdAsync(model.DocTypeId);
            if (docType == null)
            {
                throw new BusinessException($"Document type with ID {model.DocTypeId} does not exist.");
            }

            if (!_fileStorage.IsValidPath(model.FilePath, FileCategories.Documents))
            {
                throw new BusinessException("FilePath must be the path returned by POST /api/files for category 'documents'.");
            }

            if (model.ExpiryDate.HasValue && model.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new BusinessException("The document has already expired.");
            }

            var existing = dossier.Documents.FirstOrDefault(d => d.DocTypeId == model.DocTypeId);
            if (existing != null)
            {
                if (existing.Status == DocumentStatuses.Approved)
                {
                    throw new BusinessException($"'{docType.Name}' has already been approved and cannot be replaced.");
                }
                // Hết lượt nộp lại của khách. Specialist vẫn nộp hộ được sau khi đã làm việc trực tiếp với khách.
                if (isOwner && existing.Status == DocumentStatuses.Rejected && existing.RejectionCount >= DocumentStatuses.MaxRejectionCount)
                {
                    throw new BusinessException($"'{docType.Name}' was rejected {DocumentStatuses.MaxRejectionCount} times and can no longer be resubmitted. Please contact the specialist.");
                }
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (existing == null)
                {
                    await _documentRepo.AddAsync(new DossierDocument
                    {
                        DossierId = dossier.DossierId,
                        DocTypeId = model.DocTypeId,
                        DocumentNumber = model.DocumentNumber,
                        FileUrl = model.FilePath,
                        ExpiryDate = model.ExpiryDate,
                        UploadedByUserId = currentUser.UserId,
                        Status = DocumentStatuses.Pending
                    });
                }
                else
                {
                    existing.DocumentNumber = model.DocumentNumber;
                    existing.FileUrl = model.FilePath;
                    existing.ExpiryDate = model.ExpiryDate;
                    existing.UploadedByUserId = currentUser.UserId;
                    existing.UploadedAt = DateTimeOffset.UtcNow;
                    existing.Status = DocumentStatuses.Pending;
                    existing.CorrectionNote = null;
                    existing.ReviewedByUserId = null;
                    existing.ReviewedAt = null;

                    // Specialist nộp hộ thì tính lại lượt từ đầu
                    if (!isOwner)
                    {
                        existing.RejectionCount = 0;
                    }
                }

                dossier.Status = DossierStatuses.Reviewing;
                await _dossierRepo.UpdateAsync(dossier);

                if (currentUser.UserId != dossier.SpecialistUserId)
                {
                    await _notificationService.SendAsync(
                        dossier.SpecialistUserId,
                        $"Document uploaded for dossier {dossier.DossierCode}",
                        $"'{docType.Name}' for {dossier.BookingHorse.Horse!.Name} is waiting for your review.",
                        NotificationReferenceTypes.Dossier,
                        dossier.DossierId);
                }
            });

            return await GetDossierByIdAsync(dossierId, currentUser);
        }

        public async Task<DossierDetailModel> ApproveDocumentAsync(int documentId, CurrentUser currentUser)
        {
            var (dossier, document) = await GetDocumentForReviewAsync(documentId, currentUser);
            var booking = dossier.BookingHorse!.Booking!;

            // Giấy phải còn hiệu lực tới ngày khởi hành
            var departureDay = DateOnly.FromDateTime(booking.DepartureDate.UtcDateTime);
            if (document.ExpiryDate.HasValue && document.ExpiryDate.Value < departureDay)
            {
                throw new BusinessException(
                    $"'{document.DocumentType!.Name}' expires on {document.ExpiryDate:yyyy-MM-dd}, before the departure date {departureDay:yyyy-MM-dd}. Reject it and ask for a valid document.");
            }

            document.Status = DocumentStatuses.Approved;
            document.CorrectionNote = null;
            document.ReviewedByUserId = currentUser.UserId;
            document.ReviewedAt = DateTimeOffset.UtcNow;

            await _documentRepo.UpdateAsync(document);
            return await GetDossierByIdAsync(dossier.DossierId, currentUser);
        }

        // Từ chối kèm hướng dẫn sửa. Bị từ chối lần thứ 3 thì hồ sơ chuyển sang Issue và báo Manager.
        public async Task<DossierDetailModel> RejectDocumentAsync(int documentId, string correctionNote, CurrentUser currentUser)
        {
            var (dossier, document) = await GetDocumentForReviewAsync(documentId, currentUser);
            var booking = dossier.BookingHorse!.Booking!;
            string docName = document.DocumentType!.Name;
            string horseName = dossier.BookingHorse.Horse!.Name;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                document.Status = DocumentStatuses.Rejected;
                document.CorrectionNote = correctionNote.Trim();
                document.RejectionCount += 1;
                document.ReviewedByUserId = currentUser.UserId;
                document.ReviewedAt = DateTimeOffset.UtcNow;

                bool limitReached = document.RejectionCount >= DocumentStatuses.MaxRejectionCount;
                if (limitReached)
                {
                    dossier.Status = DossierStatuses.Issue;
                    dossier.IssueReason = $"'{docName}' was rejected {document.RejectionCount} times. Last reason: {document.CorrectionNote}";
                }
                else
                {
                    dossier.Status = DossierStatuses.AwaitingDocs;
                }

                await _dossierRepo.UpdateAsync(dossier);

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Document rejected for {horseName}",
                    limitReached
                        ? $"'{docName}' was rejected for the third time and can no longer be resubmitted online. Our specialist will contact you. Reason: {document.CorrectionNote}"
                        : $"'{docName}' needs to be corrected and uploaded again ({document.RejectionCount}/{DocumentStatuses.MaxRejectionCount}). Reason: {document.CorrectionNote}",
                    NotificationReferenceTypes.Dossier,
                    dossier.DossierId);

                if (limitReached)
                {
                    await _notificationService.SendToRoleAsync(
                        UserRoles.LogisticsManager,
                        $"Dossier {dossier.DossierCode} has an issue",
                        dossier.IssueReason!,
                        NotificationReferenceTypes.Dossier,
                        dossier.DossierId);
                }
            });

            return await GetDossierByIdAsync(dossier.DossierId, currentUser);
        }

        public async Task<DossierDetailModel> SubmitToAuthoritiesAsync(int dossierId, CurrentUser currentUser)
        {
            var dossier = await GetTrackedDossierAsync(dossierId);
            EnsureSpecialistInCharge(dossier, currentUser);

            if (!OpenForDocuments.Contains(dossier.Status))
            {
                throw new BusinessException($"A dossier in status {dossier.Status} cannot be submitted to the authorities.");
            }

            var booking = dossier.BookingHorse!.Booking!;
            var checklist = await BuildChecklistAsync(
                booking.PickupCountryCode, booking.DropoffCountryCode, ToDocumentModels(dossier));

            var notApproved = checklist
                .Where(c => c.IsMandatory && c.State != DocumentStatuses.Approved)
                .Select(c => $"{c.DocTypeName} ({c.State})")
                .ToList();

            if (notApproved.Count > 0)
            {
                throw new BusinessException($"Every mandatory document must be approved first. Outstanding: {string.Join("; ", notApproved)}.");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                dossier.Status = DossierStatuses.SubmittedToAuthorities;
                await _dossierRepo.UpdateAsync(dossier);

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Dossier submitted for {dossier.BookingHorse.Horse!.Name}",
                    "All documents are in order and the dossier has been submitted to the authorities for clearance.",
                    NotificationReferenceTypes.Dossier,
                    dossier.DossierId);
            });

            return await GetDossierByIdAsync(dossierId, currentUser);
        }

        public async Task<DossierDetailModel> ClearDossierAsync(int dossierId, string clearanceNumber, CurrentUser currentUser)
        {
            var dossier = await GetTrackedDossierAsync(dossierId);
            EnsureSpecialistInCharge(dossier, currentUser);

            if (dossier.Status != DossierStatuses.SubmittedToAuthorities)
            {
                throw new BusinessException($"Only a dossier submitted to the authorities can be cleared (current status: {dossier.Status}).");
            }

            var booking = dossier.BookingHorse!.Booking!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                dossier.Status = DossierStatuses.Cleared;
                dossier.ClearanceNumber = clearanceNumber.Trim();
                dossier.ClearedAt = DateTimeOffset.UtcNow;
                dossier.IssueReason = null;
                await _dossierRepo.UpdateAsync(dossier);

                // Còn hồ sơ nào của đơn chưa thông quan không (không tính ngựa đã bị hủy khỏi đơn)
                bool othersPending = await _dossierRepo.GetQueryable()
                    .AnyAsync(d => d.DossierId != dossier.DossierId
                        && d.BookingHorse!.BookingId == booking.BookingId
                        && d.BookingHorse.Status != BookingHorseStatuses.Cancelled
                        && d.Status != DossierStatuses.Cleared);

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    othersPending
                        ? $"{dossier.BookingHorse.Horse!.Name} is cleared for travel"
                        : $"Booking {booking.BookingCode} is fully cleared",
                    othersPending
                        ? $"Clearance number {dossier.ClearanceNumber}. Other horses in this booking are still being processed."
                        : "Every horse in this booking has completed quarantine and customs clearance.",
                    NotificationReferenceTypes.Dossier,
                    dossier.DossierId);

                if (!othersPending)
                {
                    await _notificationService.SendToRoleAsync(
                        UserRoles.FleetCoordinator,
                        $"Booking {booking.BookingCode} is fully cleared",
                        "All horses in this booking are cleared and ready to travel.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId);
                }
            });

            return await GetDossierByIdAsync(dossierId, currentUser);
        }

        public async Task<DossierDetailModel> MarkIssueAsync(int dossierId, string reason, CurrentUser currentUser)
        {
            var dossier = await GetTrackedDossierAsync(dossierId);
            EnsureSpecialistInCharge(dossier, currentUser);

            if (dossier.Status == DossierStatuses.Cleared || dossier.Status == DossierStatuses.Issue)
            {
                throw new BusinessException($"A dossier in status {dossier.Status} cannot be flagged with an issue.");
            }

            var booking = dossier.BookingHorse!.Booking!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                dossier.Status = DossierStatuses.Issue;
                dossier.IssueReason = reason.Trim();
                await _dossierRepo.UpdateAsync(dossier);

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Clearance issue for {dossier.BookingHorse.Horse!.Name}",
                    $"There is a problem with the clearance dossier: {dossier.IssueReason}",
                    NotificationReferenceTypes.Dossier,
                    dossier.DossierId);

                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Dossier {dossier.DossierCode} has an issue",
                    dossier.IssueReason,
                    NotificationReferenceTypes.Dossier,
                    dossier.DossierId);
            });

            return await GetDossierByIdAsync(dossierId, currentUser);
        }

        // ---------------------------------------------------------------------------------------------

        private async Task<DigitalDossier> GetTrackedDossierAsync(int dossierId)
        {
            var dossier = await _dossierRepo.GetQueryable()
                .Include(d => d.Documents).ThenInclude(doc => doc.DocumentType)
                .Include(d => d.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(d => d.BookingHorse!).ThenInclude(bh => bh.Booking)
                .FirstOrDefaultAsync(d => d.DossierId == dossierId);

            if (dossier == null)
            {
                throw new NotFoundException($"Dossier with ID {dossierId} was not found.");
            }
            return dossier;
        }

        // Thẩm định một giấy tờ: phải là Specialist phụ trách, hồ sơ còn mở, giấy đang chờ duyệt
        private async Task<(DigitalDossier Dossier, DossierDocument Document)> GetDocumentForReviewAsync(int documentId, CurrentUser currentUser)
        {
            int? dossierId = await _documentRepo.GetQueryable()
                .Where(d => d.DocumentId == documentId)
                .Select(d => (int?)d.DossierId)
                .FirstOrDefaultAsync();

            if (dossierId == null)
            {
                throw new NotFoundException($"Document with ID {documentId} was not found.");
            }

            var dossier = await GetTrackedDossierAsync(dossierId.Value);
            EnsureSpecialistInCharge(dossier, currentUser);

            if (!OpenForDocuments.Contains(dossier.Status))
            {
                throw new BusinessException($"Documents cannot be reviewed while the dossier is in status {dossier.Status}.");
            }

            var document = dossier.Documents.First(d => d.DocumentId == documentId);
            if (document.Status != DocumentStatuses.Pending)
            {
                throw new BusinessException($"Only a pending document can be reviewed (current status: {document.Status}).");
            }

            return (dossier, document);
        }

        private static bool IsSpecialistInCharge(DigitalDossier dossier, CurrentUser currentUser)
        {
            return currentUser.Role == UserRoles.Admin
                || (currentUser.Role == UserRoles.TransportSpecialist && dossier.SpecialistUserId == currentUser.UserId);
        }

        private static void EnsureSpecialistInCharge(DigitalDossier dossier, CurrentUser currentUser)
        {
            if (!IsSpecialistInCharge(dossier, currentUser))
            {
                throw new ForbiddenException("Only the specialist in charge of this dossier can do this.");
            }
        }

        private static List<DossierDocumentModel> ToDocumentModels(DigitalDossier dossier)
        {
            return dossier.Documents
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => d.ToBusinessModel())
                .ToList();
        }
    }
}
