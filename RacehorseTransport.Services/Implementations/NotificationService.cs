using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<SystemNotification> _notificationRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IEmailService _emailService;
        private readonly EmailOptions _emailOptions;

        public NotificationService(
            IRepository<SystemNotification> notificationRepo,
            IRepository<User> userRepo,
            IEmailService emailService,
            IOptions<EmailOptions> emailOptions)
        {
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
            _emailService = emailService;
            _emailOptions = emailOptions.Value;
        }

        public Task SendAsync(int recipientUserId, string title, string message, string? referenceType = null, int? referenceId = null, bool emailCustomers = false)
        {
            return SendToUsersAsync(new[] { recipientUserId }, title, message, referenceType, referenceId, emailCustomers);
        }

        public async Task SendToUsersAsync(IEnumerable<int> recipientUserIds, string title, string message, string? referenceType = null, int? referenceId = null, bool emailCustomers = false)
        {
            var userIds = recipientUserIds.Distinct().ToList();

            var notifications = userIds
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

            if (emailCustomers && userIds.Count > 0)
            {
                await EmailCustomersAsync(userIds, title, message);
            }
        }

        // Nhân sự chỉ nhận chuông trong ứng dụng; khách hàng nhận thêm email
        private async Task EmailCustomersAsync(List<int> userIds, string title, string message)
        {
            var customers = await _userRepo.GetQueryable()
                .AsNoTracking()
                .Where(u => userIds.Contains(u.UserId) && u.Role == UserRoles.Customer && u.IsActive)
                .Select(u => new { u.Email, u.FullName })
                .ToListAsync();

            foreach (var customer in customers)
            {
                _emailService.Queue(customer.Email, customer.FullName, EmailTemplateNames.Notification, new Dictionary<string, string>
                {
                    ["FullName"] = customer.FullName,
                    ["Title"] = title,
                    ["Message"] = message,
                    ["Link"] = _emailOptions.FrontendBaseUrl
                });
            }
        }

        public async Task EmailCustomerAsync(int customerUserId, string templateName, string linkPath, IDictionary<string, string> values)
        {
            var customer = await _userRepo.GetQueryable()
                .AsNoTracking()
                .Where(u => u.UserId == customerUserId && u.Role == UserRoles.Customer && u.IsActive)
                .Select(u => new { u.Email, u.FullName })
                .FirstOrDefaultAsync();
            if (customer == null)
            {
                return;
            }

            // Biến nào thiếu thì mẫu điền chuỗi rỗng, nên mail không bao giờ lộ "{{...}}"
            var filled = new Dictionary<string, string>(values)
            {
                ["FullName"] = customer.FullName,
                ["Link"] = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/{linkPath.TrimStart('/')}"
            };
            _emailService.Queue(customer.Email, customer.FullName, templateName, filled);
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
