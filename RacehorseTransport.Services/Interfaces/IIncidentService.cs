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
        Task<IncidentModel> ResolveIncidentAsync(int incidentId, CurrentUser currentUser);
    }
}
