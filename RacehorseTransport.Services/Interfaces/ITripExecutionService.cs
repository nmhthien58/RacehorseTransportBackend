using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Flow 4: đoàn xe cập nhật hành trình thực tế (xuất phát, check-in từng mốc, nhật ký thể trạng ngựa)
    public interface ITripExecutionService
    {
        Task<TripDetailModel> StartTripAsync(int tripId, CurrentUser currentUser);

        // BE-10: cùng danh sách kiểm tra mà StartTripAsync dùng để chặn xuất phát
        Task<DepartureCheckModel> GetDepartureCheckAsync(int tripId, CurrentUser currentUser);

        Task<TripDetailModel> ArriveAtCheckpointAsync(int checkpointId, CurrentUser currentUser);
        Task<TripDetailModel> ClearCheckpointAsync(int checkpointId, CurrentUser currentUser);
        Task<TripDetailModel> DepartCheckpointAsync(int checkpointId, CurrentUser currentUser);

        Task<WelfareLogModel> AddWelfareLogAsync(int tripId, CreateWelfareLogModel model, CurrentUser currentUser);
        Task<IReadOnlyList<WelfareLogModel>> GetWelfareLogsAsync(int tripId, int? tripHorseId, CurrentUser currentUser);
    }
}
