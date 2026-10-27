using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-10 (BE-06): danh sách giấy còn thiếu theo tuyến đã duyệt (nước đi + nước quá cảnh + nước đến)
    public interface IDossierChecklistService
    {
        // Ngựa (chưa thông quan) còn thiếu giấy bắt buộc; rỗng khi khách đã nộp đủ
        Task<List<MissingDocumentsModel>> GetMissingDocumentsAsync(int bookingId);
    }

    // R-10 (BE-06): hạn nộp giấy, nhắc, hủy khi quá hạn
    public interface IDossierDeadlineService
    {
        // Job định kỳ: nhắc khi còn 3 ngày và 1 ngày; quá hạn còn thiếu -> hủy đơn theo chính sách khách hủy sau cọc. Trả về số đơn đã xử lý.
        Task<int> ProcessDocumentDeadlinesAsync();
    }
}
