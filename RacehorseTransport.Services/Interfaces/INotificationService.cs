namespace RacehorseTransport.Services.Interfaces
{
    // Phần GHI thông báo (chuông trong ứng dụng). Các service nghiệp vụ gọi tới đây mỗi khi có sự kiện.
    // Phần đọc / đánh dấu đã đọc cho người dùng là task B-05.
    public interface INotificationService
    {
        // emailCustomers = true: ngoài chuông trong ứng dụng, gửi thêm email cho những người nhận là KHÁCH HÀNG đã xác thực email.
        // Chỉ bật cho các sự kiện chính (duyệt / từ chối đơn, cần bổ sung giấy tờ, thông quan, sự cố, tới điểm giao).
        Task SendAsync(int recipientUserId, string title, string message, string? referenceType = null, int? referenceId = null, bool emailCustomers = false);

        Task SendToUsersAsync(IEnumerable<int> recipientUserIds, string title, string message, string? referenceType = null, int? referenceId = null, bool emailCustomers = false);

        // Gửi cho mọi tài khoản đang hoạt động thuộc role (ví dụ báo đơn mới cho tất cả LogisticsManager)
        Task SendToRoleAsync(string role, string title, string message, string? referenceType = null, int? referenceId = null);
    }
}
