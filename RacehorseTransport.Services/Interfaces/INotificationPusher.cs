using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Đẩy thông báo vừa tạo tới trình duyệt đang mở của người nhận (chuông cập nhật ngay, không cần tải lại).
    // Bản cài đặt dùng SignalR nằm ở tầng API (Hubs/SignalRNotificationPusher.cs).
    // Không được ném lỗi: đẩy thất bại thì người dùng vẫn thấy thông báo ở lần tải danh sách sau.
    public interface INotificationPusher
    {
        Task PushAsync(int recipientUserId, NotificationModel notification);
    }
}
