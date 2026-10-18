namespace RacehorseTransport.Services.BusinessModels
{
    public class CountryDocRequirementModel
    {
        public int RequirementId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public int DocTypeId { get; set; }
        public string DocTypeCode { get; set; } = string.Empty;
        public string DocTypeName { get; set; } = string.Empty;
        public int? ValidityDays { get; set; }
        public int? ProcessingDays { get; set; } // số ngày cần để có giấy; dùng tính ngày pickup sớm nhất
        public bool IsMandatory { get; set; }
        public string? RegulationNote { get; set; }
    }

    public class CountryDocRequirementQueryOptions : BusinessQueryOptions
    {
        public string? CountryCode { get; set; }
        public string? Direction { get; set; }
    }
}
