using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-10: phát hành báo giá chốt (đơn -> Quoted, hợp đồng nháp, báo khách + email QuoteReady).
    // Tách khỏi IBookingQuoteService vì BookingQuoteService phụ thuộc IBookingService: BookingService gọi được mà không vòng DI.
    public interface IFinalQuoteIssuer
    {
        // Gọi trong transaction. Đường bộ: theo tuyến đã duyệt (routePlanId bắt buộc).
        // Đường bay: không lập tuyến trên bản đồ, routePlanId = null, giá tính theo con theo nước đến.
        Task IssueAsync(int bookingId, int? routePlanId);

        // Xem trước báo giá chốt theo một tuyến (chưa duyệt), không lưu
        Task<QuoteModel> PreviewAsync(int routePlanId);
    }
}
