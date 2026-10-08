using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IBookingService
    {
        Task<PagedBusinessResult<BookingSummaryModel>> GetBookingsAsync(BookingFilterOptions options, CurrentUser currentUser);
        Task<BookingDetailModel> GetBookingByIdAsync(int id, CurrentUser currentUser);

        // Chuyển các đơn Submitted quá hạn xét duyệt sang Expired; trả về số đơn đã chuyển. BookingExpiryJob gọi định kỳ.
        Task<int> ExpireOverdueBookingsAsync();
    }
}
