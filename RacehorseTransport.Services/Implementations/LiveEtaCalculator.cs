using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;

namespace RacehorseTransport.Services.Implementations
{
    // Giờ dự kiến cập nhật theo check-in thực tế (R-08, phần màn theo dõi). Hàm thuần để dễ viết unit test.
    //   - Độ trễ hiện tại = độ lệch so với kế hoạch ở mốc gần nhất xe đã chạm tới:
    //       đã rời mốc   -> giờ rời thực tế - (giờ dự kiến tới + thời gian nghỉ bắt buộc);
    //       đang dừng    -> giờ rời sớm nhất có thể (max(bây giờ, giờ tới + nghỉ bắt buộc)) - (giờ dự kiến tới + nghỉ);
    //       chưa xuất phát nhưng đã quá giờ khởi hành -> bây giờ - giờ dự kiến của mốc đầu.
    //   - Đang trên đường mà đã quá giờ (dự kiến + trễ) của mốc kế tiếp: coi như tới mốc đó ngay bây giờ.
    //   - Tới sớm không kéo ETA sớm lên (xe vẫn theo lịch), nên độ trễ không âm.
    //   - Mốc đã tới giữ giờ tới thực tế; mốc chưa tới = giờ dự kiến + độ trễ.
    public static class LiveEtaCalculator
    {
        public sealed class Result
        {
            public int DelayMinutes { get; init; }
            public Dictionary<int, DateTimeOffset> EstimatedArrivals { get; init; } = new(); // theo CheckpointId
        }

        private static readonly string[] OnTheRoadStatuses = { TripStatuses.InTransit, TripStatuses.EmergencyRerouting };

        public static Result Calculate(IReadOnlyList<RouteCheckpoint> route, string tripStatus, DateTimeOffset now)
        {
            var ordered = route.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
            double delay = 0;

            var lastReached = ordered.LastOrDefault(c => c.ActualArrivalTime != null);
            if (lastReached != null)
            {
                var plannedLeave = lastReached.PlannedTime.AddMinutes(lastReached.MandatoryRestMinutes);
                var actualLeave = lastReached.ActualDepartureTime
                    ?? Max(now, lastReached.ActualArrivalTime!.Value.AddMinutes(lastReached.MandatoryRestMinutes));
                // Mốc cuối không có giờ rời: độ trễ là độ trễ lúc tới
                delay = lastReached == ordered[^1]
                    ? (lastReached.ActualArrivalTime!.Value - lastReached.PlannedTime).TotalMinutes
                    : (actualLeave - plannedLeave).TotalMinutes;
            }
            else if (ordered.Count > 0 && (tripStatus == TripStatuses.Scheduled || OnTheRoadStatuses.Contains(tripStatus)))
            {
                delay = (now - ordered[0].PlannedTime).TotalMinutes;
            }
            delay = Math.Max(0, delay);

            // Đang chạy giữa hai mốc mà đã quá giờ dự kiến (kể cả trễ) của mốc kế tiếp
            var next = ordered.FirstOrDefault(c => c.ActualArrivalTime == null);
            if (next != null && lastReached?.ActualDepartureTime != null && OnTheRoadStatuses.Contains(tripStatus))
            {
                double overdue = (now - next.PlannedTime).TotalMinutes;
                delay = Math.Max(delay, overdue);
            }

            var result = new Result { DelayMinutes = (int)Math.Round(delay) };
            foreach (var checkpoint in ordered)
            {
                result.EstimatedArrivals[checkpoint.CheckpointId] = checkpoint.ActualArrivalTime
                    ?? checkpoint.PlannedTime.AddMinutes(Math.Round(delay));
            }
            return result;
        }

        private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
    }
}
