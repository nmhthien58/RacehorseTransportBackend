using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // Giai đoạn của đơn theo góc nhìn khách (R-07): gộp trạng thái đơn, thanh toán, hồ sơ và trạng thái ngựa trên chuyến
    // thành một bước duy nhất để khách theo dõi kiểu "đơn hàng". Hàm thuần để dễ viết unit test.
    // Đơn cần được nạp kèm BookingHorses -> Dossier, BookingHorses -> TripHorses -> Trip -> Checkpoints.
    //   Submitted / PreApproved / RoutePlanned (đang duyệt, lập tuyến) -> PendingReview, AwaitingDeposit -> AwaitingDeposit, Rejected / Cancelled / Expired -> Closed,
    //   AwaitingFinalPayment -> AwaitingPayment, Completed -> Completed (còn nợ phát sinh thì AwaitingPayment).
    //   Đơn Approved xét theo thứ tự (ngựa chưa giao):
    //     có con đang trên xe -> AwaitingHandover nếu xe đã tới điểm trả của đơn, không thì InTransit;
    //     có con nằm trên chuyến đã duyệt -> AwaitingPayment nếu chưa trả đủ phần còn lại, không thì Scheduled;
    //     còn hồ sơ chưa thông quan -> PreparingDossier; còn lại -> AwaitingTrip (chờ xếp chuyến).
    public static class CustomerStageRules
    {
        private static readonly string[] ScheduledTripStatuses =
        {
            TripStatuses.Scheduled, TripStatuses.InTransit, TripStatuses.EmergencyRerouting, TripStatuses.ArrivedDestination
        };

        public static string Resolve(BookingRequest booking, BookingPaymentSummaryModel payment)
        {
            switch (booking.Status)
            {
                case BookingStatuses.Submitted:
                case BookingStatuses.PreApproved:
                case BookingStatuses.RoutePlanned:
                    return CustomerStages.PendingReview;
                case BookingStatuses.Quoted:
                    return CustomerStages.QuoteReady;
                case BookingStatuses.AwaitingDeposit:
                    return CustomerStages.AwaitingDeposit;
                case BookingStatuses.Rejected:
                case BookingStatuses.Cancelled:
                case BookingStatuses.Expired:
                case BookingStatuses.QuoteExpired:
                    return CustomerStages.Closed;
                case BookingStatuses.AwaitingFinalPayment:
                    return CustomerStages.AwaitingPayment;
                case BookingStatuses.Completed:
                    return payment.ExtraAmount > payment.ExtraPaid ? CustomerStages.AwaitingPayment : CustomerStages.Completed;
            }

            // Approved (và trạng thái cũ Assigned): xét các con chưa giao
            var horses = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).ToList();
            var liveTripHorses = horses
                .SelectMany(bh => bh.TripHorses)
                .Where(th => th.Trip != null && th.Trip.OverallStatus != TripStatuses.Cancelled)
                .ToList();

            var onBoard = liveTripHorses.Where(th => th.Status == TripHorseStatuses.InTransit).ToList();
            if (onBoard.Count > 0)
            {
                return onBoard.Any(th => ArrivedAtDropoff(th.Trip!, booking.BookingId))
                    ? CustomerStages.AwaitingHandover
                    : CustomerStages.InTransit;
            }

            bool scheduled = liveTripHorses.Any(th => th.Status == TripHorseStatuses.Planned && ScheduledTripStatuses.Contains(th.Trip!.OverallStatus));
            if (scheduled)
            {
                bool balanceOpen = payment.PaymentRequired && payment.BalanceAmount > payment.BalancePaid;
                return balanceOpen ? CustomerStages.AwaitingPayment : CustomerStages.Scheduled;
            }

            // Con đã xong (giao, chết, đưa về nơi đi) không cần xét hồ sơ nữa
            var waiting = horses.Where(bh => bh.Status != BookingHorseStatuses.Deceased
                && !bh.TripHorses.Any(th => th.Trip?.OverallStatus != TripStatuses.Cancelled && OffloadRules.IsFinished(th))).ToList();
            if (waiting.Any(bh => bh.Dossier?.Status != DossierStatuses.Cleared))
            {
                return CustomerStages.PreparingDossier;
            }

            return CustomerStages.AwaitingTrip;
        }

        // Xe đang ở (hoặc đã qua) điểm trả của đơn: mốc DropOff riêng của đơn, không có thì mốc Destination
        private static bool ArrivedAtDropoff(Trip trip, int bookingId)
        {
            var route = trip.Checkpoints.Where(c => c.IsActive).ToList();
            var dropoff = route.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.DropOff && c.BookingId == bookingId)
                ?? route.FirstOrDefault(c => c.CheckpointType == CheckpointTypes.Destination);
            return dropoff?.ActualArrivalTime != null || trip.OverallStatus == TripStatuses.ArrivedDestination;
        }
    }
}
