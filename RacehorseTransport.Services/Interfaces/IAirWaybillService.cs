using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-17: vận đơn hàng không của chuyến
    public interface IAirWaybillService
    {
        Task<AirWaybillModel> GetAsync(int tripId, CurrentUser currentUser);
        Task<AirWaybillModel> UpdateAsync(int tripId, string number, string? filePath);
    }
}
