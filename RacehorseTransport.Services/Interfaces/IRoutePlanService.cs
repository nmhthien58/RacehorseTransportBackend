using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-10: tuyến dự kiến của đơn. Coordinator lập (Draft) -> trình (Submitted) -> Manager duyệt (Approved) / từ chối (Rejected).
    public interface IRoutePlanService
    {
        // Nhân sự: mọi phiên bản; khách: chỉ tuyến đã duyệt
        Task<IReadOnlyList<RoutePlanModel>> GetForBookingAsync(int bookingId, CurrentUser currentUser);
        Task<RoutePlanModel> GetByIdAsync(int routePlanId, CurrentUser currentUser);

        // Tính tuyến (từng chặng, nước quá cảnh, cảnh báo cửa khẩu) mà không lưu: dùng khi Coordinator đang kéo / thêm mốc
        Task<RoutePlanModel> PreviewAsync(int bookingId, SaveRoutePlanModel model, CurrentUser currentUser);

        Task<RoutePlanModel> CreateAsync(int bookingId, SaveRoutePlanModel model, CurrentUser currentUser);
        Task<RoutePlanModel> UpdateAsync(int routePlanId, SaveRoutePlanModel model, CurrentUser currentUser);
        Task<RoutePlanModel> SubmitAsync(int routePlanId, CurrentUser currentUser);
        Task<RoutePlanModel> ApproveAsync(int routePlanId, CurrentUser currentUser);
        Task<RoutePlanModel> RejectAsync(int routePlanId, string reason, CurrentUser currentUser);
    }
}
