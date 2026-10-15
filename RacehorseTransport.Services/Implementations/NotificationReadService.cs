using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    public class NotificationReadService : INotificationReadService
    {
        private readonly IRepository<SystemNotification> _notificationRepo;

        public NotificationReadService(IRepository<SystemNotification> notificationRepo)
        {
            _notificationRepo = notificationRepo;
        }

        public async Task<PagedBusinessResult<NotificationModel>> GetNotificationsAsync(
            int currentUserId,
            NotificationQueryOptions options)
        {
            var query = _notificationRepo.GetQueryable()
                .AsNoTracking()
                .Where(n => n.RecipientUserId == currentUserId);

            if (options.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == options.IsRead.Value);
            }

            query = query.OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.NotificationId);

            return await query.ToPagedResultAsync(options, n => n.ToBusinessModel());
        }

        public Task<int> GetUnreadCountAsync(int currentUserId)
        {
            return _notificationRepo.CountAsync(n => n.RecipientUserId == currentUserId && !n.IsRead);
        }

        public async Task<NotificationModel> MarkAsReadAsync(long notificationId, int currentUserId)
        {
            var notification = await _notificationRepo.GetQueryable()
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId
                                       && n.RecipientUserId == currentUserId);

            if (notification == null)
            {
                throw new NotFoundException($"Notification with ID {notificationId} was not found.");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _notificationRepo.UpdateAsync(notification);
            }

            return notification.ToBusinessModel();
        }

        public async Task MarkAllAsReadAsync(int currentUserId)
        {
            await _notificationRepo.GetQueryable()
                .Where(n => n.RecipientUserId == currentUserId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
        }
    }
}
