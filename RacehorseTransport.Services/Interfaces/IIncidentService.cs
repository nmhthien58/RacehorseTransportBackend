using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Flow 5: báo sự cố trên đường, đề xuất và duyệt phương án, nắn tuyến, đóng sự cố
    public interface IIncidentService
    {
        Task<PagedBusinessResult<IncidentModel>> GetIncidentsAsync(IncidentFilterOptions options, CurrentUser currentUser);
        Task<IReadOnlyList<IncidentModel>> GetTripIncidentsAsync(int tripId, CurrentUser currentUser);
        Task<IncidentModel> GetIncidentByIdAsync(int id, CurrentUser currentUser);

        Task<IncidentModel> ReportIncidentAsync(int tripId, ReportIncidentModel model, CurrentUser currentUser);

        Task<IncidentModel> ProposePlanAsync(int incidentId, ProposeIncidentPlanModel model, CurrentUser currentUser);
        Task<IncidentModel> ApprovePlanAsync(int incidentId, CurrentUser currentUser);
        Task<IncidentModel> RejectPlanAsync(int incidentId, string reason, CurrentUser currentUser);

        Task<TripDetailModel> ApplyRerouteAsync(int incidentId, ApplyRerouteModel model, CurrentUser currentUser);
        // R-09: đóng sự cố bắt buộc chọn nguyên nhân (FaultParties)
        // R-14 (BE-14b): khách có ngựa trên xe đồng ý / từ chối đi đường vòng tốn thêm tiền
        Task<IncidentModel> RecordCustomerConsentAsync(int incidentId, bool accept, CurrentUser currentUser);

        Task<IncidentModel> EmergencyAuthorizeAsync(int incidentId, string note, CurrentUser currentUser);

        Task<TripHorseModel> RecordPostOffloadDeathAsync(int tripId, int tripHorseId, PostOffloadDeathModel model, CurrentUser currentUser);

        Task<TripHorseModel> RecordVetExpenseAsync(int tripId, int tripHorseId, RecordVetExpenseModel model, CurrentUser currentUser);

        // Trả về mã đơn nối tiếp vừa tạo
        Task<int> CreateContinuationBookingAsync(int tripId, int tripHorseId, CreateContinuationBookingModel model, CurrentUser currentUser);

        Task<IncidentModel> ResolveIncidentAsync(int incidentId, string faultParty, string? faultNote, CurrentUser currentUser);

        Task<IncidentDocumentModel> UploadDocumentAsync(int incidentId, UploadIncidentDocumentModel model, CurrentUser currentUser);

        Task DeleteDocumentAsync(int incidentId, int documentId, CurrentUser currentUser);
    }
}
