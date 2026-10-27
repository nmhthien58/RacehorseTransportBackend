namespace RacehorseTransport.Repositories.Entities
{
    // billing.insurance_claims (R-13, BE-12): hồ sơ bồi thường cho một con ngựa
    public class InsuranceClaim
    {
        public int ClaimId { get; set; }
        public string ClaimCode { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public int BookingHorseId { get; set; }
        public int? IncidentId { get; set; }
        public int? DeliveryFailureId { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public string InsurancePlan { get; set; } = string.Empty;   // gói lúc xảy ra (None / Full)
        public decimal? AgreedValue { get; set; }                   // giá trị thỏa thuận lúc xảy ra
        public string? FaultParty { get; set; }                     // nguyên nhân dùng xét trần (gói None chỉ trả khi Company)
        public string Description { get; set; } = string.Empty;
        public decimal? RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string Status { get; set; } = "Open";
        public string? DecisionReason { get; set; }
        public string EvidencePaths { get; set; } = "[]";           // jsonb
        public int? OpenedByUserId { get; set; }                    // null = hệ thống tự mở
        public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
        public int? ReviewedByUserId { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public string? PaidReference { get; set; }

        public virtual BookingRequest? Booking { get; set; }
        public virtual BookingHorse? BookingHorse { get; set; }
        public virtual User? ReviewedBy { get; set; }
    }
}
