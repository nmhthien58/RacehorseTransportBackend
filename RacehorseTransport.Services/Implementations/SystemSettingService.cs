using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // Đọc / ghi cấu hình trong common.system_settings. Thiếu khóa thì dùng giá trị mặc định của model
    // (PaymentSettingsModel, PolicySettingsModel). Tên khóa khớp đúng docs/CHINH_SACH_DICH_VU.md.
    public class SystemSettingService : ISystemSettingService
    {
        public const string DepositPercentKey = "Payment.DepositPercent";
        public const string CancelRefundPercentKey = "Payment.CancelRefundPercent";
        public const string UsdVndRateKey = "Payment.UsdVndRate";
        public const string BalanceDueDaysKey = "Payment.BalanceDueDaysBeforeDeparture";
        public const string ExtraDueDaysKey = "Payment.ExtraPaymentDueDays";

        private static readonly Dictionary<string, string> Descriptions = new()
        {
            [DepositPercentKey] = "% đặt cọc trên giá báo, thu sau khi Manager duyệt đơn",
            [CancelRefundPercentKey] = "% tiền cọc được hoàn khi khách hủy đơn trước khi xuất phát",
            [UsdVndRateKey] = "Tỉ giá quy đổi USD -> VND khi thanh toán qua VNPay",
            [BalanceDueDaysKey] = "Phần còn lại phải trả trước ngày xuất phát bao nhiêu ngày",
            [ExtraDueDaysKey] = "Hạn trả chi phí phát sinh (ngày) tính từ khi đóng chuyến"
        };

        // BE-17: các khóa của Chính sách dịch vụ
        public const string PolicyVersionKey = "Policy.Version";
        public const string QuoteAcceptDaysKey = "Booking.QuoteAcceptDays";
        public const string DossierSubmitDaysKey = "Dossier.SubmitDays";
        public const string DelayGraceHoursKey = "Delay.GraceHours";
        public const string DelayPercentPer12HoursKey = "Delay.PercentPer12Hours";
        public const string DelayMaxPercentKey = "Delay.MaxPercent";
        public const string InsurancePremiumPercentKey = "Insurance.PremiumPercent";
        public const string InsuranceBaseLiabilityUsdKey = "Insurance.BaseLiabilityUsd";
        public const string InsuranceBaseVetCapUsdKey = "Insurance.BaseVetCapUsd";
        public const string InsuranceVetCapPercentKey = "Insurance.VetCapPercent";
        public const string InsuranceProofThresholdUsdKey = "Insurance.ProofThresholdUsd";
        public const string HorseMinAgeMonthsKey = "Horse.MinAgeMonths";
        public const string HorseGestationDaysKey = "Horse.GestationDays";
        public const string HorseLatePregnancyPercentKey = "Horse.LatePregnancyPercent";
        public const string HorsePostFoalingDaysKey = "Horse.PostFoalingDays";
        public const string DeliveryFreeWaitingHoursKey = "Delivery.FreeWaitingHours";
        public const string DeliveryWaitingFeePerHourUsdKey = "Delivery.WaitingFeePerHourUsd";
        public const string DeliveryMaxWaitingHoursKey = "Delivery.MaxWaitingHours";
        public const string DeliveryDecisionHoursKey = "Delivery.DecisionHours";

        // Mỗi khóa chính sách: mô tả + cách đọc / ghi vào model (giá trị lưu dạng chuỗi, số theo InvariantCulture)
        private sealed record PolicyKey(string Key, string Description, Func<PolicySettingsModel, string> Get, Action<PolicySettingsModel, string> Set);

        private static string Num(decimal value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static decimal ParseDec(string value, decimal fallback) =>
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : fallback;
        private static int ParseInt(string value, int fallback) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

        private static readonly PolicyKey[] PolicyKeys =
        {
            new(PolicyVersionKey, "Phiên bản Chính sách dịch vụ hiện hành (lưu vào đơn khi khách đồng ý)",
                m => m.PolicyVersion, (m, v) => m.PolicyVersion = v),
            new(QuoteAcceptDaysKey, "Số ngày khách được xem và chấp nhận báo giá chốt + đặt cọc",
                m => Num(m.QuoteAcceptDays), (m, v) => m.QuoteAcceptDays = ParseInt(v, m.QuoteAcceptDays)),
            new(DossierSubmitDaysKey, "Số ngày khách có để nộp đủ giấy tờ, tính từ lúc đặt cọc",
                m => Num(m.DossierSubmitDays), (m, v) => m.DossierSubmitDays = ParseInt(v, m.DossierSubmitDays)),
            new(DelayGraceHoursKey, "Số giờ trễ được miễn bồi thường",
                m => Num(m.DelayGraceHours), (m, v) => m.DelayGraceHours = ParseInt(v, m.DelayGraceHours)),
            new(DelayPercentPer12HoursKey, "% tiền cước bồi thường cho mỗi 12 giờ trễ sau ân hạn",
                m => Num(m.DelayPercentPer12Hours), (m, v) => m.DelayPercentPer12Hours = ParseDec(v, m.DelayPercentPer12Hours)),
            new(DelayMaxPercentKey, "% tiền cước bồi thường giao trễ tối đa",
                m => Num(m.DelayMaxPercent), (m, v) => m.DelayMaxPercent = ParseDec(v, m.DelayMaxPercent)),
            new(InsurancePremiumPercentKey, "% phí bảo hiểm toàn giá trị trên giá trị thỏa thuận mỗi con",
                m => Num(m.InsurancePremiumPercent), (m, v) => m.InsurancePremiumPercent = ParseDec(v, m.InsurancePremiumPercent)),
            new(InsuranceBaseLiabilityUsdKey, "Trách nhiệm tối đa (USD / con) khi ngựa chết / mất mà không mua bảo hiểm",
                m => Num(m.InsuranceBaseLiabilityUsd), (m, v) => m.InsuranceBaseLiabilityUsd = ParseDec(v, m.InsuranceBaseLiabilityUsd)),
            new(InsuranceBaseVetCapUsdKey, "Chi phí thú y tối đa (USD / con) khi ngựa bị thương mà không mua bảo hiểm",
                m => Num(m.InsuranceBaseVetCapUsd), (m, v) => m.InsuranceBaseVetCapUsd = ParseDec(v, m.InsuranceBaseVetCapUsd)),
            new(InsuranceVetCapPercentKey, "% giá trị thỏa thuận tối đa cho chi phí thú y khi có bảo hiểm",
                m => Num(m.InsuranceVetCapPercent), (m, v) => m.InsuranceVetCapPercent = ParseDec(v, m.InsuranceVetCapPercent)),
            new(InsuranceProofThresholdUsdKey, "Giá trị khai báo (USD / con) từ mức này phải kèm chứng từ",
                m => Num(m.InsuranceProofThresholdUsd), (m, v) => m.InsuranceProofThresholdUsd = ParseDec(v, m.InsuranceProofThresholdUsd)),
            new(HorseMinAgeMonthsKey, "Tuổi tối thiểu (tháng) của ngựa, tính tới ngày cuối của khoảng đón",
                m => Num(m.HorseMinAgeMonths), (m, v) => m.HorseMinAgeMonths = ParseInt(v, m.HorseMinAgeMonths)),
            new(HorseGestationDaysKey, "Số ngày thai kỳ trung bình của ngựa",
                m => Num(m.HorseGestationDays), (m, v) => m.HorseGestationDays = ParseInt(v, m.HorseGestationDays)),
            new(HorseLatePregnancyPercentKey, "Không chở ngựa cái đã quá % thai kỳ này (tính tại ngày cuối của khoảng đón)",
                m => Num(m.HorseLatePregnancyPercent), (m, v) => m.HorseLatePregnancyPercent = ParseDec(v, m.HorseLatePregnancyPercent)),
            new(HorsePostFoalingDaysKey, "Không chở ngựa cái vừa đẻ trong số ngày này",
                m => Num(m.HorsePostFoalingDays), (m, v) => m.HorsePostFoalingDays = ParseInt(v, m.HorsePostFoalingDays)),
            new(DeliveryFreeWaitingHoursKey, "Số giờ xe chờ miễn phí tại điểm giao",
                m => Num(m.DeliveryFreeWaitingHours), (m, v) => m.DeliveryFreeWaitingHours = ParseInt(v, m.DeliveryFreeWaitingHours)),
            new(DeliveryWaitingFeePerHourUsdKey, "Phí chờ (USD / giờ) sau thời gian chờ miễn phí",
                m => Num(m.DeliveryWaitingFeePerHourUsd), (m, v) => m.DeliveryWaitingFeePerHourUsd = ParseDec(v, m.DeliveryWaitingFeePerHourUsd)),
            new(DeliveryMaxWaitingHoursKey, "Số giờ chờ tính phí tối đa",
                m => Num(m.DeliveryMaxWaitingHours), (m, v) => m.DeliveryMaxWaitingHours = ParseInt(v, m.DeliveryMaxWaitingHours)),
            new(DeliveryDecisionHoursKey, "Số giờ khách có để chọn cách xử lý khi không giao được",
                m => Num(m.DeliveryDecisionHours), (m, v) => m.DeliveryDecisionHours = ParseInt(v, m.DeliveryDecisionHours)),
        };

        private readonly IRepository<SystemSetting> _settingRepo;
        private readonly IRepository<User> _userRepo;

        public SystemSettingService(IRepository<SystemSetting> settingRepo, IRepository<User> userRepo)
        {
            _settingRepo = settingRepo;
            _userRepo = userRepo;
        }

        public async Task<PaymentSettingsModel> GetPaymentSettingsAsync()
        {
            var rows = await _settingRepo.GetQueryable().AsNoTracking()
                .Where(s => s.SettingKey.StartsWith("Payment."))
                .ToListAsync();

            var model = new PaymentSettingsModel();
            decimal Dec(string key, decimal fallback) =>
                rows.FirstOrDefault(r => r.SettingKey == key) is { } r
                && decimal.TryParse(r.SettingValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : fallback;

            model.DepositPercent = Dec(DepositPercentKey, model.DepositPercent);
            model.CancelRefundPercent = Dec(CancelRefundPercentKey, model.CancelRefundPercent);
            model.UsdVndRate = Dec(UsdVndRateKey, model.UsdVndRate);
            model.BalanceDueDaysBeforeDeparture = (int)Dec(BalanceDueDaysKey, model.BalanceDueDaysBeforeDeparture);
            model.ExtraPaymentDueDays = (int)Dec(ExtraDueDaysKey, model.ExtraPaymentDueDays);

            var latest = rows.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
            if (latest != null)
            {
                model.UpdatedAt = latest.UpdatedAt;
                if (latest.UpdatedByUserId.HasValue)
                {
                    model.UpdatedByName = await _userRepo.GetQueryable().AsNoTracking()
                        .Where(u => u.UserId == latest.UpdatedByUserId.Value)
                        .Select(u => u.FullName)
                        .FirstOrDefaultAsync();
                }
            }

            return model;
        }

        public async Task<PaymentSettingsModel> UpdatePaymentSettingsAsync(PaymentSettingsModel model, CurrentUser currentUser)
        {
            if (model.DepositPercent < 0 || model.DepositPercent > 100)
            {
                throw new BusinessException("DepositPercent must be between 0 and 100.");
            }
            if (model.CancelRefundPercent < 0 || model.CancelRefundPercent > 100)
            {
                throw new BusinessException("CancelRefundPercent must be between 0 and 100.");
            }
            if (model.UsdVndRate <= 0)
            {
                throw new BusinessException("UsdVndRate must be greater than 0.");
            }
            if (model.BalanceDueDaysBeforeDeparture < 0 || model.ExtraPaymentDueDays < 1)
            {
                throw new BusinessException("BalanceDueDaysBeforeDeparture cannot be negative and ExtraPaymentDueDays must be at least 1.");
            }

            var values = new Dictionary<string, string>
            {
                [DepositPercentKey] = model.DepositPercent.ToString(CultureInfo.InvariantCulture),
                [CancelRefundPercentKey] = model.CancelRefundPercent.ToString(CultureInfo.InvariantCulture),
                [UsdVndRateKey] = model.UsdVndRate.ToString(CultureInfo.InvariantCulture),
                [BalanceDueDaysKey] = model.BalanceDueDaysBeforeDeparture.ToString(CultureInfo.InvariantCulture),
                [ExtraDueDaysKey] = model.ExtraPaymentDueDays.ToString(CultureInfo.InvariantCulture)
            };

            var existing = await _settingRepo.GetQueryable().Where(s => values.Keys.Contains(s.SettingKey)).ToListAsync();
            var now = DateTimeOffset.UtcNow;

            foreach (var (key, value) in values)
            {
                var row = existing.FirstOrDefault(s => s.SettingKey == key);
                if (row == null)
                {
                    await _settingRepo.AddAsync(new SystemSetting
                    {
                        SettingKey = key,
                        SettingValue = value,
                        Description = Descriptions[key],
                        UpdatedByUserId = currentUser.UserId,
                        UpdatedAt = now
                    });
                }
                else if (row.SettingValue != value)
                {
                    row.SettingValue = value;
                    row.UpdatedByUserId = currentUser.UserId;
                    row.UpdatedAt = now;
                }
            }

            await _settingRepo.SaveChangesAsync();
            return await GetPaymentSettingsAsync();
        }

        // =============================================================================================
        // Cấu hình Chính sách dịch vụ (BE-17)
        // =============================================================================================

        // R-17: khóa cấu hình -> giá trị (dạng invariant) để thay các giá trị [Khóa = ...] trong văn bản chính sách
        public static Dictionary<string, string> ConfigValues(PaymentSettingsModel payment, PolicySettingsModel policy)
        {
            var values = PolicyKeys.ToDictionary(k => k.Key, k => k.Get(policy));
            values[DepositPercentKey] = Num(payment.DepositPercent);
            values[CancelRefundPercentKey] = Num(payment.CancelRefundPercent);
            values[UsdVndRateKey] = Num(payment.UsdVndRate);
            values[BalanceDueDaysKey] = Num(payment.BalanceDueDaysBeforeDeparture);
            values[ExtraDueDaysKey] = Num(payment.ExtraPaymentDueDays);
            return values;
        }

        public async Task<PolicySettingsModel> GetPolicySettingsAsync()
        {
            var keys = PolicyKeys.Select(k => k.Key).ToList();
            var rows = await _settingRepo.GetQueryable().AsNoTracking()
                .Where(s => keys.Contains(s.SettingKey))
                .ToListAsync();

            var model = new PolicySettingsModel();
            foreach (var key in PolicyKeys)
            {
                var row = rows.FirstOrDefault(r => r.SettingKey == key.Key);
                if (row != null && !string.IsNullOrWhiteSpace(row.SettingValue))
                {
                    key.Set(model, row.SettingValue.Trim());
                }
            }

            var latest = rows.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
            if (latest != null)
            {
                model.UpdatedAt = latest.UpdatedAt;
                model.UpdatedByName = await GetUserNameAsync(latest.UpdatedByUserId);
            }
            return model;
        }

        public async Task<PolicySettingsModel> UpdatePolicySettingsAsync(PolicySettingsModel model, CurrentUser currentUser)
        {
            ValidatePolicySettings(model);

            var values = PolicyKeys.ToDictionary(k => k.Key, k => k.Get(model));
            var existing = await _settingRepo.GetQueryable().Where(s => values.Keys.Contains(s.SettingKey)).ToListAsync();
            var now = DateTimeOffset.UtcNow;

            foreach (var key in PolicyKeys)
            {
                string value = values[key.Key];
                var row = existing.FirstOrDefault(s => s.SettingKey == key.Key);
                if (row == null)
                {
                    await _settingRepo.AddAsync(new SystemSetting
                    {
                        SettingKey = key.Key,
                        SettingValue = value,
                        Description = key.Description,
                        UpdatedByUserId = currentUser.UserId,
                        UpdatedAt = now
                    });
                }
                else if (row.SettingValue != value)
                {
                    row.SettingValue = value;
                    row.UpdatedByUserId = currentUser.UserId;
                    row.UpdatedAt = now;
                }
            }

            await _settingRepo.SaveChangesAsync();
            return await GetPolicySettingsAsync();
        }

        public async Task<PublicPolicyModel> GetPublicPolicyAsync()
        {
            var payment = await GetPaymentSettingsAsync();
            var settings = await GetPolicySettingsAsync();

            // Trang công khai không lộ tên nhân sự đã sửa cấu hình
            payment.UpdatedByName = null;
            settings.UpdatedByName = null;

            return new PublicPolicyModel
            {
                Version = settings.PolicyVersion,
                Payment = payment,
                Settings = settings,
                UpdatedAt = new[] { payment.UpdatedAt, settings.UpdatedAt }.Max()
            };
        }

        // Hàm thuần để viết unit test: giá trị âm, % > 100, số giờ / ngày ngoài khoảng hợp lý -> 400
        public static void ValidatePolicySettings(PolicySettingsModel model)
        {
            var errors = new List<string>();

            void Percent(decimal value, string key)
            {
                if (value < 0 || value > 100) errors.Add($"{key} must be between 0 and 100.");
            }
            void NonNegative(decimal value, string key)
            {
                if (value < 0) errors.Add($"{key} cannot be negative.");
            }
            void Between(int value, int min, int max, string key)
            {
                if (value < min || value > max) errors.Add($"{key} must be between {min} and {max}.");
            }

            string version = model.PolicyVersion?.Trim() ?? string.Empty;
            if (version.Length == 0 || version.Length > 20)
            {
                errors.Add($"{PolicyVersionKey} is required and cannot exceed 20 characters.");
            }

            Between(model.QuoteAcceptDays, 1, 30, QuoteAcceptDaysKey);
            Between(model.DossierSubmitDays, 1, 60, DossierSubmitDaysKey);

            Between(model.DelayGraceHours, 0, 168, DelayGraceHoursKey);
            Percent(model.DelayPercentPer12Hours, DelayPercentPer12HoursKey);
            Percent(model.DelayMaxPercent, DelayMaxPercentKey);

            Percent(model.InsurancePremiumPercent, InsurancePremiumPercentKey);
            NonNegative(model.InsuranceBaseLiabilityUsd, InsuranceBaseLiabilityUsdKey);
            NonNegative(model.InsuranceBaseVetCapUsd, InsuranceBaseVetCapUsdKey);
            Percent(model.InsuranceVetCapPercent, InsuranceVetCapPercentKey);
            NonNegative(model.InsuranceProofThresholdUsd, InsuranceProofThresholdUsdKey);

            Between(model.HorseMinAgeMonths, 0, 240, HorseMinAgeMonthsKey);
            Between(model.HorseGestationDays, 300, 400, HorseGestationDaysKey);
            Percent(model.HorseLatePregnancyPercent, HorseLatePregnancyPercentKey);
            Between(model.HorsePostFoalingDays, 0, 60, HorsePostFoalingDaysKey);

            Between(model.DeliveryFreeWaitingHours, 0, 72, DeliveryFreeWaitingHoursKey);
            NonNegative(model.DeliveryWaitingFeePerHourUsd, DeliveryWaitingFeePerHourUsdKey);
            Between(model.DeliveryMaxWaitingHours, 1, 168, DeliveryMaxWaitingHoursKey);
            Between(model.DeliveryDecisionHours, 1, 168, DeliveryDecisionHoursKey);

            if (errors.Count > 0)
            {
                throw new BusinessException(string.Join(" ", errors));
            }
            model.PolicyVersion = version;
        }

        private async Task<string?> GetUserNameAsync(int? userId)
        {
            if (!userId.HasValue)
            {
                return null;
            }
            return await _userRepo.GetQueryable().AsNoTracking()
                .Where(u => u.UserId == userId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
        }
    }
}
