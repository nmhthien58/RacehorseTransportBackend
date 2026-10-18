using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Flow 4: đoàn xe cập nhật hành trình thực tế (xuất phát, check-in từng mốc, nhật ký thể trạng ngựa)
    public interface ITripExecutionService
    {
        Task<TripDetailModel> StartTripAsync(int tripId, CurrentUser currentUser);

        Task<TripDetailModel> ArriveAtCheckpointAsync(int checkpointId, CurrentUser currentUser);
        Task<TripDetailModel> ClearCheckpointAsync(int checkpointId, CurrentUser currentUser);
        Task<TripDetailModel> DepartCheckpointAsync(int checkpointId, CurrentUser currentUser);

        // R-05: một con ngựa rớt lại giữa đường (sự cố / thú y); chuyến vẫn đi tiếp, ngựa quay về hàng chờ xếp chuyến
        Task<TripDetailModel> OffloadHorseAsync(int tripId, int tripHorseId, OffloadHorseModel model, CurrentUser currentUser);

        Task<WelfareLogModel> AddWelfareLogAsync(int tripId, CreateWelfareLogModel model, CurrentUser currentUser);
        Task<IReadOnlyList<WelfareLogModel>> GetWelfareLogsAsync(int tripId, int? tripHorseId, CurrentUser currentUser);
    }
}
