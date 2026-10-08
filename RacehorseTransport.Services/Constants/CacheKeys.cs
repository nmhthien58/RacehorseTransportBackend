namespace RacehorseTransport.Services.Constants
{
    // Gom mọi cache key về một chỗ để không trùng và dễ xóa đúng key khi dữ liệu đổi.
    public static class CacheKeys
    {
        public const string DocumentTypesAll = "master:document-types:all";

        public static string RefreshToken(string tokenHash) => $"auth:refresh:{tokenHash}";
    }
}
