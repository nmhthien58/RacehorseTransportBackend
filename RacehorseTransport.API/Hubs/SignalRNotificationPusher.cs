using Microsoft.AspNetCore.SignalR;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Hubs
{
    public class SignalRNotificationPusher : INotificationPusher
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<SignalRNotificationPusher> _logger;

        public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationPusher> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task PushAsync(int recipientUserId, NotificationModel notification)
        {
            try
            {
                // Một người dùng mở nhiều tab thì tab nào cũng nhận (SignalR gom theo user id)
                await _hubContext.Clients.User(recipientUserId.ToString())
                    .SendAsync(NotificationHub.NotificationReceived, notification.ToResponse());
            }
            catch (Exception ex)
            {
                // Thông báo đã lưu trong DB; lần tải danh sách sau người dùng vẫn thấy
                _logger.LogWarning(ex, "Could not push notification {NotificationId} to user {UserId}",
                    notification.NotificationId, recipientUserId);
            }
        }
    }
}
