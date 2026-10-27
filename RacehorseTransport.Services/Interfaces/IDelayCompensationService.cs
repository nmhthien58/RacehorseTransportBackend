using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-09: bồi thường giao trễ. Hệ thống tạo khi đóng chuyến (TripCompletionService); Manager duyệt hoặc từ chối.
    public interface IDelayCompensationService
    {
        // Manager: danh sách, lọc theo trạng thái (mặc định tất cả), mới nhất trước
        Task<IReadOnlyList<DelayCompensationModel>> GetAllAsync(string? status, CurrentUser currentUser);

        // Khách / nhân sự xem được đơn: các khoản bồi thường trễ của đơn
        Task<IReadOnlyList<DelayCompensationModel>> GetForBookingAsync(int bookingId, CurrentUser currentUser);

        // Duyệt: số tiền khác số tính thì bắt buộc lý do; tiền được hoàn qua VNPay (hoặc chờ hoàn tay)
        Task<DelayCompensationModel> ApproveAsync(int id, decimal amount, string? reason, CurrentUser currentUser);

        Task<DelayCompensationModel> RejectAsync(int id, string reason, CurrentUser currentUser);
    }
}
