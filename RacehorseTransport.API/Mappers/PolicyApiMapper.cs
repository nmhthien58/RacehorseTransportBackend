using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;

namespace RacehorseTransport.API.Mappers
{
    public static class PolicyApiMapper
    {
        public static PolicySettingsResponse ToResponse(this PolicySettingsModel model)
        {
            return new PolicySettingsResponse
            {
                PolicyVersion = model.PolicyVersion,
                QuoteAcceptDays = model.QuoteAcceptDays,
                DelayGraceHours = model.DelayGraceHours,
                DelayPercentPer12Hours = model.DelayPercentPer12Hours,
                DelayMaxPercent = model.DelayMaxPercent,
                InsurancePremiumPercent = model.InsurancePremiumPercent,
                InsuranceBaseLiabilityUsd = model.InsuranceBaseLiabilityUsd,
                InsuranceBaseVetCapUsd = model.InsuranceBaseVetCapUsd,
                InsuranceVetCapPercent = model.InsuranceVetCapPercent,
                InsuranceProofThresholdUsd = model.InsuranceProofThresholdUsd,
                HorseMinAgeMonths = model.HorseMinAgeMonths,
                DeliveryFreeWaitingHours = model.DeliveryFreeWaitingHours,
                DeliveryWaitingFeePerHourUsd = model.DeliveryWaitingFeePerHourUsd,
                DeliveryMaxWaitingHours = model.DeliveryMaxWaitingHours,
                DeliveryDecisionHours = model.DeliveryDecisionHours,
                UpdatedAt = model.UpdatedAt,
                UpdatedByName = model.UpdatedByName
            };
        }

        public static PolicySettingsModel ToBusinessModel(this UpdatePolicySettingsRequest request)
        {
            return new PolicySettingsModel
            {
                PolicyVersion = request.PolicyVersion.Trim(),
                QuoteAcceptDays = request.QuoteAcceptDays,
                DelayGraceHours = request.DelayGraceHours,
                DelayPercentPer12Hours = request.DelayPercentPer12Hours,
                DelayMaxPercent = request.DelayMaxPercent,
                InsurancePremiumPercent = request.InsurancePremiumPercent,
                InsuranceBaseLiabilityUsd = request.InsuranceBaseLiabilityUsd,
                InsuranceBaseVetCapUsd = request.InsuranceBaseVetCapUsd,
                InsuranceVetCapPercent = request.InsuranceVetCapPercent,
                InsuranceProofThresholdUsd = request.InsuranceProofThresholdUsd,
                HorseMinAgeMonths = request.HorseMinAgeMonths,
                DeliveryFreeWaitingHours = request.DeliveryFreeWaitingHours,
                DeliveryWaitingFeePerHourUsd = request.DeliveryWaitingFeePerHourUsd,
                DeliveryMaxWaitingHours = request.DeliveryMaxWaitingHours,
                DeliveryDecisionHours = request.DeliveryDecisionHours
            };
        }

        public static PublicPolicyResponse ToResponse(this PublicPolicyModel model)
        {
            var p = model.Payment;
            var s = model.Settings;
            return new PublicPolicyResponse
            {
                Version = model.Version,
                UpdatedAt = model.UpdatedAt,
                Payment = new PolicyPaymentSection
                {
                    DepositPercent = p.DepositPercent,
                    CancelRefundPercent = p.CancelRefundPercent,
                    UsdVndRate = p.UsdVndRate,
                    BalanceDueDaysBeforeDeparture = p.BalanceDueDaysBeforeDeparture,
                    ExtraPaymentDueDays = p.ExtraPaymentDueDays
                },
                Booking = new PolicyBookingSection { QuoteAcceptDays = s.QuoteAcceptDays },
                Delay = new PolicyDelaySection
                {
                    GraceHours = s.DelayGraceHours,
                    PercentPer12Hours = s.DelayPercentPer12Hours,
                    MaxPercent = s.DelayMaxPercent
                },
                Insurance = new PolicyInsuranceSection
                {
                    PremiumPercent = s.InsurancePremiumPercent,
                    BaseLiabilityUsd = s.InsuranceBaseLiabilityUsd,
                    BaseVetCapUsd = s.InsuranceBaseVetCapUsd,
                    VetCapPercent = s.InsuranceVetCapPercent,
                    ProofThresholdUsd = s.InsuranceProofThresholdUsd
                },
                Horse = new PolicyHorseSection { MinAgeMonths = s.HorseMinAgeMonths },
                Delivery = new PolicyDeliverySection
                {
                    FreeWaitingHours = s.DeliveryFreeWaitingHours,
                    WaitingFeePerHourUsd = s.DeliveryWaitingFeePerHourUsd,
                    MaxWaitingHours = s.DeliveryMaxWaitingHours,
                    DecisionHours = s.DeliveryDecisionHours
                },
                Values = new Dictionary<string, object>
                {
                    [SystemSettingService.PolicyVersionKey] = s.PolicyVersion,
                    [SystemSettingService.DepositPercentKey] = p.DepositPercent,
                    [SystemSettingService.CancelRefundPercentKey] = p.CancelRefundPercent,
                    [SystemSettingService.UsdVndRateKey] = p.UsdVndRate,
                    [SystemSettingService.BalanceDueDaysKey] = p.BalanceDueDaysBeforeDeparture,
                    [SystemSettingService.ExtraDueDaysKey] = p.ExtraPaymentDueDays,
                    [SystemSettingService.QuoteAcceptDaysKey] = s.QuoteAcceptDays,
                    [SystemSettingService.DelayGraceHoursKey] = s.DelayGraceHours,
                    [SystemSettingService.DelayPercentPer12HoursKey] = s.DelayPercentPer12Hours,
                    [SystemSettingService.DelayMaxPercentKey] = s.DelayMaxPercent,
                    [SystemSettingService.InsurancePremiumPercentKey] = s.InsurancePremiumPercent,
                    [SystemSettingService.InsuranceBaseLiabilityUsdKey] = s.InsuranceBaseLiabilityUsd,
                    [SystemSettingService.InsuranceBaseVetCapUsdKey] = s.InsuranceBaseVetCapUsd,
                    [SystemSettingService.InsuranceVetCapPercentKey] = s.InsuranceVetCapPercent,
                    [SystemSettingService.InsuranceProofThresholdUsdKey] = s.InsuranceProofThresholdUsd,
                    [SystemSettingService.HorseMinAgeMonthsKey] = s.HorseMinAgeMonths,
                    [SystemSettingService.DeliveryFreeWaitingHoursKey] = s.DeliveryFreeWaitingHours,
                    [SystemSettingService.DeliveryWaitingFeePerHourUsdKey] = s.DeliveryWaitingFeePerHourUsd,
                    [SystemSettingService.DeliveryMaxWaitingHoursKey] = s.DeliveryMaxWaitingHours,
                    [SystemSettingService.DeliveryDecisionHoursKey] = s.DeliveryDecisionHours
                }
            };
        }
    }
}
