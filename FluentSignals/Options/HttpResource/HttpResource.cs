using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace FluentSignals.Options.HttpResource;

public class HttpResource : AsyncTypedSignal<HttpResponse?>, IResource<HttpResponse?>
{
    
    private readonly HttpClient _httpClient;
    private readonly HttpResourceOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage>? _retryPolicy;
    private Func<Task>? _lastRequest;

    public ISignal<HttpStatusCode?> LastStatusCode { get; } = new TypedSignal<HttpStatusCode?>(null);
    
    public async Task RunAsync(Func<Task> action)
    {
        await base.LoadAsync(async () =>
        {
            await action();
            return Value;
        });
    }

    public HttpResource(HttpClient httpClient)
        : this(httpClient, new HttpResourceOptions()) { }

    public HttpResource(HttpClient httpClient, HttpResourceOptions options)
        : base(null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Configure HttpClient with options
        if (_options.Timeout != default)
        {
            _httpClient.Timeout = _options.Timeout;
        }

        if (!string.IsNullOrEmpty(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }

        // Add default headers
        foreach (var header in _options.DefaultHeaders)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Configure retry policy if enabled
        if (_options.RetryOptions != null && _options.RetryOptions.MaxRetryAttempts > 0)
        {
            _retryPolicy = CreateRetryPolicy(_options.RetryOptions);
        }
    }

    public static HttpResource Create(IServiceProvider serviceProvider, HttpResourceOptions? options = null)
    {
        options ??= serviceProvider.GetService<HttpResourceOptions>() ?? new HttpResourceOptions();
        
        HttpClient httpClient;
        switch (options.ClientSource)
        {
            case HttpClientSource.Direct:
                httpClient = options.HttpClient ?? throw new InvalidOperationException("HttpClient not configured");
                break;
                
            case HttpClientSource.Factory:
                var factory = options.HttpClientFactory ?? throw new InvalidOperationException("HttpClientFactory not configured");
                httpClient = string.IsNullOrEmpty(options.HttpClientName) 
                    ? factory.CreateClient() 
                    : factory.CreateClient(options.HttpClientName);
                break;
                
            case HttpClientSource.DependencyInjection:
                factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                httpClient = string.IsNullOrEmpty(options.HttpClientName) 
                    ? factory.CreateClient() 
                    : factory.CreateClient(options.HttpClientName);
                break;
                
            default:
                throw new InvalidOperationException($"Unknown HttpClientSource: {options.ClientSource}");
        }
        
        return new HttpResource(httpClient, options);
    }

    // HTTP Methods
    public async Task<HttpResponse> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => GetAsync(url, cancellationToken);
        return await ExecuteAsync<object>(HttpMethod.Get, url, null, cancellationToken);
    }
    
