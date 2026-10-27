using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-17: hợp đồng vận chuyển. Không có bước ký riêng: khách xem PDF rồi bấm "Chấp nhận" là đồng ý giao kết.
    public interface IContractService
    {
        // Sinh bản nháp theo báo giá chốt hiện tại của đơn (gọi từ BookingQuoteService khi đơn -> Quoted); bản nháp cũ -> Superseded
        Task<BookingContractModel> CreateDraftAsync(int bookingId, string reason);

        // Bản mới khi đổi điều khoản sau khi khách đã chấp nhận (BE-06 đổi điểm đón / trả, BE-14b đi đường vòng, BE-15 đổi người nhận)
        Task<BookingContractModel> CreateAmendmentAsync(int bookingId, string reason);

        // Báo giá bị rút lại (khách đổi điểm đón / trả trước khi chấp nhận): bản nháp -> Superseded
        Task DiscardDraftsAsync(int bookingId);

        Task<BookingContractModel> GetCurrentAsync(int bookingId, CurrentUser currentUser);
        Task<List<BookingContractModel>> GetHistoryAsync(int bookingId, CurrentUser currentUser);
        Task<SignedFileUrlModel> GetFileUrlAsync(int contractId, CurrentUser currentUser);

        // Chấp nhận bản nháp đầu cùng báo giá (gọi trong transaction của accept-quote)
        Task AcceptInitialAsync(int bookingId, ContractAcceptanceModel acceptance, CurrentUser currentUser);

        // Chấp nhận bản sửa đổi (POST /api/contracts/{id}/accept)
        Task<BookingContractModel> AcceptAmendmentAsync(ContractAcceptanceModel acceptance, CurrentUser currentUser);
    }
}
