using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Flow 6: bàn giao có ký nhận (e-POD) cho từng đơn trên chuyến, rồi đóng chuyến và chốt KPI
    public interface ITripCompletionService
    {
        Task<HandoverModel> CreateHandoverAsync(int tripId, CreateHandoverModel model, CurrentUser currentUser);
        Task<IReadOnlyList<HandoverModel>> GetTripHandoversAsync(int tripId, CurrentUser currentUser);
        Task<IReadOnlyList<HandoverModel>> GetBookingHandoversAsync(int bookingId, CurrentUser currentUser);

        Task<TripDetailModel> CloseTripAsync(int tripId, string? executiveRemarks, CurrentUser currentUser);
    }

    public interface IReportService
    {
        // Tính trên các chuyến đã đóng trong khoảng [from, to]; bỏ trống là không giới hạn đầu đó
        Task<ReportSummaryModel> GetSummaryAsync(DateTimeOffset? from, DateTimeOffset? to);
    }
}
