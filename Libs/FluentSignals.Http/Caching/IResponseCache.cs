using System;
using System.Threading.Tasks;

namespace FluentSignals.Http.Caching;

/// <summary>
/// Interface for caching HTTP responses
/// </summary>
public interface IResponseCache
{
    /// <summary>
    /// Gets a cached response
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Sets a cached response
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;

    /// <summary>
    /// Removes a cached response
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Clears all cached responses
    /// </summary>
    Task ClearAsync();
}