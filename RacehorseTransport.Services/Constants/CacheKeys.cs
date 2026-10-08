namespace RacehorseTransport.Services.Constants
{
    // Gom mọi cache key về một chỗ để không trùng và dễ xóa đúng key khi dữ liệu đổi.
    public static class CacheKeys
    {
        public const string DocumentTypesAll = "master:document-types:all";

        // Bảng giá đang hiệu lực, dùng cho báo giá. Mọi thao tác thêm/sửa/bật/tắt price_items phải xóa khóa này.
        public const string PriceItemsAll = "master:price-items:active";

        public static string RefreshToken(string tokenHash) => $"auth:refresh:{tokenHash}";

        // Màn hình theo dõi của một đơn. Xóa qua ITrackingService.InvalidateTripAsync mỗi khi hành trình thay đổi.
        public static string BookingTracking(int bookingId) => $"tracking:booking:{bookingId}";
    }
}
