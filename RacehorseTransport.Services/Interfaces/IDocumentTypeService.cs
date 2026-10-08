using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IDocumentTypeService
    {
        Task<PagedBusinessResult<DocumentTypeModel>> GetDocumentTypesAsync(BusinessQueryOptions options);
        Task<IReadOnlyList<DocumentTypeModel>> GetLookupAsync();
        Task<DocumentTypeModel> GetDocumentTypeByIdAsync(int id);
        Task<DocumentTypeModel> CreateDocumentTypeAsync(DocumentTypeModel model);
        Task<DocumentTypeModel> UpdateDocumentTypeAsync(int id, DocumentTypeModel model);
        Task DeleteDocumentTypeAsync(int id);
    }
}
