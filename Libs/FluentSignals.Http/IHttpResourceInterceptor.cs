using System.Net.Http;
using System.Threading.Tasks;

namespace FluentSignals.Http
{
    /// <summary>
    /// Interface for intercepting HTTP requests and responses
    /// </summary>
    public interface IHttpResourceInterceptor
    {
        /// <summary>
        /// Called before sending the HTTP request
        /// </summary>
        Task<HttpRequestMessage> OnRequestAsync(HttpRequestMessage request);

        /// <summary>
        /// Called after receiving the HTTP response
        /// </summary>
        Task<HttpResponseMessage> OnResponseAsync(HttpResponseMessage response);

        /// <summary>
        /// Called when an exception occurs during the request
        /// </summary>
        Task OnExceptionAsync(HttpRequestMessage request, System.Exception exception);
    }
}