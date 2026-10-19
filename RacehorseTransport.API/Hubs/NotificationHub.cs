using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RacehorseTransport.API.Hubs
{
    // Kênh realtime cho chuông thông báo: /hubs/notifications
    // Frontend kết nối bằng access token (?access_token=... hoặc accessTokenFactory của @microsoft/signalr).
    // Server -> client: sự kiện "notificationReceived" với payload giống một phần tử của GET /api/notifications.
    // Client không cần gọi method nào lên server; đọc / đánh dấu đã đọc vẫn qua REST.
    [Authorize]
    public class NotificationHub : Hub
    {
        public const string Path = "/hubs/notifications";
        public const string NotificationReceived = "notificationReceived";
    }
}
