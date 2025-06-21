using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FluentSignals.Http.Interceptors
{
    /// <summary>
    /// Interceptor that adds bearer token authentication to requests
    /// </summary>
    public class BearerTokenInterceptor : IHttpResourceInterceptor
    {
        private readonly Func<Task<string>> _tokenProvider;

        /// <summary>
        /// Creates a new bearer token interceptor
        /// </summary>
        /// <param name="tokenProvider">Function that provides the bearer token</param>
        public BearerTokenInterceptor(Func<Task<string>> tokenProvider)
        {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        }

        /// <summary>
        /// Creates a new bearer token interceptor with a static token
        /// </summary>
        /// <param name="token">The bearer token</param>
        public BearerTokenInterceptor(string token) : this(() => Task.FromResult(token))
        {
        }

        public async Task<HttpRequestMessage> OnRequestAsync(HttpRequestMessage request)
        {
            var token = await _tokenProvider();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return request;
        }

        public Task<HttpResponseMessage> OnResponseAsync(HttpResponseMessage response)
        {
            return Task.FromResult(response);
        }

        public Task OnExceptionAsync(HttpRequestMessage request, Exception exception)
        {
            return Task.CompletedTask;
        }
    }
}