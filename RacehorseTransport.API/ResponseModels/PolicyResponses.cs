namespace RacehorseTransport.API.ResponseModels
{
    // Cấu hình Chính sách dịch vụ cho màn quản trị (cùng dạng với body của PUT /api/settings/policy)
    public class PolicySettingsResponse
    {
        public string PolicyVersion { get; set; } = string.Empty;
        public int QuoteAcceptDays { get; set; }
        public int DossierSubmitDays { get; set; }
        public decimal StableFeePerHorsePerDayUsd { get; set; }
        public decimal DeliveryExtraKmFeeUsd { get; set; }
        public int DelayGraceHours { get; set; }
        public decimal DelayPercentPer12Hours { get; set; }
        public decimal DelayMaxPercent { get; set; }
        public decimal InsurancePremiumPercent { get; set; }
        public decimal InsuranceBaseLiabilityUsd { get; set; }
        public decimal InsuranceBaseVetCapUsd { get; set; }
        public decimal InsuranceVetCapPercent { get; set; }
        public decimal InsuranceProofThresholdUsd { get; set; }
        public int HorseMinAgeMonths { get; set; }
        public int HorseGestationDays { get; set; }
        public decimal HorseLatePregnancyPercent { get; set; }
        public int HorsePostFoalingDays { get; set; }
        public int DeliveryFreeWaitingHours { get; set; }
        public decimal DeliveryWaitingFeePerHourUsd { get; set; }
        public int DeliveryMaxWaitingHours { get; set; }
        public int DeliveryDecisionHours { get; set; }
        public int DeliveryDeadlineBufferDays { get; set; }
        public int EuthanasiaConsentMinutes { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }
    }

    // GET /api/policy: dữ liệu cho trang Chính sách công khai
    public class PublicPolicyResponse
    {
        public string Version { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Theo nhóm, để hiển thị
        public PolicyPaymentSection Payment { get; set; } = new();
        public PolicyBookingSection Booking { get; set; } = new();
        public PolicyDelaySection Delay { get; set; } = new();
        public PolicyInsuranceSection Insurance { get; set; } = new();
        public PolicyHorseSection Horse { get; set; } = new();
        public PolicyDeliverySection Delivery { get; set; } = new();
        public PolicyIncidentSection Incident { get; set; } = new();

        // Theo đúng tên khóa trong chính sách (ví dụ "Delay.GraceHours"), để thay các chỗ [...] trong văn bản
        public Dictionary<string, object> Values { get; set; } = new();
    }

    public class PolicyPaymentSection
    {
        public decimal DepositPercent { get; set; }
        public decimal CancelRefundPercent { get; set; }
        public decimal UsdVndRate { get; set; }
        public int BalanceDueDaysBeforeDeparture { get; set; }
        public int ExtraPaymentDueDays { get; set; }
    }

    public class PolicyBookingSection
    {
        public int QuoteAcceptDays { get; set; }
        public int DossierSubmitDays { get; set; }
    }

    public class PolicyDelaySection
    {
        public int GraceHours { get; set; }
        public decimal PercentPer12Hours { get; set; }
        public decimal MaxPercent { get; set; }
    }

    public class PolicyInsuranceSection
    {
        public decimal PremiumPercent { get; set; }
        public decimal BaseLiabilityUsd { get; set; }
        public decimal BaseVetCapUsd { get; set; }
        public decimal VetCapPercent { get; set; }
        public decimal ProofThresholdUsd { get; set; }
    }

    public class PolicyHorseSection
    {
        public int MinAgeMonths { get; set; }
        public int GestationDays { get; set; }
        public decimal LatePregnancyPercent { get; set; }
        public int PostFoalingDays { get; set; }
    }

    public class PolicyIncidentSection
    {
        public int EuthanasiaConsentMinutes { get; set; }
    }

    public class PolicyDeliverySection
    {
        public decimal StableFeePerHorsePerDayUsd { get; set; }
        public decimal ExtraKmFeeUsd { get; set; }
        public int FreeWaitingHours { get; set; }
        public decimal WaitingFeePerHourUsd { get; set; }
        public int MaxWaitingHours { get; set; }
        public int DecisionHours { get; set; }
        public int DeadlineBufferDays { get; set; }
    }
}
