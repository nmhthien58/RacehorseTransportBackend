namespace RacehorseTransport.Services.BusinessModels
{
    // Cấu hình dùng trong Chính sách dịch vụ (docs/CHINH_SACH_DICH_VU.md), lưu ở common.system_settings.
    // Giá trị mặc định = giá trị ghi trong chính sách; thiếu khóa trong DB thì dùng mặc định.
    public class PolicySettingsModel
    {
        public string PolicyVersion { get; set; } = "1.1";            // Policy.Version

        public int QuoteAcceptDays { get; set; } = 3;                  // Booking.QuoteAcceptDays
        public int DossierSubmitDays { get; set; } = 7;                // Dossier.SubmitDays (R-10: hạn nộp giấy tính từ lúc cọc)
        public decimal StableFeePerHorsePerDayUsd { get; set; } = 50;  // Stable.FeePerHorsePerDayUsd (R-14: chuồng tạm + chăm sóc)
        public decimal DeliveryExtraKmFeeUsd { get; set; } = 2;        // Delivery.ExtraKmFeeUsd (R-16: giao chỗ khác, mỗi km thêm)

        public int DelayGraceHours { get; set; } = 6;                  // Delay.GraceHours
        public decimal DelayPercentPer12Hours { get; set; } = 5;       // Delay.PercentPer12Hours
        public decimal DelayMaxPercent { get; set; } = 25;             // Delay.MaxPercent

        public decimal InsurancePremiumPercent { get; set; } = 1;      // Insurance.PremiumPercent
        public decimal InsuranceBaseLiabilityUsd { get; set; } = 5000; // Insurance.BaseLiabilityUsd
        public decimal InsuranceBaseVetCapUsd { get; set; } = 1000;    // Insurance.BaseVetCapUsd
        public decimal InsuranceVetCapPercent { get; set; } = 20;      // Insurance.VetCapPercent
        public decimal InsuranceProofThresholdUsd { get; set; } = 50000; // Insurance.ProofThresholdUsd

        public int HorseMinAgeMonths { get; set; } = 6;                // Horse.MinAgeMonths
        public int HorseGestationDays { get; set; } = 340;             // Horse.GestationDays
        public decimal HorseLatePregnancyPercent { get; set; } = 90;   // Horse.LatePregnancyPercent
        public int HorsePostFoalingDays { get; set; } = 7;             // Horse.PostFoalingDays

        public int DeliveryFreeWaitingHours { get; set; } = 2;         // Delivery.FreeWaitingHours
        public decimal DeliveryWaitingFeePerHourUsd { get; set; } = 40; // Delivery.WaitingFeePerHourUsd
        public int DeliveryMaxWaitingHours { get; set; } = 24;         // Delivery.MaxWaitingHours
        public int DeliveryDecisionHours { get; set; } = 24;           // Delivery.DecisionHours
        public int DeliveryDeadlineBufferDays { get; set; } = 2;       // Delivery.DeadlineBufferDays (dự phòng cho hạn giao sớm nhất)

        public DateTimeOffset? UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }
    }

    // Trang Chính sách công khai: phiên bản + mọi giá trị cấu hình chính sách nhắc tới
    public class PublicPolicyModel
    {
        public string Version { get; set; } = string.Empty;
        public PaymentSettingsModel Payment { get; set; } = new();
        public PolicySettingsModel Settings { get; set; } = new();
        public DateTimeOffset? UpdatedAt { get; set; } // lần sửa cấu hình gần nhất (thanh toán hoặc chính sách)
    }
}
