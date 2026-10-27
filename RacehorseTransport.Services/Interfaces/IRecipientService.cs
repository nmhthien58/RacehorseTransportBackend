using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-16 (BE-15): người nhận ngựa
    public interface IRecipientService
    {
        // Điền người nhận vào đơn (dùng khi tạo đơn và khi khách sửa); chưa lưu
        Task ApplyRecipientAsync(BookingRequest booking, RecipientInputModel input);

        Task UpdateRecipientAsync(int bookingId, RecipientInputModel input, CurrentUser currentUser);
        Task ConfirmRecipientAsync(int bookingId, CurrentUser currentUser);
        Task ApproveAlternateRecipientAsync(int bookingId, string name, string? idNumber, CurrentUser currentUser);
        Task<List<TripRecipientModel>> GetTripRecipientsAsync(int tripId, CurrentUser currentUser);

        // Gọi từ luồng chuyến
        Task OnTripApprovedAsync(int tripId);
        Task OnTripStartedAsync(int tripId);
        Task OnCheckpointDepartedAsync(int tripId, int checkpointId);
        Task EnsureHandoverSignerAsync(int bookingId, string signerName);
        Task OnHandoverSignedAsync(int bookingId, string recipientName, DateTimeOffset signedAt, string condition);

        // Job: nhắc khách xác nhận người nhận có mặt 24 giờ trước ETA. Trả về số lần nhắc.
        Task<int> ProcessConfirmationRemindersAsync();
    }
}
