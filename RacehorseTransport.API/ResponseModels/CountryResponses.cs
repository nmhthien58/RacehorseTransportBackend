namespace RacehorseTransport.API.ResponseModels
{
    // R-18: phạm vi dịch vụ theo quốc gia

    public class AirportResponse
    {
        public int AirportId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string IataCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public bool IsActive { get; set; }
    }

    public class CountryCatalogResponse
    {
        public string CountryCode { get; set; } = string.Empty;
        public string NameVi { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool GroundAvailable { get; set; }
        public bool AirAvailable { get; set; }
        public List<AirportResponse> Airports { get; set; } = new();
    }

    public class CountryFreightResponse
    {
        public int PriceItemId { get; set; }
        public decimal Value { get; set; }
        public string CalcType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CountrySummaryResponse
    {
        public string CountryCode { get; set; } = string.Empty;
        public string NameVi { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public CountryFreightResponse? GroundFreight { get; set; }
        public CountryFreightResponse? AirFreight { get; set; }
        public bool GroundAvailable { get; set; }
        public bool AirAvailable { get; set; }
        public int AirportCount { get; set; }
        public int ActiveAirportCount { get; set; }
        public int ExportDocumentCount { get; set; }
        public int ImportDocumentCount { get; set; }
        public int TransitDocumentCount { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class CountryDetailResponse : CountrySummaryResponse
    {
        public List<AirportResponse> Airports { get; set; } = new();
        public List<CountryDocRequirementResponse> Requirements { get; set; } = new();
        public List<PriceItemResponse> CountryFees { get; set; } = new();
    }
}
