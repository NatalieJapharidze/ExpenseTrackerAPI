using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Text.Json;

namespace ExpenseTrackerApi.Infrastructure.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan expiration);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
        Task RemoveAsync(string key);
    }
    public class CacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly IMemoryCache _memoryCache;

        public CacheService(IConnectionMultiplexer redis, IMemoryCache memoryCache)
        {
            _database = redis.GetDatabase();
            _memoryCache = memoryCache;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            if (_memoryCache.TryGetValue(key, out T cachedValue))
                return cachedValue;

            var redisValue = await _database.StringGetAsync(key);
            if (redisValue.HasValue)
            {
                var value = JsonSerializer.Deserialize<T>(redisValue);
                _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
                return value;
            }

            return default(T);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
            var serializedValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serializedValue, expiration);
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
        {
            var cached = await GetAsync<T>(key);
            if (cached != null)
                return cached;

            var value = await factory();
            await SetAsync(key, value, expiration);
            return value;
        }

        public async Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            await _database.KeyDeleteAsync(key);
        }
    }
}