    public async Task<HttpResponse<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => GetAsync(url, cancellationToken);
        return await ExecuteAsync<T>(HttpMethod.Get, url, null, cancellationToken);
    }

    public async Task<HttpResponse> PostAsync<TBody>(string url, TBody body, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => PostAsync(url, body, cancellationToken);
        return await ExecuteAsync(HttpMethod.Post, url, body, cancellationToken);
    }
    
    public async Task<HttpResponse<TResponse>> PostAsync<TBody, TResponse>(string url, TBody body, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => PostAsync<TBody, TResponse>(url, body, cancellationToken);
        return await ExecuteAsync<TResponse>(HttpMethod.Post, url, body, cancellationToken);
    }

    public async Task<HttpResponse> PutAsync<TBody>(string url, TBody body, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => PutAsync(url, body, cancellationToken);
        return await ExecuteAsync(HttpMethod.Put, url, body, cancellationToken);
    }
    
    public async Task<HttpResponse<TResponse>> PutAsync<TBody, TResponse>(string url, TBody body, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => PutAsync<TBody, TResponse>(url, body, cancellationToken);
        return await ExecuteAsync<TResponse>(HttpMethod.Put, url, body, cancellationToken);
    }

    public async Task<HttpResponse> DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => DeleteAsync(url, cancellationToken);
        return await ExecuteAsync<object>(HttpMethod.Delete, url, null, cancellationToken);
    }
    
    public async Task<HttpResponse<T>> DeleteAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => DeleteAsync<T>(url, cancellationToken);
        return await ExecuteAsync<T>(HttpMethod.Delete, url, null, cancellationToken);
    }

    public async Task<HttpResponse> PatchAsync<TBody>(string url, TBody body, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => PatchAsync(url, body, cancellationToken);
        return await ExecuteAsync(HttpMethod.Patch, url, body, cancellationToken);
    }
    
    public async Task<HttpResponse<TResponse>> PatchAsync<TBody, TResponse>(string url, TBody body, CancellationToken cancellationToken = default)
    {
        _lastRequest = () => PatchAsync<TBody, TResponse>(url, body, cancellationToken);
        return await ExecuteAsync<TResponse>(HttpMethod.Patch, url, body, cancellationToken);
    }

    private async Task<HttpResponse> ExecuteAsync<TBody>(HttpMethod method, string url, TBody? body, CancellationToken cancellationToken)
    {
        try
        {
            IsLoading.Value = true;
            Error.Value = null;
            Value = null;

            using var request = new HttpRequestMessage(method, url);

            if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                request.Content = JsonContent.Create(body);
            }

            HttpResponseMessage response;

            if (_retryPolicy != null)
            {
                response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var retryRequest = await CloneHttpRequestMessageAsync(request);
                    return await _httpClient.SendAsync(retryRequest, cancellationToken);
                });
            }
            else
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }

            LastStatusCode.Value = response.StatusCode;
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var httpResponse = new HttpResponse(response.StatusCode, response.Headers, responseContent);
            Value = httpResponse;
            
            return httpResponse;
        }
        catch (HttpRequestException ex)
        {
            Error.Value = new HttpError(ex.Message, "HTTP_REQUEST_ERROR", ex);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Error.Value = new HttpError("Request timeout", "TIMEOUT", ex);
            throw;
        }
        catch (Exception ex)
        {
            Error.Value = new HttpError(ex.Message, "UNKNOWN_ERROR", ex);
            throw;
        }
        finally
        {
            IsLoading.Value = false;
        }
    }
    
    private async Task<HttpResponse<TResponse>> ExecuteAsync<TResponse>(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        try
        {
            IsLoading.Value = true;
            Error.Value = null;
            Value = null;

            using var request = new HttpRequestMessage(method, url);

            if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                request.Content = JsonContent.Create(body);
            }

            HttpResponseMessage response;

            if (_retryPolicy != null)
            {
                response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var retryRequest = await CloneHttpRequestMessageAsync(request);
                    return await _httpClient.SendAsync(retryRequest, cancellationToken);
                });
            }
            else
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }

            LastStatusCode.Value = response.StatusCode;
            var responseContent = await response.Content.ReadAsStringAsync();
            
            TResponse? parsedData = default;
            if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseContent))
            {
                try
                {
                    parsedData = JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                catch (JsonException)
                {
                    // If parsing fails, parsedData remains null
                }
            }
            
            var httpResponse = new HttpResponse<TResponse>(response.StatusCode, response.Headers, responseContent, parsedData);
            Value = httpResponse;
            
            return httpResponse;
        }
        catch (HttpRequestException ex)
        {
            Error.Value = new HttpError(ex.Message, "HTTP_REQUEST_ERROR", ex);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            Error.Value = new HttpError("Request timeout", "TIMEOUT", ex);
            throw;
        }
        catch (Exception ex)
        {
            Error.Value = new HttpError(ex.Message, "UNKNOWN_ERROR", ex);
            throw;
        }
        finally
        {
            IsLoading.Value = false;
        }
    }

    public async Task RefreshAsync()
    {
        if (_lastRequest != null)
        {
            await _lastRequest();
        }
    }
    
    public override void Dispose()
    {
        base.Dispose();
        LastStatusCode.Dispose();
    }

    private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(RetryOptions options)
    {
        var retryStatusCodes = options.RetryableStatusCodes.Any()
            ? options.RetryableStatusCodes.ToArray()
            : new[]
            {
                HttpStatusCode.RequestTimeout,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout,
            };

        var policyBuilder = Policy
            .HandleResult<HttpResponseMessage>(msg => retryStatusCodes.Contains(msg.StatusCode))
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>();

        if (options.UseExponentialBackoff)
        {
            return policyBuilder.WaitAndRetryAsync(
                options.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * options.InitialRetryDelay),
                onRetry: async (outcome, timespan, retryCount, context) =>
                {
                    if (options.OnRetry != null)
                    {
                        await options.OnRetry(retryCount, timespan);
                    }
                }
            );
        }
        else
        {
            return policyBuilder.WaitAndRetryAsync(
                options.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(options.InitialRetryDelay),
                onRetry: async (outcome, timespan, retryCount, context) =>
                {
                    if (options.OnRetry != null)
                    {
                        await options.OnRetry(retryCount, timespan);
                    }
                }
            );
        }
    }

    private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    /// <summary>
    /// Gets all internal signals that compose this HTTP resource.
    /// Includes the base async signal's internal signals plus the LastStatusCode signal.
    /// </summary>
    /// <returns>A collection containing all internal signals including LastStatusCode.</returns>
    public override IEnumerable<ISignal> GetInternalSignals()
    {
        // Get base internal signals (IsLoading, Error, and the value signal)
        foreach (var signal in base.GetInternalSignals())
        {
            yield return signal;
        }
        
        // Add the LastStatusCode signal specific to HttpResource
        yield return LastStatusCode;
    }
}