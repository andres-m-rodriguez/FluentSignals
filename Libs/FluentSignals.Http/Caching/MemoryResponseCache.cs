using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace FluentSignals.Http.Caching
{
    /// <summary>
    /// In-memory implementation of response cache
    /// </summary>
    public class MemoryResponseCache : IResponseCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime ExpiresAt { get; set; }

            public CacheEntry(object value, DateTime expiresAt)
            {
                Value = value;
                ExpiresAt = expiresAt;
            }

            public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        }

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.IsExpired)
                {
                    _cache.TryRemove(key, out _);
                    return Task.FromResult<T?>(null);
                }

                return Task.FromResult(entry.Value as T);
            }

            return Task.FromResult<T?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            var expiresAt = DateTime.UtcNow.Add(expiration);
            _cache[key] = new CacheEntry(value, expiresAt);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _cache.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _cache.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes expired entries from the cache
        /// </summary>
        public void CleanupExpired()
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}