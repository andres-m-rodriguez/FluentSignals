using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentSignals.Http.Caching;
using FluentSignals.Http.Types;

namespace FluentSignals.Http.Resources;

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
        ICacheProvider cache,
        string cacheKey,
        TimeSpan expiration) where T : class
    {
        return new CachedHttpResourceRequest<T>(request, cache, cacheKey, expiration);
    }

    /// <summary>
    /// Adds caching with automatic key generation based on URL
    /// </summary>
    public static HttpResourceRequest<T> WithCache<T>(
        this HttpResourceRequest<T> request,
        ICacheProvider cache,
        TimeSpan expiration) where T : class
    {
        var cacheKey = GenerateCacheKey(request);
        return new CachedHttpResourceRequest<T>(request, cache, cacheKey, expiration);
    }

    private static string GenerateCacheKey<T>(HttpResourceRequest<T> request)
    {
        // Generate cache key from method and URL
        var url = request.BuildUrl();
        return $"{request.GetType().Name}:{request._method}:{url}";
    }
}

/// <summary>
/// Cached HTTP resource request that checks cache before executing
/// </summary>
public class CachedHttpResourceRequest<T> : HttpResourceRequest<T> where T : class
{
    private readonly HttpResourceRequest<T> _innerRequest;
    private readonly ICacheProvider _cache;
    private readonly string _cacheKey;
    private readonly TimeSpan _expiration;

    internal CachedHttpResourceRequest(
        HttpResourceRequest<T> innerRequest,
        ICacheProvider cache,
        string cacheKey,
        TimeSpan expiration)
        : base(innerRequest._httpClient, innerRequest._options, innerRequest._method, innerRequest._url, innerRequest._data)
    {
        _innerRequest = innerRequest;
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));
        _expiration = expiration;

        // Copy headers and query params from inner request
        foreach (var header in innerRequest._headers)
            _headers[header.Key] = header.Value;
        
        foreach (var param in innerRequest._queryParams)
            _queryParams[param.Key] = param.Value;

        // Copy configurations
        _configureResource = innerRequest._configureResource;
        _configureRequest = innerRequest._configureRequest;
    }

    public override async Task<HttpResource> ExecuteAsync()
    {
        // Check cache first
        var cached = await _cache.GetAsync<T>(_cacheKey);
        if (cached != null)
        {
            var resource = CreateResource();
            // Create a mock response message to get headers
            using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var headers = responseMessage.Headers;
            var content = System.Text.Json.JsonSerializer.Serialize(cached);
            resource.Value = new HttpResponse<T>(HttpStatusCode.OK, headers, content, cached);
            return resource;
        }

        // Execute the request
        var result = await base.ExecuteAsync();

        // Cache successful responses
        if (result.Value?.IsSuccess == true && result.Value is HttpResponse<T> typedResponse && typedResponse.Data is T data)
        {
            await _cache.SetAsync(_cacheKey, data, _expiration);
        }

        return result;
    }

    /// <summary>
    /// Invalidates the cache entry for this request
    /// </summary>
    public async Task InvalidateCacheAsync()
    {
        await _cache.RemoveAsync(_cacheKey);
    }
}