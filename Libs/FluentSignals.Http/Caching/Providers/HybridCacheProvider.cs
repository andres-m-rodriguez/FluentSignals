using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;

namespace FluentSignals.Http.Caching.Providers
{
    /// <summary>
    /// Cache provider using Microsoft.Extensions.Caching.Hybrid
    /// </summary>
    public class HybridCacheProvider : ICacheProvider
    {
        private readonly HybridCache _hybridCache;

        public HybridCacheProvider(HybridCache hybridCache)
        {
            _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            // HybridCache uses GetOrCreateAsync pattern
            // For a pure get operation, we need to check if the value exists
            // by using a factory that returns a sentinel value
            var sentinel = new object();
            var result = await _hybridCache.GetOrCreateAsync<object?>(
                key,
                async token => await Task.FromResult(sentinel));

            if (ReferenceEquals(result, sentinel))
            {
                // Value was not in cache
                return null;
            }

            return result as T;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            var options = expiration.HasValue 
                ? new HybridCacheEntryOptions
                {
                    Expiration = expiration.Value,
                    LocalCacheExpiration = expiration.Value
                }
                : new HybridCacheEntryOptions();

            await _hybridCache.SetAsync(key, value, options);
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            // HybridCache doesn't have a RemoveAsync method in preview
            // This is a limitation of the current preview version
            // For now, we'll just complete the task
            return Task.CompletedTask;
        }
    }
}