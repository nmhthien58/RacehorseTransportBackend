namespace RacehorseTransport.Services.Options
{
    // Đọc từ section "Booking" trong appsettings
    public class BookingOptions
    {
        public const string SectionName = "Booking";

        // Đơn Submitted quá số giờ này mà chưa được duyệt / từ chối thì tự chuyển sang Expired
        public int ReviewSlaHours { get; set; } = 48;

        // Chu kỳ quét đơn quá hạn của BookingExpiryJob
        public int ExpiryCheckMinutes { get; set; } = 10;
    }
}
