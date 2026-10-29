using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-10: báo giá chốt (phát hành qua IFinalQuoteIssuer) -> khách chấp nhận (đồng ý chính sách) và đặt cọc trong hạn, hoặc từ chối
    public interface IBookingQuoteService
    {
        // R-17: chấp nhận cùng lúc báo giá + bản hợp đồng nháp khách đang xem (contractId + mã băm), lưu bằng chứng giao kết
        Task<BookingDetailModel> AcceptQuoteAsync(int bookingId, ContractAcceptanceModel acceptance, CurrentUser currentUser);
        Task<BookingDetailModel> DeclineQuoteAsync(int bookingId, string? reason, CurrentUser currentUser);

        // Job định kỳ: nhắc khi còn 1 ngày, quá hạn (chưa chấp nhận hoặc chưa cọc) -> QuoteExpired. Trả về số đơn đã xử lý.
        Task<int> ProcessQuoteDeadlinesAsync();
    }
}
