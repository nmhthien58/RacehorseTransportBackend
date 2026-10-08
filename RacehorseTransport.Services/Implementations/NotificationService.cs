using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<SystemNotification> _notificationRepo;
        private readonly IRepository<User> _userRepo;

        public NotificationService(IRepository<SystemNotification> notificationRepo, IRepository<User> userRepo)
        {
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
        }

        public Task SendAsync(int recipientUserId, string title, string message, string? referenceType = null, int? referenceId = null)
        {
            return SendToUsersAsync(new[] { recipientUserId }, title, message, referenceType, referenceId);
        }

        public async Task SendToUsersAsync(IEnumerable<int> recipientUserIds, string title, string message, string? referenceType = null, int? referenceId = null)
        {
            var notifications = recipientUserIds
                .Distinct()
                .Select(userId => new SystemNotification
                {
                    RecipientUserId = userId,
                    Title = title,
                    Message = message,
                    ReferenceType = referenceType,
                    ReferenceId = referenceId
                })
                .ToList();

            if (notifications.Count > 0)
            {
                await _notificationRepo.AddRangeAsync(notifications);
            }
        }

        public async Task SendToRoleAsync(string role, string title, string message, string? referenceType = null, int? referenceId = null)
        {
            var userIds = await _userRepo.GetQueryable()
                .Where(u => u.Role == role && u.IsActive)
                .Select(u => u.UserId)
                .ToListAsync();

            await SendToUsersAsync(userIds, title, message, referenceType, referenceId);
        }
    }
}
