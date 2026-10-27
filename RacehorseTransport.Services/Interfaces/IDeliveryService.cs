using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-16 (BE-16): không giao được, phí chờ, khách chọn cách xử lý
    public interface IDeliveryService
    {
        Task<DeliveryFailureModel> RecordFailureAsync(int tripId, int bookingId, RecordDeliveryFailureModel model, CurrentUser currentUser);
        Task<DeliveryFailureModel> DecideAsync(int bookingId, DeliveryDecisionModel model, CurrentUser currentUser);
        Task<List<DeliveryFailureModel>> GetForBookingAsync(int bookingId, CurrentUser currentUser);

        // Job: quá hạn chọn -> chuồng tạm. Trả về số bản ghi đã xử lý.
        Task<int> ProcessOverdueDecisionsAsync();

        // Đóng chuyến: phí chờ (e-POD + không giao được) và chi phí lựa chọn của khách, theo BookingId
        Task<Dictionary<int, decimal>> GetChargesForTripAsync(int tripId, DateTimeOffset closedAt);
    }
}
