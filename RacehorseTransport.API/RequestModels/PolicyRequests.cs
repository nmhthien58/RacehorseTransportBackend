using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    // BE-17: sửa cấu hình Chính sách dịch vụ (Manager / Admin). Gửi đủ mọi trường, kể cả trường không đổi.
    public class UpdatePolicySettingsRequest
    {
        [Required(ErrorMessage = "PolicyVersion is required.")]
        [MaxLength(20, ErrorMessage = "PolicyVersion cannot exceed 20 characters.")]
        public string PolicyVersion { get; set; } = string.Empty;

        [Range(1, 30, ErrorMessage = "QuoteAcceptDays must be between 1 and 30.")]
        public int QuoteAcceptDays { get; set; }

        [Range(0, 168, ErrorMessage = "DelayGraceHours must be between 0 and 168.")]
        public int DelayGraceHours { get; set; }

        [Range(0, 100, ErrorMessage = "DelayPercentPer12Hours must be between 0 and 100.")]
        public decimal DelayPercentPer12Hours { get; set; }

        [Range(0, 100, ErrorMessage = "DelayMaxPercent must be between 0 and 100.")]
        public decimal DelayMaxPercent { get; set; }

        [Range(0, 100, ErrorMessage = "InsurancePremiumPercent must be between 0 and 100.")]
        public decimal InsurancePremiumPercent { get; set; }

        [Range(0, 1000000000, ErrorMessage = "InsuranceBaseLiabilityUsd must be between 0 and 1,000,000,000.")]
        public decimal InsuranceBaseLiabilityUsd { get; set; }

        [Range(0, 1000000000, ErrorMessage = "InsuranceBaseVetCapUsd must be between 0 and 1,000,000,000.")]
        public decimal InsuranceBaseVetCapUsd { get; set; }

        [Range(0, 100, ErrorMessage = "InsuranceVetCapPercent must be between 0 and 100.")]
        public decimal InsuranceVetCapPercent { get; set; }

        [Range(0, 1000000000, ErrorMessage = "InsuranceProofThresholdUsd must be between 0 and 1,000,000,000.")]
        public decimal InsuranceProofThresholdUsd { get; set; }

        [Range(0, 240, ErrorMessage = "HorseMinAgeMonths must be between 0 and 240.")]
        public int HorseMinAgeMonths { get; set; }

        [Range(300, 400, ErrorMessage = "HorseGestationDays must be between 300 and 400.")]
        public int HorseGestationDays { get; set; }

        [Range(0, 100, ErrorMessage = "HorseLatePregnancyPercent must be between 0 and 100.")]
        public decimal HorseLatePregnancyPercent { get; set; }

        [Range(0, 60, ErrorMessage = "HorsePostFoalingDays must be between 0 and 60.")]
        public int HorsePostFoalingDays { get; set; }

        [Range(0, 72, ErrorMessage = "DeliveryFreeWaitingHours must be between 0 and 72.")]
        public int DeliveryFreeWaitingHours { get; set; }

        [Range(0, 100000, ErrorMessage = "DeliveryWaitingFeePerHourUsd must be between 0 and 100,000.")]
        public decimal DeliveryWaitingFeePerHourUsd { get; set; }

        [Range(1, 168, ErrorMessage = "DeliveryMaxWaitingHours must be between 1 and 168.")]
        public int DeliveryMaxWaitingHours { get; set; }

        [Range(1, 168, ErrorMessage = "DeliveryDecisionHours must be between 1 and 168.")]
        public int DeliveryDecisionHours { get; set; }
    }
}
