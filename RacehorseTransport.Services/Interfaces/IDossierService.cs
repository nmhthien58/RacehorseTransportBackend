using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IDossierService
    {
        Task<PagedBusinessResult<DossierSummaryModel>> GetDossiersAsync(DossierFilterOptions options, CurrentUser currentUser);
        Task<DossierDetailModel> GetDossierByIdAsync(int id, CurrentUser currentUser);

        // Flow 2: yêu cầu giấy tờ -> khách nộp -> thẩm định từng giấy -> nộp cơ quan chức năng -> thông quan
        Task<DossierDetailModel> RequestDocumentsAsync(int dossierId, string? note, CurrentUser currentUser);
        Task<DossierDetailModel> UploadDocumentAsync(int dossierId, UploadDocumentModel model, CurrentUser currentUser);
        Task<DossierDetailModel> ApproveDocumentAsync(int documentId, CurrentUser currentUser);
        Task<DossierDetailModel> RejectDocumentAsync(int documentId, string correctionNote, CurrentUser currentUser);
        Task<DossierDetailModel> SubmitToAuthoritiesAsync(int dossierId, CurrentUser currentUser);
        Task<DossierDetailModel> ClearDossierAsync(int dossierId, string clearanceNumber, CurrentUser currentUser);
        Task<DossierDetailModel> MarkIssueAsync(int dossierId, string reason, CurrentUser currentUser);
    }
}
