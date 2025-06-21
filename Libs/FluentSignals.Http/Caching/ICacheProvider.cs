using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSignals.Http.Caching
{
    /// <summary>
    /// Abstraction for cache providers
    /// </summary>
    public interface ICacheProvider
    {
        /// <summary>
        /// Gets a value from the cache
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            where T : class;

        /// <summary>
        /// Sets a value in the cache
        /// </summary>
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default
        )
            where T : class;

        /// <summary>
        /// Removes a value from the cache
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}
