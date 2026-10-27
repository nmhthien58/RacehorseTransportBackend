using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IBookingService
    {
        Task<PagedBusinessResult<BookingSummaryModel>> GetBookingsAsync(BookingFilterOptions options, CurrentUser currentUser);
        Task<BookingDetailModel> GetBookingByIdAsync(int id, CurrentUser currentUser);

        // Flow 1
        Task<BookingDetailModel> CreateBookingAsync(CreateBookingModel model, CurrentUser currentUser);
        Task<BookingDetailModel> ApproveBookingAsync(int id, int specialistUserId, CurrentUser currentUser);
        Task<BookingDetailModel> RejectBookingAsync(int id, string reason, CurrentUser currentUser);
        Task<BookingDetailModel> UpdateTransitCountriesAsync(int id, List<string> transitCountryCodes, CurrentUser currentUser);

        // BE-09: Manager chốt giá trị thỏa thuận từng con (mặc định = giá trị khai báo) trước khi gửi báo giá
        Task<BookingDetailModel> UpdateHorseValuesAsync(int id, UpdateHorseValuesModel model, CurrentUser currentUser);
        Task<BookingDetailModel> ReassignSpecialistAsync(int id, int specialistUserId, CurrentUser currentUser);
        Task<BookingDetailModel> CancelBookingAsync(int id, CurrentUser currentUser);

        // Chuyển các đơn Submitted quá hạn xét duyệt sang Expired; trả về số đơn đã chuyển. BookingExpiryJob gọi định kỳ.
        Task<int> ExpireOverdueBookingsAsync();
    }
}
