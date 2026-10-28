using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    // R-18: Request -> BusinessModel -> Response cho quốc gia và sân bay
    public static class CountryApiMapper
    {
        public static SaveCountryModel ToBusinessModel(this CreateCountryRequest request) => new()
        {
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            NameVi = request.NameVi.Trim(),
            NameEn = request.NameEn.Trim(),
            SortOrder = request.SortOrder
        };

        public static SaveCountryModel ToBusinessModel(this UpdateCountryRequest request, string countryCode) => new()
        {
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            NameVi = request.NameVi.Trim(),
            NameEn = request.NameEn.Trim(),
            SortOrder = request.SortOrder
        };

        public static SaveAirportModel ToBusinessModel(this SaveAirportRequest request) => new()
        {
            IataCode = request.IataCode.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim()
        };

        public static AirportResponse ToResponse(this AirportModel model) => new()
        {
            AirportId = model.AirportId,
            CountryCode = model.CountryCode,
            IataCode = model.IataCode,
            Name = model.Name,
            City = model.City,
            IsActive = model.IsActive
        };

        public static CountryCatalogResponse ToResponse(this CountryCatalogModel model) => new()
        {
            CountryCode = model.CountryCode,
            NameVi = model.NameVi,
            NameEn = model.NameEn,
            GroundAvailable = model.GroundAvailable,
            AirAvailable = model.AirAvailable,
            Airports = model.Airports.Select(a => a.ToResponse()).ToList()
        };

        public static CountrySummaryResponse ToResponse(this CountrySummaryModel model)
        {
            var response = new CountrySummaryResponse();
            Fill(response, model);
            return response;
        }

        public static CountryDetailResponse ToResponse(this CountryDetailModel model)
        {
            var response = new CountryDetailResponse
            {
                Airports = model.Airports.Select(a => a.ToResponse()).ToList(),
                Requirements = model.Requirements.Select(r => r.ToResponse()).ToList(),
                CountryFees = model.CountryFees.Select(p => p.ToResponse()).ToList()
            };
            Fill(response, model);
            return response;
        }

        private static void Fill(CountrySummaryResponse response, CountrySummaryModel model)
        {
            response.CountryCode = model.CountryCode;
            response.NameVi = model.NameVi;
            response.NameEn = model.NameEn;
            response.IsActive = model.IsActive;
            response.SortOrder = model.SortOrder;
            response.GroundFreight = ToResponse(model.GroundFreight);
            response.AirFreight = ToResponse(model.AirFreight);
            response.GroundAvailable = model.GroundAvailable;
            response.AirAvailable = model.AirAvailable;
            response.AirportCount = model.AirportCount;
            response.ActiveAirportCount = model.ActiveAirportCount;
            response.ExportDocumentCount = model.ExportDocumentCount;
            response.ImportDocumentCount = model.ImportDocumentCount;
            response.TransitDocumentCount = model.TransitDocumentCount;
            response.Warnings = model.Warnings.ToList();
        }

        private static CountryFreightResponse? ToResponse(CountryFreightModel? model) =>
            model == null ? null : new CountryFreightResponse
            {
                PriceItemId = model.PriceItemId,
                Value = model.Value,
                CalcType = model.CalcType,
                IsActive = model.IsActive
            };
    }
}
