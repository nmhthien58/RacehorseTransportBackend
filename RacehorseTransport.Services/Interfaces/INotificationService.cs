namespace RacehorseTransport.Services.Interfaces
{
    // Phần GHI thông báo (chuông trong ứng dụng). Các service nghiệp vụ gọi tới đây mỗi khi có sự kiện.
    // Phần đọc / đánh dấu đã đọc cho người dùng là task B-05.
    public interface INotificationService
    {
        // emailCustomers = true: ngoài chuông trong ứng dụng, gửi thêm email mẫu chung (Notification) cho những người nhận là KHÁCH HÀNG.
        // Sự kiện đã có mẫu riêng (duyệt / từ chối đơn, cần bổ sung giấy tờ, thông quan, sự cố, tới điểm giao) thì để false
        // và gọi EmailCustomerAsync. Mẫu chung chỉ còn dùng cho sự kiện chưa có mẫu (giấy bị từ chối, hồ sơ có vấn đề).
        Task SendAsync(int recipientUserId, string title, string message, string? referenceType = null, int? referenceId = null, bool emailCustomers = false);

        Task SendToUsersAsync(IEnumerable<int> recipientUserIds, string title, string message, string? referenceType = null, int? referenceId = null, bool emailCustomers = false);

        // Gửi cho mọi tài khoản đang hoạt động thuộc role (ví dụ báo đơn mới cho tất cả LogisticsManager)
        Task SendToRoleAsync(string role, string title, string message, string? referenceType = null, int? referenceId = null);

        // Chỉ gửi email theo mẫu riêng (không tạo chuông) cho một khách hàng đang hoạt động; người nhận khác bị bỏ qua.
        // values: các biến của mẫu (xem EmailTemplateNames). {{FullName}} lấy từ tài khoản,
        // {{Link}} = FrontendBaseUrl + linkPath (ví dụ "/customer/bookings/12").
        Task EmailCustomerAsync(int customerUserId, string templateName, string linkPath, IDictionary<string, string> values);

        // Gửi email theo mẫu đến một địa chỉ email bất kỳ (dùng cho người nhận không phải user hệ thống, ví dụ BE-15)
        Task EmailAddressAsync(string email, string name, string templateName, IDictionary<string, string> values);
    }
}
