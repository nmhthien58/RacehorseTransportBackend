namespace RacehorseTransport.Services.BusinessModels
{
    // R-18: phạm vi dịch vụ theo quốc gia

    public class AirportModel
    {
        public int AirportId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string IataCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public bool IsActive { get; set; }
    }

    // Danh sách cho form đặt chuyến: chỉ nước đang bật, chỉ sân bay đang mở
    public class CountryCatalogModel
    {
        public string CountryCode { get; set; } = string.Empty;
        public string NameVi { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool GroundAvailable { get; set; } // nhận làm nước đến bằng đường bộ
        public bool AirAvailable { get; set; }    // nhận làm nước đến bằng đường bay
        public List<AirportModel> Airports { get; set; } = new();
    }

    // Dòng cước FREIGHT của một nước theo một phương thức (null = chưa khai báo)
    public class CountryFreightModel
    {
        public int PriceItemId { get; set; }
        public decimal Value { get; set; }
        public string CalcType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CountrySummaryModel
    {
        public string CountryCode { get; set; } = string.Empty;
        public string NameVi { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public CountryFreightModel? GroundFreight { get; set; }
        public CountryFreightModel? AirFreight { get; set; }
        public bool GroundAvailable { get; set; }
        public bool AirAvailable { get; set; }
        public int AirportCount { get; set; }
        public int ActiveAirportCount { get; set; }
        public int ExportDocumentCount { get; set; }
        public int ImportDocumentCount { get; set; }
        public int TransitDocumentCount { get; set; }
        public List<string> Warnings { get; set; } = new(); // CountryWarnings
    }

    public class CountryDetailModel : CountrySummaryModel
    {
        public List<AirportModel> Airports { get; set; } = new();
        public List<CountryDocRequirementModel> Requirements { get; set; } = new();
        public List<PriceItemModel> CountryFees { get; set; } = new(); // phí riêng của nước, trừ cước FREIGHT
    }

    public class SaveCountryModel
    {
        public string CountryCode { get; set; } = string.Empty;
        public string NameVi { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class SaveAirportModel
    {
        public string IataCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
    }
}
