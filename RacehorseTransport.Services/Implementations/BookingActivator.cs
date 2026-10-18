using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // R-02: đơn chỉ bắt đầu làm hồ sơ / xếp chuyến sau khi khách đã cọc. Lớp này làm bước "kích hoạt" đó:
    // đơn -> Approved, từng con ngựa -> Approved và được mở một bộ hồ sơ kiểm dịch giao cho Specialist phụ trách.
    // Gọi từ PaymentService khi khoản cọc thành công, và từ BookingService khi % cọc = 0.
    // Không tự mở transaction: người gọi bọc trong transaction của mình. Cần nạp sẵn booking.BookingHorses.
    public class BookingActivator : IBookingActivator
    {
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<DigitalDossier> _dossierRepo;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;

        public BookingActivator(
            IRepository<BookingRequest> bookingRepo,
            IRepository<DigitalDossier> dossierRepo,
            ICodeGenerator codeGenerator,
            INotificationService notificationService)
        {
            _bookingRepo = bookingRepo;
            _dossierRepo = dossierRepo;
            _codeGenerator = codeGenerator;
            _notificationService = notificationService;
        }

        public async Task ActivateAsync(BookingRequest booking)
        {
            if (booking.Status != BookingStatuses.AwaitingDeposit && booking.Status != BookingStatuses.Submitted)
            {
                return; // đã kích hoạt rồi (ví dụ IPN và ReturnUrl cùng tới)
            }
            if (!booking.AssignedSpecialistId.HasValue)
            {
                throw new InvalidOperationException($"Booking {booking.BookingCode} has no specialist assigned.");
            }

            int specialistId = booking.AssignedSpecialistId.Value;
            booking.Status = BookingStatuses.Approved;

            var dossiers = new List<DigitalDossier>();
            foreach (var bookingHorse in booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled))
            {
                bookingHorse.Status = BookingHorseStatuses.Approved;
                dossiers.Add(new DigitalDossier
                {
                    DossierCode = await _codeGenerator.NextAsync(CodePrefixes.Dossier),
                    BookingHorseId = bookingHorse.BookingHorseId,
                    SpecialistUserId = specialistId,
                    Status = DossierStatuses.Draft
                });
            }

            await _bookingRepo.UpdateAsync(booking);
            await _dossierRepo.AddRangeAsync(dossiers);

            await _notificationService.SendAsync(
                booking.CustomerUserId,
                $"Booking {booking.BookingCode} is confirmed",
                "Your booking is confirmed. The quarantine specialist will now prepare the clearance dossiers; please upload the requested documents.",
                NotificationReferenceTypes.Booking,
                booking.BookingId);

            await _notificationService.SendAsync(
                specialistId,
                $"Booking {booking.BookingCode} assigned to you",
                $"You are responsible for the clearance dossiers of {dossiers.Count} horse(s) in this booking.",
                NotificationReferenceTypes.Booking,
                booking.BookingId);
        }
    }
}
