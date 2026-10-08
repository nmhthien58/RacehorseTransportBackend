using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // Quy tắc chốt KPI khi đóng chuyến (docs/TASKS.md, phụ lục T-26). Hàm thuần, không đụng DB, để dễ viết unit test.
    public static class TripKpiCalculator
    {
        public const int GraceMinutes = 30;       // trễ tới 30 phút vẫn tính đúng giờ
        public const int PenaltyStepMinutes = 30; // mỗi 30 phút trễ
        public const decimal PenaltyPerStep = 5m; // trừ 5 điểm
        public const decimal MaxScore = 100m;
        public const decimal MinScore = 50m;

        // hasForceMajeure: chuyến có sự cố đã được duyệt phương án (Approved) hoặc đã xử lý xong (Resolved)
        public static TripKpiResult Calculate(DateTimeOffset plannedEnd, DateTimeOffset actualEnd, bool hasForceMajeure)
        {
            int delayMinutes = Math.Max(0, (int)Math.Floor((actualEnd - plannedEnd).TotalMinutes));

            if (delayMinutes <= GraceMinutes)
            {
                return new TripKpiResult { DelayMinutes = delayMinutes, OnTimeStatus = OnTimeStatuses.OnTime, KpiScore = MaxScore };
            }

            if (hasForceMajeure)
            {
                return new TripKpiResult { DelayMinutes = delayMinutes, OnTimeStatus = OnTimeStatuses.DelayedForceMajeure, KpiScore = MaxScore };
            }

            decimal score = MaxScore - (delayMinutes / PenaltyStepMinutes) * PenaltyPerStep; // chia nguyên: 31 phút trừ 5, 60 phút trừ 10
            return new TripKpiResult
            {
                DelayMinutes = delayMinutes,
                OnTimeStatus = OnTimeStatuses.DelayedOperationalFault,
                KpiScore = Math.Max(MinScore, score)
            };
        }
    }
}
