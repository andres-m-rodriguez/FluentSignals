using FluentSignals.Http.Contracts;
using FluentSignals.Http.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Playground.Console.Middleware;

public class CacheMiddleware : IHttpResourceMiddleware
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheMiddleware> _logger;

    public CacheMiddleware(IMemoryCache cache, ILogger<CacheMiddleware> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> InvokeAsync(
        HttpRequestMessage request,
        HttpResourceHandler next,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = request.RequestUri?.ToString() ?? "";

        if (_cache.TryGetValue<HttpResponseMessage>(cacheKey, out var cachedResponse))
        {

            return await CloneResponseAsync(cachedResponse);
        }

        _logger.LogInformation("Cache miss for {Url}", cacheKey);

        var response = await next(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _cache.Set(cacheKey, await CloneResponseAsync(response), TimeSpan.FromMinutes(5));
        }

        return response;
    }

    private async Task<HttpResponseMessage> CloneResponseAsync(HttpResponseMessage original)
    {
        var newResponse = new HttpResponseMessage(original.StatusCode)
        {
            Version = original.Version,
            ReasonPhrase = original.ReasonPhrase,
            RequestMessage = original.RequestMessage,
        };

        if (original.Content != null)
        {
            var contentBytes = await original.Content.ReadAsByteArrayAsync();
            newResponse.Content = new ByteArrayContent(contentBytes);

            foreach (var header in original.Content.Headers)
                newResponse.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in original.Headers)
            newResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return newResponse;
    }
}
