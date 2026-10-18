using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // Đọc / ghi cấu hình trong common.system_settings. Thiếu khóa thì dùng giá trị mặc định của PaymentSettingsModel.
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
    }
}
