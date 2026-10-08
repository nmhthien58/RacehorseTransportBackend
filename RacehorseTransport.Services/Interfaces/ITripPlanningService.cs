using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Dữ liệu Coordinator cần khi lập kế hoạch chuyến (Flow 3)
    public interface ITripPlanningService
    {
        // Hàng chờ xếp chuyến: ngựa của các đơn đã duyệt mà chưa nằm trên chuyến nào còn hiệu lực
        Task<IReadOnlyList<UnplannedHorseModel>> GetUnplannedHorsesAsync(string? transportMode, int? bookingId);

        // Phương tiện / nhân sự không vướng chuyến nào trong khoảng thời gian [from, to]
        Task<IReadOnlyList<AvailableVehicleModel>> GetAvailableVehiclesAsync(DateTimeOffset from, DateTimeOffset to, string? transportMode);
        Task<IReadOnlyList<UserModel>> GetAvailableCrewAsync(DateTimeOffset from, DateTimeOffset to);

        // Bản nháp kế hoạch chuyến
        Task<TripDetailModel> CreateTripAsync(TripPlanModel plan, CurrentUser currentUser);
        Task<TripDetailModel> UpdateTripAsync(int tripId, TripPlanModel plan, CurrentUser currentUser);

        // Mốc lộ trình của bản nháp
        Task<TripDetailModel> AddCheckpointAsync(int tripId, CheckpointInputModel checkpoint, CurrentUser currentUser);
        Task<TripDetailModel> UpdateCheckpointAsync(int tripId, int checkpointId, CheckpointInputModel checkpoint, CurrentUser currentUser);
        Task<TripDetailModel> DeleteCheckpointAsync(int tripId, int checkpointId, CurrentUser currentUser);
        Task<TripDetailModel> ReorderCheckpointsAsync(int tripId, List<int> checkpointIds, CurrentUser currentUser);

        // Trình duyệt: Draft -> PendingApproval -> Scheduled (hoặc bị trả về Draft; bị trả 3 lần thì Cancelled)
        Task<TripDetailModel> SubmitTripAsync(int tripId, CurrentUser currentUser);
        Task<TripDetailModel> ApproveTripAsync(int tripId, CurrentUser currentUser);
        Task<TripDetailModel> RejectTripAsync(int tripId, string reason, CurrentUser currentUser);
        Task<TripDetailModel> CancelTripAsync(int tripId, CurrentUser currentUser);
    }
}
