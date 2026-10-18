using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // Quy tắc của một lộ trình hoàn chỉnh khi trình / duyệt kế hoạch (R-04 ghép nhiều đơn). Hàm thuần để dễ viết unit test.
    //   - Bắt đầu bằng Origin, kết thúc bằng Destination, giờ dự kiến tăng dần, nằm trong thời gian kế hoạch của chuyến.
    //   - Mốc PickUp / DropOff gắn với một đơn có ngựa trên chuyến; mỗi đơn tối đa một mốc đón và một mốc trả, đón trước trả.
    //   - Đơn không có PickUp lên xe ở Origin; không có DropOff thì giao ở Destination.
    //   - Giờ đón của đơn nằm trong khoảng pickup (ngày cuối tính trọn ngày), giờ tới mốc trả không trễ hạn giao.
    public static class TripRouteRules
    {
        public static void Validate(
            IList<RouteCheckpoint> route,
            IReadOnlyCollection<RouteBookingRule> bookings,
            DateTimeOffset plannedStart,
            DateTimeOffset plannedEnd)
        {
            if (route.Count < 2)
            {
                throw new BusinessException("The route needs at least an origin and a destination checkpoint.");
            }
            if (route[0].CheckpointType != CheckpointTypes.Origin)
            {
                throw new BusinessException("The first checkpoint must be of type Origin.");
            }
            if (route[^1].CheckpointType != CheckpointTypes.Destination)
            {
                throw new BusinessException("The last checkpoint must be of type Destination.");
            }
            if (route.Skip(1).Any(c => c.CheckpointType == CheckpointTypes.Origin)
                || route.Take(route.Count - 1).Any(c => c.CheckpointType == CheckpointTypes.Destination))
            {
                throw new BusinessException("Only the first checkpoint can be an Origin and only the last can be a Destination.");
            }

            for (int i = 1; i < route.Count; i++)
            {
                if (route[i].PlannedTime <= route[i - 1].PlannedTime)
                {
                    throw new BusinessException($"Checkpoint '{route[i].CheckpointName}' must be planned after '{route[i - 1].CheckpointName}'.");
                }
            }

            if (route[0].PlannedTime < plannedStart || route[^1].PlannedTime > plannedEnd)
            {
                throw new BusinessException("Checkpoint times must fall between the trip's planned start and end.");
            }

            var byId = bookings.ToDictionary(b => b.BookingId);

            foreach (var checkpoint in route)
            {
                bool isBookingStop = CheckpointTypes.BookingStops.Contains(checkpoint.CheckpointType);
                if (isBookingStop && checkpoint.BookingId == null)
                {
                    throw new BusinessException($"Checkpoint '{checkpoint.CheckpointName}' ({checkpoint.CheckpointType}) must say which booking is picked up / dropped off there.");
                }
                if (!isBookingStop && checkpoint.BookingId != null)
                {
                    throw new BusinessException($"Only PickUp and DropOff checkpoints can be linked to a booking ('{checkpoint.CheckpointName}' is {checkpoint.CheckpointType}).");
                }
                if (checkpoint.BookingId != null && !byId.ContainsKey(checkpoint.BookingId.Value))
                {
                    throw new BusinessException($"Checkpoint '{checkpoint.CheckpointName}' is linked to booking ID {checkpoint.BookingId}, which has no horses on this trip.");
                }
            }

            foreach (var booking in bookings)
            {
                var pickups = route.Where(c => c.CheckpointType == CheckpointTypes.PickUp && c.BookingId == booking.BookingId).ToList();
                var dropoffs = route.Where(c => c.CheckpointType == CheckpointTypes.DropOff && c.BookingId == booking.BookingId).ToList();

                if (pickups.Count > 1 || dropoffs.Count > 1)
                {
                    throw new BusinessException($"Booking {booking.BookingCode} can have at most one PickUp and one DropOff checkpoint.");
                }

                var pickup = pickups.FirstOrDefault() ?? route[0];
                var dropoff = dropoffs.FirstOrDefault() ?? route[^1];

                if (route.IndexOf(pickup) >= route.IndexOf(dropoff))
                {
                    throw new BusinessException($"Booking {booking.BookingCode} must be picked up before it is dropped off.");
                }

                if (pickup.PlannedTime < booking.PickupWindowStart || pickup.PlannedTime >= booking.PickupWindowEnd.AddDays(1))
                {
                    throw new BusinessException(
                        $"Booking {booking.BookingCode} can only be picked up between {booking.PickupWindowStart:yyyy-MM-dd} and {booking.PickupWindowEnd:yyyy-MM-dd}; it is planned at '{pickup.CheckpointName}' on {pickup.PlannedTime:yyyy-MM-dd HH:mm zzz}.");
                }

                if (dropoff.PlannedTime > booking.DeliveryDeadline)
                {
                    throw new BusinessException(
                        $"Booking {booking.BookingCode} must be delivered by {booking.DeliveryDeadline:yyyy-MM-dd HH:mm zzz}; it reaches '{dropoff.CheckpointName}' at {dropoff.PlannedTime:yyyy-MM-dd HH:mm zzz}.");
                }
            }
        }

        // Mốc nơi đơn được trả: DropOff của đơn, nếu không có thì mốc Destination
        public static RouteCheckpoint? DropoffCheckpoint(IEnumerable<RouteCheckpoint> activeRoute, int bookingId)
        {
            var route = activeRoute.ToList();
            return route.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.DropOff && c.BookingId == bookingId)
                ?? route.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.Destination);
        }

        // Mốc nơi đơn được đón: PickUp của đơn, nếu không có thì mốc Origin
        public static RouteCheckpoint? PickupCheckpoint(IEnumerable<RouteCheckpoint> activeRoute, int bookingId)
        {
            var route = activeRoute.ToList();
            return route.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.PickUp && c.BookingId == bookingId)
                ?? route.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.Origin);
        }
    }
}
