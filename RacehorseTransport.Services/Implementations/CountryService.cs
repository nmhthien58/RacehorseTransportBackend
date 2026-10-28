using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Helpers;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    // R-18: phạm vi dịch vụ theo quốc gia.
    // Không lưu cờ "có đường bộ / đường bay" riêng: công tắc chính là trạng thái của dòng cước FREIGHT (theo phương thức, nước đến)
    // trong bảng giá, để chỉ có một chỗ quyết định. Đường bay còn cần sân bay đang mở ở cả hai đầu.
    public class CountryService : ICountryService
    {
        private readonly IRepository<Country> _countryRepo;
        private readonly IRepository<Airport> _airportRepo;
        private readonly IRepository<PriceItem> _priceItemRepo;
        private readonly IRepository<CountryDocRequirement> _requirementRepo;
        private readonly ICacheService _cache;

        public CountryService(
            IRepository<Country> countryRepo,
            IRepository<Airport> airportRepo,
            IRepository<PriceItem> priceItemRepo,
            IRepository<CountryDocRequirement> requirementRepo,
            ICacheService cache)
        {
            _countryRepo = countryRepo;
            _airportRepo = airportRepo;
            _priceItemRepo = priceItemRepo;
            _requirementRepo = requirementRepo;
            _cache = cache;
        }

        public async Task<List<CountryCatalogModel>> GetCatalogAsync()
        {
            var countries = await _countryRepo.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Airports)
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.NameVi)
                .ToListAsync();
            var freights = await LoadFreightsAsync();

            return countries.Select(c =>
            {
                var state = ToState(c, freights);
                return new CountryCatalogModel
                {
                    CountryCode = c.CountryCode,
                    NameVi = c.NameVi,
                    NameEn = c.NameEn,
                    GroundAvailable = ServiceRouteRules.GroundAvailable(state),
                    AirAvailable = ServiceRouteRules.AirAvailable(state),
                    Airports = c.Airports.Where(a => a.IsActive).OrderBy(a => a.IataCode).Select(ToModel).ToList()
                };
            }).ToList();
        }

        public async Task<List<CountrySummaryModel>> GetCountriesAsync()
        {
            var countries = await _countryRepo.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Airports)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.NameVi)
                .ToListAsync();
            var freights = await LoadFreightsAsync();
            var requirementCounts = await _requirementRepo.GetQueryable()
                .AsNoTracking()
                .GroupBy(r => new { r.CountryCode, r.Direction })
                .Select(g => new { g.Key.CountryCode, g.Key.Direction, Count = g.Count() })
                .ToListAsync();

            return countries.Select(c =>
            {
                var summary = new CountrySummaryModel();
                FillSummary(summary, c, freights,
                    requirementCounts.Where(r => r.CountryCode == c.CountryCode).ToDictionary(r => r.Direction, r => r.Count));
                return summary;
            }).ToList();
        }

        public async Task<CountryDetailModel> GetCountryAsync(string countryCode)
        {
            string code = NormalizeCode(countryCode);
            var country = await _countryRepo.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Airports)
                .FirstOrDefaultAsync(c => c.CountryCode == code)
                ?? throw new NotFoundException($"Country '{code}' was not found.");

            var freights = await LoadFreightsAsync();
            var requirements = await _requirementRepo.GetQueryable()
                .AsNoTracking()
                .Include(r => r.DocumentType)
                .Where(r => r.CountryCode == code)
                .ToListAsync();
            var fees = await _priceItemRepo.GetQueryable()
                .AsNoTracking()
                .Where(p => p.DestCountryCode == code && p.ItemCode != FreightCode)
                .OrderBy(p => p.Category).ThenBy(p => p.ItemCode)
                .ToListAsync();

            var detail = new CountryDetailModel
            {
                Airports = country.Airports.OrderBy(a => a.IataCode).Select(ToModel).ToList(),
                Requirements = requirements
                    .OrderBy(r => Array.IndexOf(RequirementDirections.All, r.Direction))
                    .ThenBy(r => r.DocumentType!.Name)
                    .Select(r => r.ToBusinessModel())
                    .ToList(),
                CountryFees = fees.Select(p => p.ToBusinessModel()).ToList()
            };
            FillSummary(detail, country, freights,
                requirements.GroupBy(r => r.Direction).ToDictionary(g => g.Key, g => g.Count()));
            return detail;
        }

        public async Task<CountryDetailModel> CreateCountryAsync(SaveCountryModel model)
        {
            string code = NormalizeCode(model.CountryCode);
            if (await _countryRepo.ExistsAsync(c => c.CountryCode == code))
            {
                throw new BusinessException($"Country '{code}' already exists.");
            }

            await _countryRepo.AddAsync(new Country
            {
                CountryCode = code,
                NameVi = model.NameVi.Trim(),
                NameEn = model.NameEn.Trim(),
                SortOrder = model.SortOrder,
                IsActive = true
            });
            return await GetCountryAsync(code);
        }

        public async Task<CountryDetailModel> UpdateCountryAsync(string countryCode, SaveCountryModel model)
        {
            var country = await FindCountryAsync(countryCode);
            country.NameVi = model.NameVi.Trim();
            country.NameEn = model.NameEn.Trim();
            country.SortOrder = model.SortOrder;
            await _countryRepo.UpdateAsync(country);
            return await GetCountryAsync(country.CountryCode);
        }

        public async Task<CountryDetailModel> SetActiveAsync(string countryCode, bool isActive)
        {
            var country = await FindCountryAsync(countryCode);
            country.IsActive = isActive;
            await _countryRepo.UpdateAsync(country);
            return await GetCountryAsync(country.CountryCode);
        }

        // Bật / tắt và đổi giá cước của một phương thức. Tắt thì giá vẫn được giữ, chỉ là khách không chọn được.
        public async Task<CountryDetailModel> SetFreightAsync(string countryCode, string transportMode, decimal value, bool isActive)
        {
            var country = await FindCountryAsync(countryCode);
            if (!TransportModes.All.Contains(transportMode))
            {
                throw new BusinessException($"TransportMode '{transportMode}' is invalid. Allowed values: {string.Join(", ", TransportModes.All)}.");
            }
            if (value <= 0 || value > 99999999999999.9999m)
            {
                throw new BusinessException("The freight rate must be greater than zero.");
            }

            bool air = transportMode == TransportModes.Air;
            if (air && isActive && !await _airportRepo.ExistsAsync(a => a.CountryCode == country.CountryCode && a.IsActive))
            {
                throw new BusinessException($"Add at least one active airport in {country.NameEn} before turning on air transport.");
            }

            var freight = await _priceItemRepo.GetQueryable()
                .FirstOrDefaultAsync(p => p.ItemCode == FreightCode && p.TransportMode == transportMode && p.DestCountryCode == country.CountryCode);

            if (freight == null)
            {
                await _priceItemRepo.AddAsync(new PriceItem
                {
                    Category = PriceCategories.Freight,
                    ItemCode = FreightCode,
                    ItemName = air ? $"Cước đường bay ({country.NameVi})" : $"Cước đường bộ ({country.NameVi})",
                    TransportMode = transportMode,
                    DestCountryCode = country.CountryCode,
                    // Đường bay tính theo con (cước theo vùng), đường bộ theo km mỗi con
                    CalcType = air ? PriceCalcTypes.PerHorse : PriceCalcTypes.PerKmPerHorse,
                    Value = value,
                    IsActive = isActive
                });
            }
            else
            {
                freight.Value = value;
                freight.IsActive = isActive;
                await _priceItemRepo.UpdateAsync(freight);
            }

            await _cache.RemoveAsync(CacheKeys.PriceItemsAll);
            return await GetCountryAsync(country.CountryCode);
        }

        public async Task<CountryDetailModel> AddAirportAsync(string countryCode, SaveAirportModel model)
        {
            var country = await FindCountryAsync(countryCode);
            string iata = NormalizeIata(model.IataCode);
            await EnsureUniqueIataAsync(iata, null);

            await _airportRepo.AddAsync(new Airport
            {
                CountryCode = country.CountryCode,
                IataCode = iata,
                Name = model.Name.Trim(),
                City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
                IsActive = true
            });
            return await GetCountryAsync(country.CountryCode);
        }

        public async Task<CountryDetailModel> UpdateAirportAsync(int airportId, SaveAirportModel model)
        {
            var airport = await FindAirportAsync(airportId);
            string iata = NormalizeIata(model.IataCode);
            await EnsureUniqueIataAsync(iata, airportId);

            airport.IataCode = iata;
            airport.Name = model.Name.Trim();
            airport.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
            await _airportRepo.UpdateAsync(airport);
            return await GetCountryAsync(airport.CountryCode);
        }

        public async Task<CountryDetailModel> SetAirportActiveAsync(int airportId, bool isActive)
        {
            var airport = await FindAirportAsync(airportId);
            airport.IsActive = isActive;
            await _airportRepo.UpdateAsync(airport);
            return await GetCountryAsync(airport.CountryCode);
        }

        public async Task EnsureRouteAvailableAsync(string originCountryCode, string destinationCountryCode, string transportMode)
        {
            string origin = NormalizeCode(originCountryCode);
            string destination = NormalizeCode(destinationCountryCode);
            var countries = await _countryRepo.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Airports)
                .Where(c => c.CountryCode == origin || c.CountryCode == destination)
                .ToListAsync();
            var freights = await LoadFreightsAsync();

            ServiceRouteRules.CountryState? State(string code)
            {
                var country = countries.FirstOrDefault(c => c.CountryCode == code);
                return country == null ? null : ToState(country, freights);
            }

            string? problem = ServiceRouteRules.Problem(transportMode, origin, State(origin), destination, State(destination));
            if (problem != null)
            {
                throw new BusinessException(problem);
            }
        }

        // ---------------------------------------------------------------------------------------------

        private const string FreightCode = "FREIGHT";

        private async Task<List<PriceItem>> LoadFreightsAsync()
        {
            return await _priceItemRepo.GetQueryable()
                .AsNoTracking()
                .Where(p => p.ItemCode == FreightCode && p.DestCountryCode != null)
                .ToListAsync();
        }

        private static PriceItem? FindFreight(List<PriceItem> freights, string countryCode, string mode) =>
            freights.FirstOrDefault(p => p.DestCountryCode == countryCode && p.TransportMode == mode);

        private static ServiceRouteRules.CountryState ToState(Country country, List<PriceItem> freights) =>
            new(country.CountryCode,
                country.IsActive,
                FindFreight(freights, country.CountryCode, TransportModes.Ground)?.IsActive == true,
                FindFreight(freights, country.CountryCode, TransportModes.Air)?.IsActive == true,
                country.Airports.Count(a => a.IsActive));

        private static void FillSummary(
            CountrySummaryModel summary, Country country, List<PriceItem> freights, Dictionary<string, int> requirementCounts)
        {
            var state = ToState(country, freights);
            summary.CountryCode = country.CountryCode;
            summary.NameVi = country.NameVi;
            summary.NameEn = country.NameEn;
            summary.IsActive = country.IsActive;
            summary.SortOrder = country.SortOrder;
            summary.GroundFreight = ToFreightModel(FindFreight(freights, country.CountryCode, TransportModes.Ground));
            summary.AirFreight = ToFreightModel(FindFreight(freights, country.CountryCode, TransportModes.Air));
            summary.GroundAvailable = ServiceRouteRules.GroundAvailable(state);
            summary.AirAvailable = ServiceRouteRules.AirAvailable(state);
            summary.AirportCount = country.Airports.Count;
            summary.ActiveAirportCount = state.ActiveAirports;
            summary.ExportDocumentCount = requirementCounts.GetValueOrDefault(RequirementDirections.Export);
            summary.ImportDocumentCount = requirementCounts.GetValueOrDefault(RequirementDirections.Import);
            summary.TransitDocumentCount = requirementCounts.GetValueOrDefault(RequirementDirections.Transit);
            summary.Warnings = CountryWarnings.For(summary);
        }

        private static CountryFreightModel? ToFreightModel(PriceItem? item) =>
            item == null ? null : new CountryFreightModel
            {
                PriceItemId = item.PriceItemId,
                Value = item.Value,
                CalcType = item.CalcType,
                IsActive = item.IsActive
            };

        private static AirportModel ToModel(Airport airport) => new()
        {
            AirportId = airport.AirportId,
            CountryCode = airport.CountryCode,
            IataCode = airport.IataCode,
            Name = airport.Name,
            City = airport.City,
            IsActive = airport.IsActive
        };

        private async Task<Country> FindCountryAsync(string countryCode)
        {
            string code = NormalizeCode(countryCode);
            return await _countryRepo.GetQueryable().FirstOrDefaultAsync(c => c.CountryCode == code)
                ?? throw new NotFoundException($"Country '{code}' was not found.");
        }

        private async Task<Airport> FindAirportAsync(int airportId)
        {
            return await _airportRepo.GetQueryable().FirstOrDefaultAsync(a => a.AirportId == airportId)
                ?? throw new NotFoundException($"Airport with ID {airportId} was not found.");
        }

        private async Task EnsureUniqueIataAsync(string iata, int? excludedId)
        {
            if (await _airportRepo.ExistsAsync(a => a.IataCode == iata && (!excludedId.HasValue || a.AirportId != excludedId.Value)))
            {
                throw new BusinessException($"Airport code '{iata}' already exists.");
            }
        }

        private static string NormalizeCode(string? code)
        {
            string value = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (value.Length != 2 || value.Any(ch => ch < 'A' || ch > 'Z'))
            {
                throw new BusinessException("Country code must be exactly two letters (ISO 3166-1 alpha-2), for example VN.");
            }
            return value;
        }

        private static string NormalizeIata(string? code)
        {
            string value = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (value.Length != 3 || value.Any(ch => ch < 'A' || ch > 'Z'))
            {
                throw new BusinessException("Airport code must be exactly three letters (IATA), for example SGN.");
            }
            return value;
        }
    }

    // Cảnh báo cho trang cấu hình của Manager: nước đang bật nhưng còn thiếu thông tin để chạy được tuyến
    public static class CountryWarnings
    {
        public const string MissingImportDocuments = "MissingImportDocuments";   // nhận làm nước đến mà chưa khai giấy nhập
        public const string MissingExportDocuments = "MissingExportDocuments";   // làm nước đi được mà chưa khai giấy xuất
        public const string AirFreightWithoutAirport = "AirFreightWithoutAirport"; // có cước bay đang bật nhưng không còn sân bay mở

        public static List<string> For(CountrySummaryModel country)
        {
            var warnings = new List<string>();
            if (!country.IsActive)
            {
                return warnings;
            }
            if ((country.GroundAvailable || country.AirAvailable) && country.ImportDocumentCount == 0)
            {
                warnings.Add(MissingImportDocuments);
            }
            if (country.ExportDocumentCount == 0)
            {
                warnings.Add(MissingExportDocuments);
            }
            if (country.AirFreight?.IsActive == true && country.ActiveAirportCount == 0)
            {
                warnings.Add(AirFreightWithoutAirport);
            }
            return warnings;
        }
    }
}
