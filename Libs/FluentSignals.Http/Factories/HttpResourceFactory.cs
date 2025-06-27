using FluentSignals.Http.Core;
using FluentSignals.Http.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentSignals.Http.Factories;

public class HttpResourceFactory(IServiceProvider service, IOptions<HttpResourceOptions> options)
{
    private HttpClient? _cachedClient;
    private readonly IServiceProvider _service = service;
    private readonly HttpResourceOptions _options = options.Value;

    public HttpResource<HttpResponseMessage> Create(string url)
    {
        var client = ResolveHttpClient();
        return new HttpResource<HttpResponseMessage>(
            client,
            requestBuilder: () => new HttpRequestMessage(HttpMethod.Get, url),
            service
        );
    }

    public HttpResource<T> Create<T>(string url)
    {
        var client = ResolveHttpClient();
        return new HttpResource<T>(
            client,
            requestBuilder: () => new HttpRequestMessage(HttpMethod.Get, url),
            service
        );
    }

    public HttpResource<T> Create<T>(Func<HttpRequestMessage> requestBuilder)
    {
        var client = ResolveHttpClient();
        return new HttpResource<T>(client, requestBuilder, service);
    }

    private HttpClient ResolveHttpClient()
    {
        if (_cachedClient is not null)
        {
            return _cachedClient;
        }

        var preference = _options.HttpClientPreference;
        var httpClient = _service.GetService<HttpClient>();
        var httpFactory = _service.GetService<IHttpClientFactory>();

        bool httpClientExists = httpClient is not null;
        bool httpFactoryExists = httpFactory is not null;

        if (!httpClientExists && !httpFactoryExists)
            throw new InvalidOperationException(
                "Neither HttpClient nor IHttpClientFactory is registered."
            );

        if (preference.IsHttpClient && httpClientExists)
            return _cachedClient = httpClient!;

        if (preference.IsHttpFactory && httpFactoryExists)
            return _cachedClient = httpFactory!.CreateClient();

        if (!preference.ThrowExceptionIfPreferedClientNotFound)
            return _cachedClient = httpClient ?? httpFactory!.CreateClient();

        throw new InvalidOperationException(
            $"Preferred HttpClient ({preference.Value}) not found and fallback is disabled."
        );
    }
}
