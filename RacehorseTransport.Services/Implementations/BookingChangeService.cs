using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // R-10 (BE-06, Chính sách mục 2): thay đổi sau khi gửi đơn.
    //   Khách đổi điểm đón / trả (chưa trả tiền): tính lại khoảng giá; tuyến / báo giá / hợp đồng nháp cũ bỏ đi, đơn về PreApproved để lập tuyến lại
    //   -> duyệt tuyến mới -> báo giá chốt mới (Quoted). Đã cọc thì liên hệ công ty (hủy theo chính sách rồi đặt đơn mới).
    //   Công ty đổi tuyến thêm nước mới sau khi khách đã cọc: khách được hủy và hoàn 100% tới route_change_cancel_until.
    public class BookingChangeService : IBookingChangeService
    {
        private static readonly string[] LocationEditableStatuses =
        {
            BookingStatuses.Submitted, BookingStatuses.PreApproved, BookingStatuses.RoutePlanned,
            BookingStatuses.Quoted, BookingStatuses.AwaitingDeposit
        };
        private static readonly string[] RequoteStatuses =
        {
            BookingStatuses.RoutePlanned, BookingStatuses.Quoted, BookingStatuses.AwaitingDeposit
        };
        private const string ReplanReason = "The customer changed the pickup / drop-off; please plan the route again.";

        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<RoutePlan> _planRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IPricingService _pricingService;
        private readonly IPaymentService _paymentService;
        private readonly IContractService _contractService;
        private readonly IFinalQuoteIssuer _finalQuoteIssuer;
        private readonly IBookingService _bookingService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public BookingChangeService(
            IRepository<BookingRequest> bookingRepo,
            IRepository<RoutePlan> planRepo,
            IRepository<TripHorse> tripHorseRepo,
            IPricingService pricingService,
            IPaymentService paymentService,
            IContractService contractService,
            IFinalQuoteIssuer finalQuoteIssuer,
            IBookingService bookingService,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _bookingRepo = bookingRepo;
            _planRepo = planRepo;
            _tripHorseRepo = tripHorseRepo;
            _pricingService = pricingService;
            _paymentService = paymentService;
            _contractService = contractService;
            _finalQuoteIssuer = finalQuoteIssuer;
            _bookingService = bookingService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        // =============================================================================================
        // Khách đổi điểm đón / trả
        // =============================================================================================

        public async Task<BookingDetailModel> ChangeLocationsAsync(int bookingId, ChangeLocationsModel model, CurrentUser currentUser)
        {
            var booking = await GetOwnTrackedBookingAsync(bookingId, currentUser);
            if (!LocationEditableStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"The pickup / drop-off can no longer be changed online (current status: {booking.Status}). Please contact the logistics team.");
            }
            if (booking.Status == BookingStatuses.AwaitingDeposit && booking.Payments.Any(p => p.Status == PaymentStatuses.Succeeded && PaymentTypes.Charges.Contains(p.PaymentType)))
            {
                throw new BusinessException("A payment was already made for this booking. Please contact the logistics team to change the pickup / drop-off.");
            }

            // Khoảng giá mới theo điểm mới (giữ nguyên lịch khách đã chọn)
            var quote = await _pricingService.CalculateQuoteRangeAsync(new QuoteInputModel
            {
                TransportMode = booking.TransportMode,
                PickupCountryCode = model.PickupCountryCode,
                PickupAddress = model.PickupAddress,
                DropoffCountryCode = model.DropoffCountryCode,
                DropoffAddress = model.DropoffAddress,
                PickupLat = model.PickupLat,
                PickupLng = model.PickupLng,
                DropoffLat = model.DropoffLat,
                DropoffLng = model.DropoffLng,
                RequiresClimateControl = booking.RequiresClimateControl,
                DeclaredValue = booking.DeclaredValue,
                PickupWindowStart = booking.PickupWindowStart,
                PickupWindowEnd = booking.PickupWindowEnd,
                DeliveryDeadline = booking.DeliveryDeadline,
                QuotedAt = booking.CreatedAt,
                StallClasses = booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled).Select(bh => bh.StallClass).ToList(),
                SkipScheduleCheck = true,
                ForceExpress = booking.IsExpress
            });

            string previousStatus = booking.Status;
            bool requote = RequoteStatuses.Contains(previousStatus);
            // Đường bay không lập tuyến: báo giá lại ngay theo điểm mới thay vì chờ điều phối
            bool airRequote = requote && QuoteRules.QuotesOnPreApproval(booking.TransportMode);
            var plans = await _planRepo.GetQueryable()
                .Where(p => p.BookingId == bookingId
                    && (p.Status == RoutePlanStatuses.Approved || p.Status == RoutePlanStatuses.Submitted || p.Status == RoutePlanStatuses.Draft))
                .ToListAsync();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                booking.PickupAddress = model.PickupAddress.Trim();
                booking.PickupCountryCode = model.PickupCountryCode;
                booking.PickupLat = model.PickupLat;
                booking.PickupLng = model.PickupLng;
                booking.DropoffAddress = model.DropoffAddress.Trim();
                booking.DropoffCountryCode = model.DropoffCountryCode;
                booking.DropoffLat = model.DropoffLat;
                booking.DropoffLng = model.DropoffLng;
                booking.QuoteMin = quote.QuoteMin;
                booking.QuoteMax = quote.QuoteMax;
                booking.DistanceKm = quote.DistanceKm ?? booking.DistanceKm;
                booking.EstimatedTransitHours = quote.EstimatedHours;
                booking.QuoteBreakdown = _pricingService.SerializeLines(quote.Lines);

                foreach (var row in booking.TransitCountries.ToList())
                {
                    booking.TransitCountries.Remove(row);
                }
                await _bookingRepo.SaveChangesAsync();
                for (int i = 0; i < quote.TransitCountryCodes.Count; i++)
                {
                    booking.TransitCountries.Add(new BookingTransitCountry { SequenceOrder = i + 1, CountryCode = quote.TransitCountryCodes[i] });
                }

                // Tuyến cũ không còn đúng: bản đã duyệt -> Superseded, bản nháp / đang chờ duyệt -> Rejected (điều phối lưu lại theo điểm mới rồi trình)
                foreach (var plan in plans)
                {
                    if (plan.Status == RoutePlanStatuses.Approved)
                    {
                        plan.Status = RoutePlanStatuses.Superseded;
                    }
                    else
                    {
                        plan.Status = RoutePlanStatuses.Rejected;
                        plan.RejectionReason = ReplanReason;
                    }
                    plan.UpdatedAt = DateTimeOffset.UtcNow;
                }

                if (requote)
                {
                    // Báo giá chốt cũ hết giá trị; đơn chờ lập tuyến lại rồi báo giá mới
                    booking.Status = BookingStatuses.PreApproved;
                    booking.EstimatedCost = 0;
                    booking.QuotedAt = null;
                    booking.QuoteExpiresAt = null;
                    booking.QuoteReminderSentAt = null;
                    booking.QuoteAcceptedAt = null;
                    booking.DepositPercent = null;
                    booking.DepositAmount = 0;
                    await _contractService.DiscardDraftsAsync(bookingId);
                }
                await _bookingRepo.SaveChangesAsync();

                if (airRequote)
                {
                    await _finalQuoteIssuer.IssueAsync(bookingId, null);
                    await _notificationService.SendToRoleAsync(
                        UserRoles.LogisticsManager,
                        $"Booking {booking.BookingCode}: pickup / drop-off changed",
                        $"The customer changed the pickup / drop-off to {booking.PickupAddress} ({booking.PickupCountryCode}) → {booking.DropoffAddress} ({booking.DropoffCountryCode}). A new final quote of {booking.EstimatedCost:N2} {booking.CurrencyCode} was sent.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId);
                }
                else if (previousStatus != BookingStatuses.Submitted)
                {
                    string message = $"The customer changed the pickup / drop-off to {booking.PickupAddress} ({booking.PickupCountryCode}) → {booking.DropoffAddress} ({booking.DropoffCountryCode}). "
                        + (requote ? "The previous quote is withdrawn: please plan the route again." : "Please plan the route with the new points.");
                    await _notificationService.SendToRoleAsync(UserRoles.FleetCoordinator, $"Booking {booking.BookingCode}: pickup / drop-off changed", message, NotificationReferenceTypes.Booking, booking.BookingId);
                    await _notificationService.SendToRoleAsync(UserRoles.LogisticsManager, $"Booking {booking.BookingCode}: pickup / drop-off changed", message, NotificationReferenceTypes.Booking, booking.BookingId);
                }
            });

            return await _bookingService.GetBookingByIdAsync(bookingId, currentUser);
        }

        // =============================================================================================
        // Khách hủy vì công ty đổi tuyến (hoàn 100%)
        // =============================================================================================

        public async Task<BookingDetailModel> CancelForRouteChangeAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await GetOwnTrackedBookingAsync(bookingId, currentUser);
            var now = DateTimeOffset.UtcNow;
            if (booking.RouteChangeCancelUntil == null || now > booking.RouteChangeCancelUntil.Value
                || (booking.Status != BookingStatuses.Approved && booking.Status != BookingStatuses.Assigned))
            {
                throw new BusinessException("This booking has no company route change that allows a free cancellation. Use POST /api/bookings/{id}/cancel instead.");
            }

            var horseIds = booking.BookingHorses.Select(bh => bh.BookingHorseId).ToList();
            string? tripCode = await _tripHorseRepo.GetQueryable()
                .Where(th => horseIds.Contains(th.BookingHorseId)
                    && th.Trip!.OverallStatus != TripStatuses.Cancelled
                    && TripHorseStatuses.Holding.Contains(th.Status))
                .Select(th => th.Trip!.TripCode)
                .FirstOrDefaultAsync();
            if (tripCode != null)
            {
                throw new BusinessException($"Horses of this booking are already planned on trip {tripCode}. Please contact the logistics team to cancel; the refund is still 100%.");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                booking.Status = BookingStatuses.Cancelled;
                booking.RouteChangeCancelUntil = null;
                foreach (var horse in booking.BookingHorses.Where(bh => bh.Status != BookingHorseStatuses.Cancelled))
                {
                    horse.Status = BookingHorseStatuses.Cancelled;
                }
                await _bookingRepo.UpdateAsync(booking);

                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Booking {booking.BookingCode} cancelled after a route change",
                    "The customer cancelled because the new route needs documents they cannot provide. Everything paid is refunded (100%).",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId);
            });

            await _paymentService.RefundForCancellationAsync(booking, currentUser, fullRefund: true);
            return await _bookingService.GetBookingByIdAsync(bookingId, currentUser);
        }

        // ---------------------------------------------------------------------------------------------

        private async Task<BookingRequest> GetOwnTrackedBookingAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await _bookingRepo.GetQueryable()
                .Include(b => b.BookingHorses)
                .Include(b => b.TransitCountries)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId)
                ?? throw new NotFoundException($"Booking with ID {bookingId} was not found.");
            if (booking.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only change your own bookings.");
            }
            return booking;
        }
    }
}
