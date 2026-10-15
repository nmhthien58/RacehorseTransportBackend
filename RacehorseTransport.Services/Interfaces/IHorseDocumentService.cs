using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Hồ sơ thú y lưu theo ngựa: chủ ngựa thêm / xóa, chủ ngựa và nhân sự xem
    public interface IHorseDocumentService
    {
        Task<List<HorseDocumentModel>> GetDocumentsAsync(int horseId, CurrentUser currentUser);
        Task<HorseDocumentModel> AddDocumentAsync(int horseId, CreateHorseDocumentModel model, CurrentUser currentUser);
        Task DeleteDocumentAsync(int horseId, int documentId, CurrentUser currentUser);
    }
}
