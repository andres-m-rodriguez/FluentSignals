using FluentSignals.Http.Options;
using FluentSignals.Http.Resources;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace FluentSignals.Blazor.Http;

public class HttpResourceFactory : IHttpResourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpResourceOptions _defaultOptions;

    public HttpResourceFactory(IServiceProvider serviceProvider, HttpResourceOptions defaultOptions)
    {
        _serviceProvider = serviceProvider;
        _defaultOptions = defaultOptions;
    }

    public HttpResource Create()
    {
        return HttpResource.Create(_serviceProvider, _defaultOptions);
    }

    public HttpResource Create(HttpClient httpClient)
    {
        var options = new HttpResourceOptions();
        options.UseDirectHttpClient(httpClient);
        return new HttpResource(httpClient, options);
    }

    public HttpResource CreateWithBaseUrl(string baseUrl)
    {
        var options = new HttpResourceOptions();
        options.BaseUrl = baseUrl;
        return HttpResource.Create(_serviceProvider, options);
    }

    public HttpResource CreateWithOptions(Action<HttpResourceOptions> configure)
    {
        var options = new HttpResourceOptions();
        configure?.Invoke(options);
        return HttpResource.Create(_serviceProvider, options);
    }
}