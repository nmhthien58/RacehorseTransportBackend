using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-13 (BE-12): hồ sơ bồi thường bảo hiểm
    public interface IClaimService
    {
        // Gọi từ luồng sự cố / e-POD / không giao được
        Task OpenAutomaticallyAsync(int bookingHorseId, string claimType, string description, int? incidentId = null, int? deliveryFailureId = null);

        Task<ClaimModel> OpenByCustomerAsync(int bookingId, OpenClaimModel model, CurrentUser currentUser);
        Task<ClaimModel> AddEvidenceAsync(int claimId, List<string> paths, CurrentUser currentUser);
        Task<ClaimModel> GetAsync(int claimId, CurrentUser currentUser);
        Task<List<ClaimModel>> ListAsync(string? status, int? bookingId, CurrentUser currentUser);
        Task<ClaimModel> StartReviewAsync(int claimId, CurrentUser currentUser);
        Task<ClaimModel> ApproveAsync(int claimId, decimal approvedAmount, string? faultParty, string? reason, CurrentUser currentUser);
        Task<ClaimModel> RejectAsync(int claimId, string reason, CurrentUser currentUser);
        Task<ClaimModel> PayAsync(int claimId, string? manualReference, CurrentUser currentUser);
    }
}
