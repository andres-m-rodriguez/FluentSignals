using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSignals.Http.Caching.Providers
{
    /// <summary>
    /// A cache provider that doesn't cache anything (null object pattern)
    /// </summary>
    public class NoCacheProvider : ICacheProvider
    {
        public static readonly NoCacheProvider Instance = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            return Task.FromResult<T?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}