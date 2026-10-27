namespace RacehorseTransport.Services.Implementations
{
    // R-10 (BE-06, Chính sách mục 6): hạn nộp giấy tính từ lúc cọc, nhắc khi còn 3 ngày và 1 ngày, quá hạn còn thiếu thì hủy.
    // Hàm thuần để dễ viết unit test.
    public static class DocumentDeadlineRules
    {
        public enum Action { None, FirstReminder, SecondReminder, Expire }

        // Số "lần nhắc" đặc biệt: đã quá hạn nhưng không tự hủy được (ngựa đã lên chuyến) và đã báo Manager -> không xử lý nữa
        public const int OverdueHandled = 3;

        public static DateTimeOffset DueAt(DateTimeOffset from, int submitDays) => from.AddDays(submitDays);

        public static Action Decide(DateTimeOffset dueAt, int remindersSent, DateTimeOffset now)
        {
            if (remindersSent >= OverdueHandled)
            {
                return Action.None;
            }
            if (now >= dueAt)
            {
                return Action.Expire;
            }
            var remaining = dueAt - now;
            if (remaining <= TimeSpan.FromDays(1) && remindersSent < 2)
            {
                return Action.SecondReminder;
            }
            if (remaining <= TimeSpan.FromDays(3) && remindersSent < 1)
            {
                return Action.FirstReminder;
            }
            return Action.None;
        }

        // Công ty đổi tuyến thêm nước mới: khách có thêm submitDays ngày kể từ bây giờ (không rút ngắn hạn đang có)
        public static DateTimeOffset ExtendedDueAt(DateTimeOffset? currentDueAt, DateTimeOffset now, int submitDays)
        {
            var extended = now.AddDays(submitDays);
            return currentDueAt.HasValue && currentDueAt.Value > extended ? currentDueAt.Value : extended;
        }

        // Nước quá cảnh mới so với tuyến cũ (giữ thứ tự của tuyến mới)
        public static List<string> NewCountries(IEnumerable<string> oldTransit, IEnumerable<string> newTransit)
        {
            var old = new HashSet<string>(oldTransit, StringComparer.OrdinalIgnoreCase);
            return newTransit.Where(c => !old.Contains(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
