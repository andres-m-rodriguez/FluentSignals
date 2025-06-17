using System.Net.Http;
using System.Text.Json;

namespace FluentSignals.Options.HttpResource;

public enum HttpClientSource
{
    DependencyInjection,
    Direct,
    Factory
}

public class HttpResourceOptions
{
    public string? BaseUrl { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string> DefaultHeaders { get; set; } = [];
    
    public HttpClientSource ClientSource { get; set; } = HttpClientSource.DependencyInjection;
    public HttpClient? HttpClient { get; set; }
    public IHttpClientFactory? HttpClientFactory { get; set; }
    public string? HttpClientName { get; set; }
    
    public RetryOptions? RetryOptions { get; set; }
    
    /// <summary>
    /// JSON serialization options used for deserializing HTTP responses.
    /// If not set, defaults to PropertyNameCaseInsensitive = true.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public void Validate()
    {
        switch (ClientSource)
        {
            case HttpClientSource.Direct:
                if (HttpClient == null)
                    throw new InvalidOperationException("HttpClient must be provided when ClientSource is Direct.");
                break;

            case HttpClientSource.Factory:
                if (HttpClientFactory == null)
                    throw new InvalidOperationException("HttpClientFactory must be provided when ClientSource is Factory.");
                break;

            case HttpClientSource.DependencyInjection:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(ClientSource), ClientSource, "Invalid HttpClientSource value.");
        }
    }
}
