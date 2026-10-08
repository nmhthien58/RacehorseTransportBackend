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
    }
}
