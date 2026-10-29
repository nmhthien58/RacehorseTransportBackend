using Microsoft.Extensions.Options;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.API.BackgroundJobs
{
    // Chạy nền cùng API: định kỳ chuyển các đơn Submitted quá hạn xét duyệt (mặc định 48 giờ) và các đơn đã duyệt
    // quá hạn đặt cọc sang Expired, rồi gửi nhắc nợ chi phí phát sinh quá hạn.
    // Quy tắc nằm ở BookingService.ExpireOverdueBookingsAsync; lớp này chỉ lo việc hẹn giờ.
    public class BookingExpiryJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingExpiryJob> _logger;
        private readonly TimeSpan _interval;

        public BookingExpiryJob(
            IServiceScopeFactory scopeFactory,
            IOptions<BookingOptions> options,
            ILogger<BookingExpiryJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.ExpiryCheckMinutes));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            do
            {
                try
                {
                    // Service dùng DbContext (scoped) nên mỗi lần quét phải tạo một scope riêng
                    using var scope = _scopeFactory.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                    int expired = await bookingService.ExpireOverdueBookingsAsync();
                    if (expired > 0)
                    {
                        _logger.LogInformation("Expired {Count} booking(s) that passed the review / deposit deadline.", expired);
                    }

                    // R-10: báo giá chốt sắp hết hạn -> nhắc; quá hạn chưa chấp nhận / chưa cọc -> QuoteExpired
                    var quoteService = scope.ServiceProvider.GetRequiredService<IBookingQuoteService>();
                    int quoted = await quoteService.ProcessQuoteDeadlinesAsync();
                    if (quoted > 0)
                    {
                        _logger.LogInformation("Processed {Count} quote deadline(s) (reminders or expiries).", quoted);
                    }

                    // R-10 (BE-06): hạn nộp giấy: nhắc khi còn 3 ngày / 1 ngày, quá hạn còn thiếu -> hủy đơn
                    var deadlineService = scope.ServiceProvider.GetRequiredService<IDossierDeadlineService>();
                    int docs = await deadlineService.ProcessDocumentDeadlinesAsync();
                    if (docs > 0)
                    {
                        _logger.LogInformation("Processed {Count} document deadline(s) (reminders or cancellations).", docs);
                    }

                    // Chính sách mục 5: còn vài ngày tới giờ đi mà ngựa chưa nộp giấy khám lâm sàng -> nhắc một lần
                    var fitExamService = scope.ServiceProvider.GetRequiredService<IFitExamReminderService>();
                    int fitExams = await fitExamService.ProcessRemindersAsync();
                    if (fitExams > 0)
                    {
                        _logger.LogInformation("Sent {Count} fit-to-travel exam reminder(s).", fitExams);
                    }

                    // R-16 (BE-15): bỏ bước nhắc khách xác nhận người nhận có mặt (29/10/2026):
                    // người nhận đã nhận email và tài xế chủ động gọi trước khi tới điểm giao

                    // R-16 (BE-16): không giao được, quá hạn khách chọn -> chuồng tạm
                    var deliveryService = scope.ServiceProvider.GetRequiredService<IDeliveryService>();
                    int decisions = await deliveryService.ProcessOverdueDecisionsAsync();
                    if (decisions > 0)
                    {
                        _logger.LogInformation("Applied the default decision (temporary stable) to {Count} failed delivery(ies).", decisions);
                    }

                    // R-02: nhắc nợ chi phí phát sinh quá hạn (ngày đến hạn, +3, +7)
                    var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                    int reminded = await paymentService.SendOverdueRemindersAsync();
                    if (reminded > 0)
                    {
                        _logger.LogInformation("Sent payment reminders for {Count} booking(s).", reminded);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Lỗi một lần quét (ví dụ mất kết nối DB) không được làm dừng job
                    _logger.LogWarning(ex, "Booking expiry check failed; it will run again in {Interval}.", _interval);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
