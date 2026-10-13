using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface INotificationReadService
    {
        Task<PagedBusinessResult<NotificationModel>> GetNotificationsAsync(
            int currentUserId,
            NotificationQueryOptions options);

        Task<int> GetUnreadCountAsync(int currentUserId);
        Task<NotificationModel> MarkAsReadAsync(long notificationId, int currentUserId);
        Task MarkAllAsReadAsync(int currentUserId);
    }
}
