using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Chính sách mục 5: nhắc khách cho ngựa khám lâm sàng trong N giờ trước giờ xuất phát.
    //   Manager duyệt chuyến -> nhắc khám (hoặc báo khám lại nếu giấy đã duyệt không còn hợp với giờ đi mới).
    //   Còn vài ngày tới giờ đi mà ngựa chưa nộp giấy khám -> job nhắc thêm một lần.
    public class FitExamReminderService : IFitExamReminderService
    {
        private const string ReminderTitlePrefix = "Fit-to-travel exam reminder";

        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IRepository<SystemNotification> _notificationRepo;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly BookingOptions _bookingOptions;

        public FitExamReminderService(
            IRepository<TripHorse> tripHorseRepo,
            IRepository<SystemNotification> notificationRepo,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            IOptions<BookingOptions> bookingOptions)
        {
            _tripHorseRepo = tripHorseRepo;
            _notificationRepo = notificationRepo;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _bookingOptions = bookingOptions.Value;
        }

        public async Task NotifyTripScheduledAsync(int tripId)
        {
            var horses = await LoadHorsesAsync(_tripHorseRepo.GetQueryable().Where(th => th.TripId == tripId));
            int hours = _bookingOptions.FitExamValidHours;

            foreach (var horse in horses)
            {
                var approved = horse.FitExams.Where(f => FitExamRules.CountsAtDeparture(f.Status)).ToList();
                if (approved.Any(f => FitExamRules.IsIssuedInWindow(f.IssueDate, horse.PlannedStartDate, hours)))
                {
                    continue; // giấy đã duyệt vẫn hợp với giờ đi
                }

                // Có giấy đã duyệt nhưng ngoài khoảng mới: giờ xuất phát đã đổi khi duyệt lại chuyến
                await SendAsync(horse, retake: approved.Count > 0);
            }
        }

        public async Task<int> ProcessRemindersAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var until = now.AddDays(_bookingOptions.FitExamReminderDaysBeforeDeparture);
            var horses = await LoadHorsesAsync(_tripHorseRepo.GetQueryable()
                .Where(th => th.Trip!.PlannedStartDate > now && th.Trip.PlannedStartDate <= until));

            int sent = 0;
            foreach (var horse in horses)
            {
                // Đã nộp (đang chờ duyệt hoặc đã duyệt hợp lệ) thì không nhắc
                bool submitted = horse.FitExams.Any(f => f.Status == DocumentStatuses.Pending
                    || (FitExamRules.CountsAtDeparture(f.Status)
                        && FitExamRules.IsIssuedInWindow(f.IssueDate, horse.PlannedStartDate, _bookingOptions.FitExamValidHours)));
                if (submitted)
                {
                    continue;
                }

                // Chỉ nhắc một lần cho mỗi lần duyệt chuyến: dựa vào thông báo đã gửi, khỏi thêm cột
                string title = ReminderTitle(horse.HorseName);
                var since = horse.TripApprovedAt ?? DateTimeOffset.MinValue;
                bool alreadySent = await _notificationRepo.GetQueryable()
                    .AnyAsync(n => n.RecipientUserId == horse.CustomerUserId
                        && n.ReferenceType == NotificationReferenceTypes.Dossier
                        && n.ReferenceId == horse.DossierId
                        && n.Title == title
                        && n.CreatedAt >= since);
                if (alreadySent)
                {
                    continue;
                }

                await _unitOfWork.ExecuteInTransactionAsync(() => SendAsync(horse, retake: false));
                sent++;
            }
            return sent;
        }

        private async Task SendAsync(HorseOnTrip horse, bool retake)
        {
            var (start, end) = FitExamRules.ExamWindow(horse.PlannedStartDate, _bookingOptions.FitExamValidHours);
            string windowStart = FitExamRules.VietnamTime(start);
            string windowEnd = FitExamRules.VietnamTime(end);

            await _notificationService.SendAsync(
                horse.CustomerUserId,
                retake ? $"New fit-to-travel exam needed: {horse.HorseName}" : ReminderTitle(horse.HorseName),
                retake
                    ? $"The departure time of trip {horse.TripCode} has changed, so the approved fit-to-travel exam of {horse.HorseName} is no longer valid. Please have the horse examined again between {windowStart} and {windowEnd} (Vietnam time) and upload the new certificate to the dossier."
                    : $"Please have {horse.HorseName} examined by a veterinarian between {windowStart} and {windowEnd} (Vietnam time) and upload the fit-to-travel certificate to the dossier. The horse cannot depart on trip {horse.TripCode} until the specialist approves it.",
                NotificationReferenceTypes.Dossier,
                horse.DossierId);

            await _notificationService.EmailCustomerAsync(
                horse.CustomerUserId,
                retake ? EmailTemplateNames.FitExamRetake : EmailTemplateNames.FitExamReminder,
                CustomerPages.Booking(horse.BookingId),
                new Dictionary<string, string>
                {
                    ["BookingCode"] = horse.BookingCode,
                    ["HorseName"] = horse.HorseName,
                    ["TripCode"] = horse.TripCode,
                    ["ExamWindowStart"] = windowStart,
                    ["ExamWindowEnd"] = windowEnd
                });
        }

        private static string ReminderTitle(string horseName) => $"{ReminderTitlePrefix}: {horseName}";

        // Ngựa còn trên chuyến đã duyệt (Scheduled), có hồ sơ, kèm các giấy khám chưa bị từ chối trong hồ sơ của đơn
        private static async Task<List<HorseOnTrip>> LoadHorsesAsync(IQueryable<TripHorse> query)
        {
            var rows = await query
                .AsNoTracking()
                .Where(th => th.Status == TripHorseStatuses.Planned
                    && th.Trip!.OverallStatus == TripStatuses.Scheduled
                    && th.BookingHorse!.Dossier != null)
                .Select(th => new
                {
                    th.Trip!.TripCode,
                    th.Trip.PlannedStartDate,
                    th.Trip.ApprovedAt,
                    th.BookingHorse!.BookingId,
                    th.BookingHorse.Booking!.BookingCode,
                    th.BookingHorse.Booking.CustomerUserId,
                    HorseName = th.BookingHorse.Horse!.Name,
                    th.BookingHorse.Dossier!.DossierId,
                    FitExams = th.BookingHorse.Dossier.Documents
                        .Where(d => d.DocumentType!.Code == DocumentTypeCodes.FitExam && d.Status != DocumentStatuses.Rejected)
                        .Select(d => new { d.Status, d.ExpiryDate, d.UploadedAt })
                        .ToList()
                })
                .ToListAsync();

            return rows.Select(r => new HorseOnTrip
            {
                TripCode = r.TripCode,
                PlannedStartDate = r.PlannedStartDate,
                TripApprovedAt = r.ApprovedAt,
                BookingId = r.BookingId,
                BookingCode = r.BookingCode,
                CustomerUserId = r.CustomerUserId,
                HorseName = r.HorseName,
                DossierId = r.DossierId,
                FitExams = r.FitExams.Select(f => (f.Status, FitExamRules.IssueDateOf(f.ExpiryDate, f.UploadedAt))).ToList()
            }).ToList();
        }

        private class HorseOnTrip
        {
            public string TripCode { get; set; } = string.Empty;
            public DateTimeOffset PlannedStartDate { get; set; }
            public DateTimeOffset? TripApprovedAt { get; set; }
            public int BookingId { get; set; }
            public string BookingCode { get; set; } = string.Empty;
            public int CustomerUserId { get; set; }
            public string HorseName { get; set; } = string.Empty;
            public int DossierId { get; set; }
            public List<(string Status, DateOnly IssueDate)> FitExams { get; set; } = new();
        }
    }
}
