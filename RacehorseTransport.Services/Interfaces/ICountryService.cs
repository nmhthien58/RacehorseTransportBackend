using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // R-18: phạm vi dịch vụ theo quốc gia (nước, phương thức, sân bay)
    public interface ICountryService
    {
        Task<List<CountryCatalogModel>> GetCatalogAsync();
        Task<List<CountrySummaryModel>> GetCountriesAsync();
        Task<CountryDetailModel> GetCountryAsync(string countryCode);
        Task<CountryDetailModel> CreateCountryAsync(SaveCountryModel model);
        Task<CountryDetailModel> UpdateCountryAsync(string countryCode, SaveCountryModel model);
        Task<CountryDetailModel> SetActiveAsync(string countryCode, bool isActive);
        Task<CountryDetailModel> SetFreightAsync(string countryCode, string transportMode, decimal value, bool isActive);
        Task<CountryDetailModel> AddAirportAsync(string countryCode, SaveAirportModel model);
        Task<CountryDetailModel> UpdateAirportAsync(int airportId, SaveAirportModel model);
        Task<CountryDetailModel> SetAirportActiveAsync(int airportId, bool isActive);

        // Chặn đặt đơn / xem giá cho tuyến công ty chưa nhận (BusinessException kèm lý do)
        Task EnsureRouteAvailableAsync(string originCountryCode, string destinationCountryCode, string transportMode);
    }
}
