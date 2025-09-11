using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace SharedKernel.Utility
{
    public static class IDistributedCacheExtensions
    {
        public static async Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options)
            where T : class, new()
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, serializedValue, options);
        }

        public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key)
            where T : class, new()
        {
            var serializedValue = await cache.GetStringAsync(key);
            if (serializedValue != null)
            {
                return JsonSerializer.Deserialize<T>(serializedValue);
            }
            return null;
        }
        public static async Task SetAsync(this IDistributedCache cache, string key, string value, DistributedCacheEntryOptions options)
        {
            await cache.SetAsync(key, Encoding.UTF8.GetBytes(value), options);
        }

        public static async Task<string?> GetAsync(this IDistributedCache cache, string key)
        {
            var data = await cache.GetAsync(key);
            if (data != null)
            {
                return Encoding.UTF8.GetString(data);
            }
            return null;
        }
    }
}

