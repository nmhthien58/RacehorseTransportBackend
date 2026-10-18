using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface ITrackingService
    {
        // Bảng điều khiển: tiến độ thực tế so với kế hoạch của các chuyến (mặc định: các chuyến chưa đóng)
        Task<IReadOnlyList<TripProgressModel>> GetTripProgressAsync(string? status);

        // Khách theo dõi đơn của mình: dòng thời gian các mốc và nhật ký thể trạng của ngựa trong đơn
        Task<BookingTrackingModel> GetBookingTrackingAsync(int bookingId, CurrentUser currentUser);

        // Gọi sau mỗi lần cập nhật hành trình để màn hình theo dõi không hiện dữ liệu cũ
        Task InvalidateTripAsync(int tripId);

        // Sau mỗi lần check-in: báo khách (một lần mỗi đơn) khi giờ giao dự kiến vượt hạn giao (R-08)
        Task NotifyLateDeliveriesAsync(int tripId);
    }
}
