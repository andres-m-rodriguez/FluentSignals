using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;

namespace FluentSignals.Options.HttpResource;

public static class HttpResourceOptionsExtensions
{
    public static IServiceCollection AddFluentSignalsHttpResource(
        this IServiceCollection services,
        Action<HttpResourceOptions>? configure = null)
    {
        var options = new HttpResourceOptions();
        configure?.Invoke(options);
        options.Validate();
        
        services.AddSingleton(options);
        
        if (options.ClientSource == HttpClientSource.DependencyInjection)
        {
            services.AddHttpClient();
        }
        
        return services;
    }
    
    public static HttpResourceOptions UseDirectHttpClient(
        this HttpResourceOptions options,
        HttpClient httpClient)
    {
        options.ClientSource = HttpClientSource.Direct;
        options.HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        return options;
    }
    
    public static HttpResourceOptions UseHttpClientFactory(
        this HttpResourceOptions options,
        IHttpClientFactory factory,
        string? clientName = null)
    {
        options.ClientSource = HttpClientSource.Factory;
        options.HttpClientFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        options.HttpClientName = clientName;
        return options;
    }
    
    public static HttpResourceOptions UseDependencyInjection(
        this HttpResourceOptions options,
        string? clientName = null)
    {
        options.ClientSource = HttpClientSource.DependencyInjection;
        options.HttpClientName = clientName;
        return options;
    }
    
    public static HttpResourceOptions WithBaseUrl(
        this HttpResourceOptions options,
        string baseUrl)
    {
        options.BaseUrl = baseUrl;
        return options;
    }
    
    public static HttpResourceOptions WithTimeout(
        this HttpResourceOptions options,
        TimeSpan timeout)
    {
        options.Timeout = timeout;
        return options;
    }
    
    public static HttpResourceOptions WithHeader(
        this HttpResourceOptions options,
        string name,
        string value)
    {
        options.DefaultHeaders[name] = value;
        return options;
    }
    
    public static HttpResourceOptions WithRetry(
        this HttpResourceOptions options,
        Action<RetryOptions> configureRetry)
    {
        options.RetryOptions ??= new RetryOptions();
        configureRetry?.Invoke(options.RetryOptions);
        return options;
    }
    
    public static HttpResourceOptions WithRetry(
        this HttpResourceOptions options,
        int maxAttempts,
        params HttpStatusCode[] retryableStatusCodes)
    {
        options.RetryOptions ??= new RetryOptions();
        options.RetryOptions.MaxRetryAttempts = maxAttempts;
        options.RetryOptions.RetryableStatusCodes = retryableStatusCodes.ToList();
        return options;
    }
}