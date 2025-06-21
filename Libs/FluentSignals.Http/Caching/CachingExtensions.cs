using System;
using FluentSignals.Http.Resources;

namespace FluentSignals.Http.Caching
{
    /// <summary>
    /// Extension methods for adding caching support to HTTP resource requests
    /// </summary>
    public static class CachingExtensions
    {
        /// <summary>
        /// Adds caching to a typed HTTP resource request
        /// </summary>
        public static HttpResourceRequest<T> WithCache<T>(
            this HttpResourceRequest<T> request,
            ICacheProvider cacheProvider,
            string cacheKey,
            TimeSpan expiration) where T : class
        {
            if (cacheProvider is Providers.NoCacheProvider)
            {
                // If using NoCacheProvider, just return the original request
                return request;
            }

            return new CachedHttpResourceRequest<T>(request, cacheProvider, cacheKey, expiration);
        }

        /// <summary>
        /// Adds caching with automatic key generation based on URL
        /// </summary>
        public static HttpResourceRequest<T> WithCache<T>(
            this HttpResourceRequest<T> request,
            ICacheProvider cacheProvider,
            TimeSpan expiration) where T : class
        {
            if (cacheProvider is Providers.NoCacheProvider)
            {
                // If using NoCacheProvider, just return the original request
                return request;
            }

            var cacheKey = GenerateCacheKey(request);
            return new CachedHttpResourceRequest<T>(request, cacheProvider, cacheKey, expiration);
        }

        private static string GenerateCacheKey<T>(HttpResourceRequest<T> request)
        {
            // Generate cache key from method and URL
            var url = request.BuildUrl();
            return $"{typeof(T).Name}:{request._method}:{url}";
        }
    }
}