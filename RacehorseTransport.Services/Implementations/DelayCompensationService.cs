using System.Globalization;
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
    // R-09: bồi thường giao trễ chờ Manager duyệt (Chính sách mục 7).
    //   AwaitingApproval -> Approved (hoàn tiền qua các dòng Refund) | Rejected
    public class DelayCompensationService : IDelayCompensationService
    {
        private readonly IRepository<DelayCompensation> _compensationRepo;
        private readonly IBookingService _bookingService;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;

        public DelayCompensationService(
            IRepository<DelayCompensation> compensationRepo,
            IBookingService bookingService,
            IPaymentService paymentService,
            INotificationService notificationService)
        {
            _compensationRepo = compensationRepo;
            _bookingService = bookingService;
            _paymentService = paymentService;
            _notificationService = notificationService;
        }

        public async Task<IReadOnlyList<DelayCompensationModel>> GetAllAsync(string? status, CurrentUser currentUser)
        {
            var query = Query();
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!DelayCompensationStatuses.All.Contains(status))
                {
                    throw new BusinessException($"Status '{status}' is invalid. Allowed values: {string.Join(", ", DelayCompensationStatuses.All)}.");
                }
                query = query.Where(c => c.Status == status);
            }

            var rows = await query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.DelayCompensationId).ToListAsync();
            return rows.Select(ToModel).ToList();
        }

        public async Task<IReadOnlyList<DelayCompensationModel>> GetForBookingAsync(int bookingId, CurrentUser currentUser)
        {
            // Dùng lại quy tắc "ai được xem đơn" của BookingService (404 / 403)
            await _bookingService.GetBookingByIdAsync(bookingId, currentUser);

            var rows = await Query().Where(c => c.BookingId == bookingId).OrderBy(c => c.CreatedAt).ToListAsync();
            return rows.Select(ToModel).ToList();
        }

        public async Task<DelayCompensationModel> ApproveAsync(int id, decimal amount, string? reason, CurrentUser currentUser)
        {
            var compensation = await GetAwaitingAsync(id);
            var booking = compensation.Booking!;

            if (amount <= 0)
            {
                throw new BusinessException("Amount must be greater than 0. To pay nothing, reject the compensation with a reason.");
            }
            if (amount != compensation.CalculatedAmount && string.IsNullOrWhiteSpace(reason))
            {
                throw new BusinessException($"Give a reason when the amount differs from the calculated {compensation.CalculatedAmount:N2} {compensation.CurrencyCode}.");
            }
            decimal refundable = await _paymentService.GetRefundableAmountAsync(booking.BookingId);
            if (amount > refundable)
            {
                throw new BusinessException($"Booking {booking.BookingCode} has only {refundable:N2} {compensation.CurrencyCode} that can still be refunded.");
            }

            // Chốt duyệt trước rồi mới gọi cổng hoàn tiền (không gọi cổng trong transaction)
            compensation.Status = DelayCompensationStatuses.Approved;
            compensation.ApprovedAmount = amount;
            compensation.ReviewReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            compensation.ReviewedByUserId = currentUser.UserId;
            compensation.ReviewedAt = DateTimeOffset.UtcNow;
            await _compensationRepo.UpdateAsync(compensation);

            await _paymentService.RefundAmountAsync(
                booking,
                amount,
                $"Delay compensation for trip {compensation.Trip!.TripCode} ({compensation.DelayHours:0.##} h late)",
                currentUser);

            await _notificationService.SendAsync(
                booking.CustomerUserId,
                $"Delay compensation for booking {booking.BookingCode}",
                $"Your horses arrived {compensation.DelayHours:0.##} hour(s) after the delivery deadline. We are refunding {amount:N2} {compensation.CurrencyCode} to your original payment method.",
                NotificationReferenceTypes.Booking,
                booking.BookingId);
            await _notificationService.EmailCustomerAsync(
                booking.CustomerUserId,
                EmailTemplateNames.DelayCompensation,
                CustomerPages.Booking(booking.BookingId),
                new Dictionary<string, string>
                {
                    ["BookingCode"] = booking.BookingCode,
                    ["DelayHours"] = compensation.DelayHours.ToString("0.##", CultureInfo.InvariantCulture),
                    ["Amount"] = $"{amount.ToString("N2", CultureInfo.InvariantCulture)} {compensation.CurrencyCode}"
                });

            return ToModel(await Query().FirstAsync(c => c.DelayCompensationId == id));
        }

        public async Task<DelayCompensationModel> RejectAsync(int id, string reason, CurrentUser currentUser)
        {
            var compensation = await GetAwaitingAsync(id);
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new BusinessException("Reason is required to reject a delay compensation.");
            }

            compensation.Status = DelayCompensationStatuses.Rejected;
            compensation.ReviewReason = reason.Trim();
            compensation.ReviewedByUserId = currentUser.UserId;
            compensation.ReviewedAt = DateTimeOffset.UtcNow;
            await _compensationRepo.UpdateAsync(compensation);

            return ToModel(await Query().FirstAsync(c => c.DelayCompensationId == id));
        }

        // ---------------------------------------------------------------------------------------------

        private IQueryable<DelayCompensation> Query()
        {
            return _compensationRepo.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Booking!).ThenInclude(b => b.Customer)
                .Include(c => c.Trip)
                .Include(c => c.ReviewedBy);
        }

        private async Task<DelayCompensation> GetAwaitingAsync(int id)
        {
            var compensation = await _compensationRepo.GetQueryable()
                .Include(c => c.Booking)
                .Include(c => c.Trip)
                .FirstOrDefaultAsync(c => c.DelayCompensationId == id)
                ?? throw new NotFoundException($"Delay compensation with ID {id} was not found.");

            if (compensation.Status != DelayCompensationStatuses.AwaitingApproval)
            {
                throw new BusinessException($"This delay compensation has already been reviewed (status: {compensation.Status}).");
            }
            return compensation;
        }

        private static DelayCompensationModel ToModel(DelayCompensation entity)
        {
            return new DelayCompensationModel
            {
                DelayCompensationId = entity.DelayCompensationId,
                BookingId = entity.BookingId,
                BookingCode = entity.Booking?.BookingCode ?? string.Empty,
                CustomerName = entity.Booking?.Customer?.FullName,
                TripId = entity.TripId,
                TripCode = entity.Trip?.TripCode ?? string.Empty,
                DelayHours = entity.DelayHours,
                FreightAmount = entity.FreightAmount,
                Percent = entity.Percent,
                CalculatedAmount = entity.CalculatedAmount,
                ApprovedAmount = entity.ApprovedAmount,
                CurrencyCode = entity.CurrencyCode,
                Status = entity.Status,
                ReviewReason = entity.ReviewReason,
                ReviewedByName = entity.ReviewedBy?.FullName,
                ReviewedAt = entity.ReviewedAt,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
