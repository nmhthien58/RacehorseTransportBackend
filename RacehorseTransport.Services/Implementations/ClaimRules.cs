using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // R-13 (BE-12, Chính sách mục 8.2): trần chi trả bồi thường theo gói. Hàm thuần để dễ viết unit test.
    public static class ClaimRules
    {
        // Khách tự mở hồ sơ trong 24 giờ sau khi nhận ngựa (mục 8.5)
        public static readonly TimeSpan CustomerClaimWindow = TimeSpan.FromHours(24);

        // Không mua bảo hiểm: chỉ trả khi lỗi công ty; chết / mất ≤ BaseLiabilityUsd, thương ≤ BaseVetCapUsd.
        // Toàn giá trị: mọi nguyên nhân; chết / mất ≤ 100% giá trị thỏa thuận, thương ≤ VetCapPercent% giá trị thỏa thuận.
        // Trả về 0 khi gói không mua mà không phải lỗi công ty.
        public static decimal Cap(string plan, string claimType, decimal? agreedValue, string? faultParty, PolicySettingsModel policy)
        {
            bool full = plan == InsurancePlans.Full;
            if (!full && faultParty != FaultParties.Company)
            {
                return 0m;
            }
            decimal value = agreedValue ?? 0m;
            return (full, claimType) switch
            {
                (false, ClaimTypes.Injury) => policy.InsuranceBaseVetCapUsd,
                (false, _) => policy.InsuranceBaseLiabilityUsd,
                (true, ClaimTypes.Injury) => Math.Round(value * policy.InsuranceVetCapPercent / 100, 2, MidpointRounding.AwayFromZero),
                _ => value
            };
        }

        public static void EnsureApprovable(string plan, string claimType, decimal? agreedValue, string? faultParty, decimal amount, PolicySettingsModel policy)
        {
            if (amount <= 0)
            {
                throw new BusinessException("ApprovedAmount must be greater than 0. Reject the claim instead.");
            }
            if (plan != InsurancePlans.Full && faultParty != FaultParties.Company)
            {
                throw new BusinessException("This horse has no insurance: compensation is only paid when the company is at fault (FaultParty = Company).");
            }
            decimal cap = Cap(plan, claimType, agreedValue, faultParty, policy);
            if (amount > cap)
            {
                throw new BusinessException($"The approved amount {amount:N2} USD is above the cap of {cap:N2} USD for a {claimType} claim with plan {plan}.");
            }
        }

        public static bool WithinCustomerWindow(DateTimeOffset? deliveredAt, DateTimeOffset now) =>
            deliveredAt.HasValue && now >= deliveredAt.Value && now - deliveredAt.Value <= CustomerClaimWindow;
    }
}
