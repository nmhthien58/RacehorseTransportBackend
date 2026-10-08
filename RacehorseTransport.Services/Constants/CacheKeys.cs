namespace RacehorseTransport.Services.Constants
{
    // Gom mọi cache key về một chỗ để không trùng và dễ xóa đúng key khi dữ liệu đổi.
    public static class CacheKeys
    {
        public const string DocumentTypesAll = "master:document-types:all";

        // Bảng giá đang hiệu lực, dùng cho báo giá. Mọi thao tác thêm/sửa/bật/tắt price_items phải xóa khóa này.
        public const string PriceItemsAll = "master:price-items:active";

        // Màn hình theo dõi của một đơn. Xóa qua ITrackingService.InvalidateTripAsync mỗi khi hành trình thay đổi.
        public static string BookingTracking(int bookingId) => $"tracking:booking:{bookingId}";

        // Khóa công khai Google dùng để ký ID token (đăng nhập bằng Google)
        public const string GoogleSigningKeys = "google:signing-keys";

        // Access token ngắn hạn để gọi Gmail API
        public const string GmailAccessToken = "google:gmail-access-token";

        // Link xem có thời hạn của một file đã lưu
        public static string FileViewUrl(string path) => $"files:view-url:{path}";
    }
}
