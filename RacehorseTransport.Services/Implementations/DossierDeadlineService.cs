using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // R-10 (BE-06): job hạn nộp giấy. Đơn đã cọc có docs_due_at (= lúc cọc + Dossier.SubmitDays, hoặc gia hạn khi công ty đổi tuyến).
    //   Còn 3 ngày / 1 ngày mà còn thiếu giấy -> email DocumentsReminder cho từng ngựa còn thiếu.
    //   Quá hạn còn thiếu -> hủy đơn, hoàn tiền như khách tự hủy sau cọc (hoàn 100% nếu đang có thay đổi tuyến do công ty), email BookingCancelledMissingDocs.
    //   Ngựa đã nằm trên chuyến thì không tự hủy: báo Manager xử lý.
    public class DossierDeadlineService : IDossierDeadlineService
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly string[] DocsPhaseStatuses = { BookingStatuses.Approved, BookingStatuses.Assigned };

        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IDossierChecklistService _checklistService;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public DossierDeadlineService(
            IRepository<BookingRequest> bookingRepo,
            IRepository<TripHorse> tripHorseRepo,
            IDossierChecklistService checklistService,
            IPaymentService paymentService,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _bookingRepo = bookingRepo;
            _tripHorseRepo = tripHorseRepo;
            _checklistService = checklistService;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> ProcessDocumentDeadlinesAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var bookings = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses)
                .Where(b => DocsPhaseStatuses.Contains(b.Status)
                    && b.DocsDueAt != null
                    && b.DocsRemindersSent < DocumentDeadlineRules.OverdueHandled)
                .ToListAsync();

            int processed = 0;
            foreach (var booking in bookings)
            {
                var action = DocumentDeadlineRules.Decide(booking.DocsDueAt!.Value, booking.DocsRemindersSent, now);
                if (action == DocumentDeadlineRules.Action.None)
                {
                    continue;
                }
                var missing = await _checklistService.GetMissingDocumentsAsync(booking.BookingId);
                if (missing.Count == 0)
                {
                    continue; // đã nộp đủ (đang chờ duyệt cũng tính là đã nộp)
                }

                if (action == DocumentDeadlineRules.Action.Expire)
                {
                    await ExpireAsync(booking, missing);
                }
                else
                {
                    await RemindAsync(booking, missing, action == DocumentDeadlineRules.Action.FirstReminder ? 1 : 2);
                }
                processed++;
            }
            return processed;
        }

        private async Task RemindAsync(BookingRequest booking, List<MissingDocumentsModel> missing, int reminderNumber)
        {
            booking.DocsRemindersSent = reminderNumber;
            await _bookingRepo.SaveChangesAsync();

            string deadline = VietnamTime(booking.DocsDueAt!.Value);
            foreach (var horse in missing)
            {
                string list = string.Join(", ", horse.DocumentNames);
                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Documents still missing for {horse.HorseName}",
                    $"Booking {booking.BookingCode}: please upload {list} by {deadline} (Vietnam time). After that the booking is cancelled under the cancellation policy.",
                    NotificationReferenceTypes.Dossier,
                    horse.DossierId);
                await _notificationService.EmailCustomerAsync(
                    booking.CustomerUserId,
                    EmailTemplateNames.DocumentsReminder,
                    CustomerPages.Booking(booking.BookingId),
                    new Dictionary<string, string>
                    {
                        ["BookingCode"] = booking.BookingCode,
                        ["HorseName"] = horse.HorseName,
                        ["DocumentList"] = list,
                        ["Deadline"] = deadline,
                        ["ReminderNumber"] = reminderNumber.ToString(CultureInfo.InvariantCulture)
                    });
            }
        }

        private async Task ExpireAsync(BookingRequest booking, List<MissingDocumentsModel> missing)
        {
            string summary = string.Join("; ", missing.Select(m => $"{m.HorseName}: {string.Join(", ", m.DocumentNames)}"));
            var horseIds = booking.BookingHorses.Select(bh => bh.BookingHorseId).ToList();
            string? tripCode = await _tripHorseRepo.GetQueryable()
                .Where(th => horseIds.Contains(th.BookingHorseId)
                    && th.Trip!.OverallStatus != TripStatuses.Cancelled
                    && TripHorseStatuses.Holding.Contains(th.Status))
                .Select(th => th.Trip!.TripCode)
                .FirstOrDefaultAsync();

            if (tripCode != null)
            {
                // Ngựa đã được xếp chuyến: không tự hủy, để Manager quyết định
                booking.DocsRemindersSent = DocumentDeadlineRules.OverdueHandled;
                await _bookingRepo.SaveChangesAsync();
                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Documents overdue: {booking.BookingCode}",
                    $"The document deadline passed with documents still missing ({summary}), but the horses are already planned on trip {tripCode}. Please decide how to proceed.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
                return;
            }

            // Đang có thay đổi tuyến do công ty (nước mới cần giấy) -> khách không có lỗi, hoàn 100%
            bool fullRefund = booking.RouteChangeCancelUntil != null;
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                booking.Status = BookingStatuses.Cancelled;
                booking.RouteChangeCancelUntil = null;
                foreach (var horse in booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled))
                {
                    horse.Status = BookingHorseStatuses.Cancelled;
                }
                await _bookingRepo.UpdateAsync(booking);

                var staff = new List<int>();
                if (booking.AssignedSpecialistId.HasValue)
                {
                    staff.Add(booking.AssignedSpecialistId.Value);
                }
                await _notificationService.SendToUsersAsync(
                    staff,
                    $"Booking {booking.BookingCode} cancelled: documents missing",
                    $"The document deadline passed with documents still missing ({summary}).",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Booking {booking.BookingCode} cancelled: documents missing",
                    $"The document deadline passed with documents still missing ({summary}). Refunds follow the cancellation policy.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            // Hoàn tiền sau khi đã chốt hủy (gọi cổng thanh toán, không giữ transaction). Người thực hiện ghi là Manager đã duyệt đơn.
            var actor = new CurrentUser
            {
                UserId = booking.ReviewedByUserId ?? booking.AssignedSpecialistId ?? booking.CustomerUserId,
                Role = UserRoles.LogisticsManager
            };
            var refunds = await _paymentService.RefundForCancellationAsync(booking, actor, fullRefund);
            decimal refunded = refunds.Sum(r => r.Amount);

            await _notificationService.EmailCustomerAsync(
                booking.CustomerUserId,
                EmailTemplateNames.BookingCancelledMissingDocs,
                CustomerPages.Booking(booking.BookingId),
                new Dictionary<string, string>
                {
                    ["BookingCode"] = booking.BookingCode,
                    ["RefundAmount"] = $"{refunded.ToString("N2", CultureInfo.InvariantCulture)} {booking.CurrencyCode}"
                });
        }

        private static string VietnamTime(DateTimeOffset value) =>
            value.ToOffset(VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
