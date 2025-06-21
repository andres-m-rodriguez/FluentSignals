using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FluentSignals.Http.Interceptors
{
    /// <summary>
    /// Interceptor that handles retry logic for failed requests
    /// </summary>
    public class RetryInterceptor : IHttpResourceInterceptor
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _delay;
        private readonly Func<HttpResponseMessage, bool> _shouldRetry;

        public RetryInterceptor(int maxRetries = 3, TimeSpan? delay = null)
        {
            _maxRetries = maxRetries;
            _delay = delay ?? TimeSpan.FromSeconds(1);
            _shouldRetry = DefaultShouldRetry;
        }

        public RetryInterceptor(int maxRetries, TimeSpan delay, Func<HttpResponseMessage, bool> shouldRetry)
        {
            _maxRetries = maxRetries;
            _delay = delay;
            _shouldRetry = shouldRetry ?? DefaultShouldRetry;
        }

        private static bool DefaultShouldRetry(HttpResponseMessage response)
        {
            // Retry on 5xx errors and specific 4xx errors
            return response.StatusCode >= HttpStatusCode.InternalServerError ||
                   response.StatusCode == HttpStatusCode.RequestTimeout ||
                   response.StatusCode == HttpStatusCode.TooManyRequests;
        }

        public Task<HttpRequestMessage> OnRequestAsync(HttpRequestMessage request)
        {
            // Store retry count
            request.Options.Set(new HttpRequestOptionsKey<int>("RetryCount"), 0);
            request.Options.Set(new HttpRequestOptionsKey<int>("MaxRetries"), _maxRetries);
            request.Options.Set(new HttpRequestOptionsKey<TimeSpan>("RetryDelay"), _delay);
            
            return Task.FromResult(request);
        }

        public async Task<HttpResponseMessage> OnResponseAsync(HttpResponseMessage response)
        {
            if (!_shouldRetry(response))
                return response;

            if (response.RequestMessage?.Options.TryGetValue(new HttpRequestOptionsKey<int>("RetryCount"), out var retryCount) == true &&
                retryCount < _maxRetries)
            {
                // Mark response for retry
                response.Headers.Add("X-Should-Retry", "true");
                response.Headers.Add("X-Retry-Count", retryCount.ToString());
                
                // Wait before retry
                await Task.Delay(_delay);
            }

            return response;
        }

        public Task OnExceptionAsync(HttpRequestMessage request, Exception exception)
        {
            // Network errors should be retried
            if (exception is HttpRequestException || exception is TaskCanceledException)
            {
                if (request.Options.TryGetValue(new HttpRequestOptionsKey<int>("RetryCount"), out var retryCount) &&
                    retryCount < _maxRetries)
                {
                    // Mark for retry by storing in request options
                    request.Options.Set(new HttpRequestOptionsKey<bool>("ShouldRetryOnException"), true);
                }
            }

            return Task.CompletedTask;
        }
    }
}