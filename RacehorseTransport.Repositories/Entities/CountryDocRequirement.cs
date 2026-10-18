namespace RacehorseTransport.Repositories.Entities
{
    // clearance.country_doc_requirements
    public class CountryDocRequirement
    {
        public int RequirementId { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public int DocTypeId { get; set; }
        public int? ValidityDays { get; set; }
        public int? ProcessingDays { get; set; } // số ngày cần để có được giấy này (xin phép, xét nghiệm, cách ly...)
        public bool IsMandatory { get; set; } = true;
        public string? RegulationNote { get; set; }

        // Navigation properties
        public virtual DocumentType? DocumentType { get; set; }
    }
}
