using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;

namespace RacehorseTransport.Services.Implementations
{
    // R-14 (BE-14b, Chính sách mục 9): quay về nơi đi, hoàn tiền theo bên có lỗi, phí chuồng tạm. Hàm thuần để dễ viết unit test.
    public static class ReturnRouteRules
    {
        public const string ReturnSuffix = " (quay về)";

        public sealed record ReturnStop(string Name, string CheckpointType, string Address, decimal? Latitude, decimal? Longitude,
            decimal? DistanceKmFromPrevious, DateTimeOffset PlannedTime, int MandatoryRestMinutes);

        public sealed record ReturnPlan(List<RouteCheckpoint> Deactivate, List<ReturnStop> NewStops, RouteCheckpoint TurnAround);

        // Tuyến về = các mốc đã đi qua theo chiều ngược lại, kết thúc ở điểm xuất phát (thành điểm giao cuối, e-POD ký ở đó).
        // Mọi mốc chưa tới (kể cả điểm đón / trả / đích cũ) bị bỏ. Giờ dự kiến lấy theo khoảng cách giờ của chiều đi.
        public static ReturnPlan Build(IReadOnlyList<RouteCheckpoint> activeRoute, DateTimeOffset now)
        {
            var route = activeRoute.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
            var reached = route.Where(c => c.ActualArrivalTime != null).ToList();
            if (reached.Count == 0)
            {
                throw new Services.Exceptions.BusinessException("The trip has not left its origin yet; cancel the trip instead of returning.");
            }

            var turnAround = reached[^1];
            bool standingThere = turnAround.ActualDepartureTime == null;
            // Đang đứng ở mốc cuối đã tới thì đi ngược từ mốc trước nó; đã rời mốc đó thì phải quay lại qua chính mốc đó
            var backwards = Enumerable.Reverse(reached).Skip(standingThere ? 1 : 0).ToList();
            if (backwards.Count == 0)
            {
                throw new Services.Exceptions.BusinessException("The vehicle is still at the origin; no return route is needed.");
            }

            var stops = new List<ReturnStop>();
            var time = now;
            var from = turnAround;
            if (!standingThere)
            {
                // Xe đang chạy giữa hai mốc: thời gian quay lại mốc vừa rời ≈ thời gian đã chạy từ lúc rời
                time = now + (now - turnAround.ActualDepartureTime!.Value);
            }
            for (int i = 0; i < backwards.Count; i++)
            {
                var stop = backwards[i];
                if (i > 0 || standingThere)
                {
                    time += (from.PlannedTime - stop.PlannedTime).Duration();
                }
                bool last = i == backwards.Count - 1;
                stops.Add(new ReturnStop(
                    stop.CheckpointName + ReturnSuffix,
                    last ? CheckpointTypes.Destination : stop.CheckpointType == CheckpointTypes.BorderGate ? CheckpointTypes.BorderGate : CheckpointTypes.RestStop,
                    stop.Address,
                    stop.Latitude,
                    stop.Longitude,
                    // Chặng về A -> B (B liền trước A ở chiều đi) dài bằng chặng đi B -> A; quay lại mốc vừa rời thì chưa biết
                    i == 0 && !standingThere ? null : from.DistanceKmFromPrevious,
                    time,
                    last ? 0 : stop.MandatoryRestMinutes));
                time = time.AddMinutes(last ? 0 : stop.MandatoryRestMinutes);
                from = stop;
            }

            var deactivate = route.Where(c => c.ActualArrivalTime == null && c.Status == CheckpointStatuses.Pending).ToList();
            return new ReturnPlan(deactivate, stops, turnAround);
        }

        // Tỉ lệ quãng đường chưa đi của một đơn khi quay đầu: (quãng từ chỗ quay đầu tới điểm giao) / (quãng từ điểm đón tới điểm giao).
        // Thiếu số km thì tính theo giờ dự kiến. Chưa được đón thì chưa đi gì (1).
        public static decimal UnusedRatio(IReadOnlyList<RouteCheckpoint> route, RouteCheckpoint? pickup, RouteCheckpoint? dropoff, RouteCheckpoint turnAround)
        {
            var ordered = route.OrderBy(c => c.SequenceOrder).ToList();
            int p = pickup == null ? 0 : ordered.FindIndex(c => c.CheckpointId == pickup.CheckpointId);
            int d = dropoff == null ? ordered.Count - 1 : ordered.FindIndex(c => c.CheckpointId == dropoff.CheckpointId);
            int t = ordered.FindIndex(c => c.CheckpointId == turnAround.CheckpointId);
            if (p < 0 || d < 0 || t < 0 || d <= p)
            {
                return 1m;
            }
            if (t <= p)
            {
                return 1m;
            }
            if (t >= d)
            {
                return 0m;
            }

            bool haveKm = ordered.Skip(p + 1).Take(d - p).All(c => c.DistanceKmFromPrevious.HasValue);
            decimal Span(int from, int to) => haveKm
                ? ordered.Skip(from + 1).Take(to - from).Sum(c => c.DistanceKmFromPrevious!.Value)
                : (decimal)(ordered[to].PlannedTime - ordered[from].PlannedTime).TotalMinutes;

            decimal total = Span(p, d);
            return total <= 0 ? 1m : Math.Clamp(Math.Round(Span(t, d) / total, 4), 0m, 1m);
        }

        // Tiền hoàn khi quay về (bảng 9.1): lỗi công ty hoàn 100% đã trả; lỗi khách không hoàn;
        // bất khả kháng hoàn tiền cước × tỉ lệ quãng đường chưa đi (phí thủ tục, bảo hiểm không nằm trong tiền cước nên không hoàn)
        public static decimal RefundAmount(string faultParty, decimal freight, decimal refundable, decimal unusedRatio)
        {
            decimal amount = faultParty switch
            {
                FaultParties.Company => refundable,
                FaultParties.ForceMajeure => Math.Round(freight * unusedRatio, 2, MidpointRounding.AwayFromZero),
                _ => 0m
            };
            return Math.Max(0, Math.Min(amount, refundable));
        }

        // Một con ngựa rời chuyến giữa đường (BE-11): hoàn phần cước chưa đi của con đó (bảng 9.1, cột "tiền cước phần chưa đi").
        // Lỗi công ty và bất khả kháng đều hoàn theo tỉ lệ chưa đi (phí thủ tục không nằm trong tiền cước); lỗi khách không hoàn.
        // Phần cước của một con = tiền cước của đơn / số ngựa của đơn.
        public static decimal OffloadRefundAmount(string faultParty, decimal bookingFreight, int horsesInBooking, decimal unusedRatio, decimal refundable)
        {
            if (faultParty == FaultParties.Customer || horsesInBooking <= 0)
            {
                return 0m;
            }
            decimal amount = Math.Round(bookingFreight / horsesInBooking * unusedRatio, 2, MidpointRounding.AwayFromZero);
            return Math.Max(0, Math.Min(amount, refundable));
        }

        // Phí chuồng tạm: số ngày (chưa đủ ngày tính tròn một ngày, tối thiểu 1) × số ngựa × phí / con / ngày
        public static decimal StableFee(DateTimeOffset from, DateTimeOffset to, int horses, decimal feePerHorsePerDay)
        {
            if (horses <= 0 || feePerHorsePerDay <= 0)
            {
                return 0m;
            }
            int days = Math.Max(1, (int)Math.Ceiling((to - from).TotalHours / 24));
            return days * horses * feePerHorsePerDay;
        }
    }
}
