using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // R-10 (BE-06): giấy khách còn phải nộp của từng ngựa, dùng cho job nhắc / hủy khi quá hạn nộp giấy
    public partial class DossierService : IDossierChecklistService
    {
        public async Task<List<MissingDocumentsModel>> GetMissingDocumentsAsync(int bookingId)
        {
            var dossiers = await _dossierRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(d => d.BookingHorse!).ThenInclude(bh => bh.Horse)
                .Include(d => d.BookingHorse!).ThenInclude(bh => bh.Booking)
                .Include(d => d.Documents)
                .Where(d => d.BookingHorse!.BookingId == bookingId
                    && d.BookingHorse.Status != BookingHorseStatuses.Cancelled
                    && d.Status != DossierStatuses.Cleared)
                .ToListAsync();

            var result = new List<MissingDocumentsModel>();
            foreach (var dossier in dossiers)
            {
                var checklist = await BuildChecklistAsync(dossier.BookingHorse!.Booking!, dossier.BookingHorseId, ToDocumentModels(dossier));
                // Khách còn phải nộp: giấy bắt buộc chưa có bản nào, hoặc bản mới nhất bị từ chối (đang chờ duyệt thì coi như đã nộp).
                // Giấy công ty xin (R-19) không tính: khách không bị nhắc hay bị hủy đơn vì giấy đó.
                // FIT_EXAM (PostClearance) có nhắc riêng theo giờ xuất phát, không tính hạn nộp giấy.
                var missing = FitExamRules.RequiredForClearance(checklist)
                    .Where(c => c.ProvidedBy == DocumentProviders.Customer && (c.State == ChecklistStates.Missing || c.State == DocumentStatuses.Rejected))
                    .Select(c => c.DocTypeName)
                    .ToList();
                if (missing.Count > 0)
                {
                    result.Add(new MissingDocumentsModel
                    {
                        DossierId = dossier.DossierId,
                        BookingHorseId = dossier.BookingHorseId,
                        HorseName = dossier.BookingHorse.Horse?.Name ?? $"#{dossier.BookingHorseId}",
                        DocumentNames = missing
                    });
                }
            }
            return result;
        }
    }
}
