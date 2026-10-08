using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.BackgroundJobs
{
    // Chạy nền cùng API: lấy từng email trong hàng đợi ra gửi. Gửi lỗi thì ghi log và đi tiếp;
    // request đã tạo ra email đó không bị ảnh hưởng.
    public class EmailDispatchJob : BackgroundService
    {
        private readonly IEmailQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailDispatchJob> _logger;

        public EmailDispatchJob(IEmailQueue queue, IServiceScopeFactory scopeFactory, ILogger<EmailDispatchJob> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var message in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    await sender.SendAsync(message, stoppingToken);
                    _logger.LogInformation("Email '{Subject}' sent to {To}.", message.Subject, message.ToAddress);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Email '{Subject}' to {To} could not be sent.", message.Subject, message.ToAddress);
                }
            }
        }
    }
}
