namespace RacehorseTransport.Services.Interfaces
{
    // Bọc IDistributedCache: chạy trên Redis nếu có cấu hình ConnectionStrings:Redis, không thì dùng bộ nhớ trong.
    // Cache lỗi (Redis tắt) không làm hỏng request: Get trả về null, Set/Remove bỏ qua.
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan ttl);
        Task RemoveAsync(string key);

        // Có trong cache thì trả về luôn; chưa có thì gọi factory, lưu lại rồi trả về.
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
    }
}
