using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FluentSignals.Http.Interceptors
{
    /// <summary>
    /// Interceptor that logs HTTP requests and responses
    /// </summary>
    public class LoggingInterceptor : IHttpResourceInterceptor
    {
        private readonly ILogger _logger;
        private readonly LogLevel _requestLogLevel;
        private readonly LogLevel _responseLogLevel;
        private readonly bool _logBody;

        public LoggingInterceptor(ILogger logger, LogLevel requestLogLevel = LogLevel.Debug, LogLevel responseLogLevel = LogLevel.Debug, bool logBody = false)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _requestLogLevel = requestLogLevel;
            _responseLogLevel = responseLogLevel;
            _logBody = logBody;
        }

        public async Task<HttpRequestMessage> OnRequestAsync(HttpRequestMessage request)
        {
            if (_logger.IsEnabled(_requestLogLevel))
            {
                _logger.Log(_requestLogLevel, "HTTP {Method} {Uri}", request.Method, request.RequestUri);
                
                if (_logBody && request.Content != null)
                {
                    var body = await request.Content.ReadAsStringAsync();
                    _logger.Log(_requestLogLevel, "Request Body: {Body}", body);
                }
            }

            // Store start time for measuring duration
            request.Options.Set(new HttpRequestOptionsKey<Stopwatch>("RequestStopwatch"), Stopwatch.StartNew());
            
            return request;
        }

        public Task<HttpResponseMessage> OnResponseAsync(HttpResponseMessage response)
        {
            if (_logger.IsEnabled(_responseLogLevel))
            {
                var duration = TimeSpan.Zero;
                if (response.RequestMessage?.Options.TryGetValue(new HttpRequestOptionsKey<Stopwatch>("RequestStopwatch"), out var stopwatch) == true)
                {
                    stopwatch.Stop();
                    duration = stopwatch.Elapsed;
                }

                _logger.Log(_responseLogLevel, "HTTP {Method} {Uri} responded {StatusCode} in {Duration}ms",
                    response.RequestMessage?.Method,
                    response.RequestMessage?.RequestUri,
                    (int)response.StatusCode,
                    duration.TotalMilliseconds);
            }

            return Task.FromResult(response);
        }

        public Task OnExceptionAsync(HttpRequestMessage request, Exception exception)
        {
            _logger.LogError(exception, "HTTP {Method} {Uri} failed with exception", request.Method, request.RequestUri);
            return Task.CompletedTask;
        }
    }
}