namespace RacehorseTransport.API.ResponseModels
{
    public class ContractResponse
    {
        public int ContractId { get; set; }
        public string ContractCode { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string FileSha256 { get; set; } = string.Empty;
        public string PolicyVersion { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public DateTimeOffset? FileUrlExpiresAt { get; set; }
        public int? AcceptedByUserId { get; set; }
        public string? AcceptedByName { get; set; }
        public DateTimeOffset? AcceptedAt { get; set; }
        public string? AcceptedIp { get; set; }
        public string? AcceptedUserAgent { get; set; }
        public string? AcceptedPolicyVersion { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class AirWaybillResponse
    {
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string? AirWaybillNumber { get; set; }
        public string? AirWaybillFilePath { get; set; }
        public string? AirWaybillFileUrl { get; set; }
    }
}
