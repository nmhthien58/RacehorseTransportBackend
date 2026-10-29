using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // Trang "Giao dịch" của Manager: lọc giao dịch trên mọi đơn và tính số liệu tổng hợp.
    // Tách thành hàm tĩnh để test được bằng danh sách trong bộ nhớ (IQueryable của EF dịch được sang SQL).
    public static class PaymentListRules
    {
        // Ngày lọc là ngày theo giờ Việt Nam (manager làm việc theo giờ VN), cột created_at lưu UTC
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        // [from 00:00 giờ VN, (to + 1 ngày) 00:00 giờ VN): "to" tính trọn ngày
        public static (DateTimeOffset? FromUtc, DateTimeOffset? ToUtcExclusive) CreatedRange(DateOnly? from, DateOnly? to)
        {
            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                throw new BusinessException("'from' must be on or before 'to'.");
            }

            DateTimeOffset? start = from.HasValue
                ? new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), VietnamOffset).ToUniversalTime()
                : null;
            DateTimeOffset? end = to.HasValue
                ? new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), VietnamOffset).ToUniversalTime()
                : null;
            return (start, end);
        }

        public static IQueryable<Payment> ApplyCreatedRange(IQueryable<Payment> query, DateOnly? from, DateOnly? to)
        {
            var (start, end) = CreatedRange(from, to);
            if (start.HasValue)
            {
                var s = start.Value;
                query = query.Where(p => p.CreatedAt >= s);
            }
            if (end.HasValue)
            {
                var e = end.Value;
                query = query.Where(p => p.CreatedAt < e);
            }
            return query;
        }

        // Lọc theo loại, trạng thái, khoảng ngày tạo và tìm theo mã đơn / tên khách (cần Include Booking.Customer khi chạy trên EF)
        public static IQueryable<Payment> ApplyFilter(IQueryable<Payment> query, PaymentFilterOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.Type))
            {
                if (!PaymentTypes.All.Contains(options.Type))
                {
                    throw new BusinessException($"Type '{options.Type}' is invalid. Allowed values: {string.Join(", ", PaymentTypes.All)}.");
                }
                query = query.Where(p => p.PaymentType == options.Type);
            }

            if (!string.IsNullOrWhiteSpace(options.Status))
            {
                if (!PaymentStatuses.All.Contains(options.Status))
                {
                    throw new BusinessException($"Status '{options.Status}' is invalid. Allowed values: {string.Join(", ", PaymentStatuses.All)}.");
                }
                query = query.Where(p => p.Status == options.Status);
            }

            query = ApplyCreatedRange(query, options.From, options.To);

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                string search = options.Search.Trim().ToLower();
                query = query.Where(p => p.Booking!.BookingCode.ToLower().Contains(search)
                    || p.Booking.Customer!.FullName.ToLower().Contains(search));
            }

            return query;
        }

        // Tổng hợp: "thành công / đang chờ" chỉ tính các khoản THU của khách (cọc, phần còn lại, phát sinh);
        // tiền chi cho khách tách riêng ở refundedAmount để không cộng lẫn tiền vào và tiền ra.
        public static PaymentListSummaryModel Summarize(IEnumerable<Payment> payments)
        {
            var list = payments.ToList();
            var charges = list.Where(p => PaymentTypes.Charges.Contains(p.PaymentType)).ToList();
            var refunds = list.Where(p => p.PaymentType == PaymentTypes.Refund).ToList();

            return new PaymentListSummaryModel
            {
                SucceededCount = charges.Count(p => p.Status == PaymentStatuses.Succeeded),
                SucceededAmount = charges.Where(p => p.Status == PaymentStatuses.Succeeded).Sum(p => p.Amount),
                PendingCount = charges.Count(p => p.Status == PaymentStatuses.Pending),
                PendingAmount = charges.Where(p => p.Status == PaymentStatuses.Pending).Sum(p => p.Amount),
                FailedCount = list.Count(p => p.Status == PaymentStatuses.Failed),
                ExpiredCount = list.Count(p => p.Status == PaymentStatuses.Expired),
                RefundedAmount = refunds.Where(p => p.Status == PaymentStatuses.Succeeded).Sum(p => p.Amount),
                // Dòng hoàn thất bại / còn chờ: Manager phải thử lại hoặc đánh dấu đã hoàn thủ công
                RefundsNeedingActionCount = refunds.Count(p => p.Status == PaymentStatuses.Failed || p.Status == PaymentStatuses.Pending)
            };
        }
    }
}
