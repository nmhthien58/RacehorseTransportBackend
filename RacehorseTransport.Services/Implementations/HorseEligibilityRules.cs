using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // R-12: ngựa có được chở không (Chính sách dịch vụ mục 5). Hàm thuần để dễ viết unit test.
    //   - Tuổi tính tới NGÀY CUỐI của khoảng đón >= Horse.MinAgeMonths (theo chính sách).
    //   - Ngựa cái mang thai: ngày cuối của khoảng đón phải trước (ngày dự sinh - GestationDays × (100% - LatePregnancyPercent))
    //     (mặc định 340 × 10% = 34 ngày). Dùng ngày cuối vì càng về sau càng gần ngày đẻ.
    //   - Ngựa cái vừa đẻ: NGÀY ĐẦU của khoảng đón phải cách lần đẻ gần nhất ít nhất PostFoalingDays ngày,
    //     để ngày đón nào trong khoảng cũng đã qua thời gian hồi phục.
    // Ngày so theo lịch của khách (phần ngày của PickupWindowStart / End với múi giờ khách gửi lên).
    public static class HorseEligibilityRules
    {
        // Số ngày trước ngày dự sinh không được chở (làm tròn lên)
        public static int LatePregnancyDays(PolicySettingsModel settings) =>
            (int)Math.Ceiling(settings.HorseGestationDays * (100 - settings.HorseLatePregnancyPercent) / 100m);

        // Các lý do con ngựa không được chở trong khoảng đón này; rỗng = được chở
        public static List<string> Problems(Horse horse, DateOnly pickupFirstDay, DateOnly pickupLastDay, PolicySettingsModel settings)
        {
            var problems = new List<string>();

            if (horse.DateOfBirth.AddMonths(settings.HorseMinAgeMonths) > pickupLastDay)
            {
                problems.Add($"it will be younger than {settings.HorseMinAgeMonths} months on {pickupLastDay:yyyy-MM-dd} (born {horse.DateOfBirth:yyyy-MM-dd})");
            }

            if (horse.IsPregnant && horse.ExpectedFoalingDate.HasValue)
            {
                var lastAllowed = horse.ExpectedFoalingDate.Value.AddDays(-LatePregnancyDays(settings));
                if (pickupLastDay >= lastAllowed)
                {
                    problems.Add($"it is due to foal on {horse.ExpectedFoalingDate:yyyy-MM-dd}; a pregnant mare can only travel if the pickup window ends before {lastAllowed:yyyy-MM-dd}");
                }
            }

            if (horse.LastFoalingDate.HasValue)
            {
                var firstAllowed = horse.LastFoalingDate.Value.AddDays(settings.HorsePostFoalingDays);
                if (pickupFirstDay < firstAllowed && pickupLastDay >= horse.LastFoalingDate.Value)
                {
                    problems.Add($"it foaled on {horse.LastFoalingDate:yyyy-MM-dd}; it can travel from {firstAllowed:yyyy-MM-dd}, so the pickup window must start on or after that date");
                }
            }

            return problems;
        }

        // Một thông báo lỗi liệt kê mọi con ngựa không đạt và lý do; null = tất cả đạt
        public static string? Describe(IEnumerable<Horse> horses, DateOnly pickupFirstDay, DateOnly pickupLastDay, PolicySettingsModel settings)
        {
            var lines = horses
                .Select(h => (h.Name, Problems: Problems(h, pickupFirstDay, pickupLastDay, settings)))
                .Where(x => x.Problems.Count > 0)
                .Select(x => $"{x.Name}: {string.Join("; ", x.Problems)}.")
                .ToList();

            return lines.Count == 0
                ? null
                : $"These horses cannot be transported in this pickup window: {string.Join(" ", lines)}";
        }
    }
}
