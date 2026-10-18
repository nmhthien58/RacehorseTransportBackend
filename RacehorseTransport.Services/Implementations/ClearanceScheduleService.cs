using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Thời gian làm hồ sơ của tuyến = giấy lâu nhất trong các yêu cầu Export (nước đi) + Transit (nước quá cảnh)
    // + Import (nước đến) có ghi processing_days. Các giấy làm song song nên lấy max, không cộng dồn.
    // Tuyến chưa có số liệu thì dùng Booking:DefaultClearanceDays.
    public class ClearanceScheduleService : IClearanceScheduleService
    {
        private readonly IRepository<CountryDocRequirement> _requirementRepo;
        private readonly BookingOptions _options;

        public ClearanceScheduleService(IRepository<CountryDocRequirement> requirementRepo, IOptions<BookingOptions> options)
        {
            _requirementRepo = requirementRepo;
            _options = options.Value;
        }

        public async Task<int> GetClearanceDaysAsync(string originCountry, IReadOnlyList<string> transitCountries, string destinationCountry)
        {
            var transit = transitCountries.ToList();

            int? maxDays = await _requirementRepo.GetQueryable()
                .AsNoTracking()
                .Where(r => r.ProcessingDays != null
                    && ((r.CountryCode == originCountry && r.Direction == RequirementDirections.Export)
                        || (r.CountryCode == destinationCountry && r.Direction == RequirementDirections.Import)
                        || (transit.Contains(r.CountryCode) && r.Direction == RequirementDirections.Transit)))
                .MaxAsync(r => r.ProcessingDays);

            return maxDays ?? _options.DefaultClearanceDays;
        }
    }
}
