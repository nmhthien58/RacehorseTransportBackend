namespace RacehorseTransport.Services.Interfaces
{
    // Phần GHI thông báo (chuông trong ứng dụng). Các service nghiệp vụ gọi tới đây mỗi khi có sự kiện.
    // Phần đọc / đánh dấu đã đọc cho người dùng là task B-05.
    public interface INotificationService
    {
        Task SendAsync(int recipientUserId, string title, string message, string? referenceType = null, int? referenceId = null);

        Task SendToUsersAsync(IEnumerable<int> recipientUserIds, string title, string message, string? referenceType = null, int? referenceId = null);

        // Gửi cho mọi tài khoản đang hoạt động thuộc role (ví dụ báo đơn mới cho tất cả LogisticsManager)
        Task SendToRoleAsync(string role, string title, string message, string? referenceType = null, int? referenceId = null);
    }
}
