using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    public class DistributedCacheService : ICacheService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IDistributedCache _cache;
        private readonly ILogger<DistributedCacheService> _logger;

        public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var json = await _cache.GetStringAsync(key);
                return json == null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache GET failed for key {Key}; treating as a miss.", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, JsonOptions);
                await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache SET failed for key {Key}.", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache REMOVE failed for key {Key}.", key);
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
        {
            var cached = await GetAsync<T>(key);
            if (cached != null)
            {
                return cached;
            }

            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, ttl);
            }
            return value;
        }
    }
}
