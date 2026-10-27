using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-10 (BE-06): thay đổi sau khi gửi đơn
    public interface IBookingChangeService
    {
        // Khách đổi điểm đón / trả khi chưa trả tiền: tính lại khoảng giá, đơn về PreApproved để lập tuyến + báo giá lại
        Task<BookingDetailModel> ChangeLocationsAsync(int bookingId, ChangeLocationsModel model, CurrentUser currentUser);

        // Khách hủy vì công ty đổi tuyến thêm nước mới (hoàn 100%)
        Task<BookingDetailModel> CancelForRouteChangeAsync(int bookingId, CurrentUser currentUser);
    }
}
