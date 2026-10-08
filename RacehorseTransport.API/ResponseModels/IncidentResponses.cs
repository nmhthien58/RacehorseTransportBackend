namespace RacehorseTransport.API.ResponseModels
{
    public class IncidentResponse
    {
        public int IncidentId { get; set; }
        public string IncidentCode { get; set; } = string.Empty;
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string TripStatus { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? AffectedTripHorseId { get; set; }
        public string? AffectedHorseName { get; set; }
        public int ReportedByUserId { get; set; }
        public string? ReportedByName { get; set; }
        public DateTimeOffset ReportedAt { get; set; }
        public string? ProposedAction { get; set; }
        public string? RevisedRouteNotes { get; set; }
        public decimal AdditionalCost { get; set; }
        public string? RejectionReason { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
    }
}
