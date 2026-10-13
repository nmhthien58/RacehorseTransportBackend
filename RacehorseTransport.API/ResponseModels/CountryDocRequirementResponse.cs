namespace RacehorseTransport.API.ResponseModels
{
    public class CountryDocRequirementResponse
    {
        public int RequirementId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public int DocTypeId { get; set; }
        public string DocTypeCode { get; set; } = string.Empty;
        public string DocTypeName { get; set; } = string.Empty;
        public int? ValidityDays { get; set; }
        public bool IsMandatory { get; set; }
        public string? RegulationNote { get; set; }
    }
}
