namespace RacehorseTransport.Services.BusinessModels
{
    // R-13 (BE-12): khách tự mở hồ sơ bồi thường
    public class OpenClaimModel
    {
        public int BookingHorseId { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? RequestedAmount { get; set; }
        public List<string>? EvidencePaths { get; set; }
    }

    public class ClaimModel
    {
        public int ClaimId { get; set; }
        public string ClaimCode { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int BookingHorseId { get; set; }
        public string HorseName { get; set; } = string.Empty;
        public int? IncidentId { get; set; }
        public int? DeliveryFailureId { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public string InsurancePlan { get; set; } = string.Empty;
        public decimal? AgreedValue { get; set; }
        public string? FaultParty { get; set; }
        public decimal Cap { get; set; }                 // trần chi trả theo gói
        public string Description { get; set; } = string.Empty;
        public decimal? RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DecisionReason { get; set; }
        public List<string> EvidencePaths { get; set; } = new();
        public List<string> EvidenceUrls { get; set; } = new();
        public bool OpenedBySystem { get; set; }
        public DateTimeOffset OpenedAt { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public string? PaidReference { get; set; }
    }
}
