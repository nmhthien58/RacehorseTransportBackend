namespace RacehorseTransport.Services.Interfaces
{
    // Số ngày cần để làm xong hồ sơ kiểm dịch của một tuyến (nước đi, các nước quá cảnh, nước đến)
    public interface IClearanceScheduleService
    {
        Task<int> GetClearanceDaysAsync(string originCountry, IReadOnlyList<string> transitCountries, string destinationCountry);
    }
}
