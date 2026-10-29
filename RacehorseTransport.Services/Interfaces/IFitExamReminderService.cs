namespace RacehorseTransport.Services.Interfaces
{
    // Nhắc khách cho ngựa khám lâm sàng (FIT_EXAM) trong N giờ trước giờ xuất phát và tải giấy khám lên hồ sơ
    public interface IFitExamReminderService
    {
        // Gọi trong transaction lúc Manager duyệt chuyến: nhắc khám, hoặc báo khám lại nếu giấy đã duyệt không còn hợp với giờ đi mới
        Task NotifyTripScheduledAsync(int tripId);

        // Job nền: nhắc lần hai khi còn vài ngày tới giờ xuất phát mà ngựa chưa nộp giấy khám. Trả về số ngựa đã nhắc.
        Task<int> ProcessRemindersAsync();
    }
}
