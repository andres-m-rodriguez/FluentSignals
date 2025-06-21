using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentSignals.Http.Options;

namespace FluentSignals.Http.Resources
{
    /// <summary>
    /// Base class for typed HTTP resources with interceptor support
    /// </summary>
    public abstract class TypedHttpResourceWithInterceptors : TypedHttpResource
    {
        private readonly List<IHttpResourceInterceptor> _interceptors = new();

        protected TypedHttpResourceWithInterceptors()
        {
        }

        protected TypedHttpResourceWithInterceptors(HttpClient httpClient, string baseUrl)
            : base(httpClient, baseUrl)
        {
        }

        protected TypedHttpResourceWithInterceptors(HttpClient httpClient, string baseUrl, HttpResourceOptions options)
            : base(httpClient, baseUrl, options)
        {
        }

        /// <summary>
        /// Adds an interceptor to the resource
        /// </summary>
        protected void AddInterceptor(IHttpResourceInterceptor interceptor)
        {
            _interceptors.Add(interceptor);
        }

        /// <summary>
        /// Removes an interceptor from the resource
        /// </summary>
        protected bool RemoveInterceptor(IHttpResourceInterceptor interceptor)
        {
            return _interceptors.Remove(interceptor);
        }

        /// <summary>
        /// Clears all interceptors
        /// </summary>
        protected void ClearInterceptors()
        {
            _interceptors.Clear();
        }

        /// <summary>
        /// Creates a GET request resource with interceptor support
        /// </summary>
        protected new HttpResourceRequest<T> Get<T>(string url)
        {
            return base.Get<T>(url).WithInterceptors(_interceptors);
        }

        /// <summary>
        /// Creates a POST request resource with interceptor support
        /// </summary>
        protected new HttpResourceRequest<TResponse> Post<TRequest, TResponse>(string url, TRequest data)
        {
            return base.Post<TRequest, TResponse>(url, data).WithInterceptors(_interceptors);
        }

        /// <summary>
        /// Creates a PUT request resource with interceptor support
        /// </summary>
        protected new HttpResourceRequest<TResponse> Put<TRequest, TResponse>(string url, TRequest data)
        {
            return base.Put<TRequest, TResponse>(url, data).WithInterceptors(_interceptors);
        }

        /// <summary>
        /// Creates a PATCH request resource with interceptor support
        /// </summary>
        protected new HttpResourceRequest<TResponse> Patch<TRequest, TResponse>(string url, TRequest data)
        {
            return base.Patch<TRequest, TResponse>(url, data).WithInterceptors(_interceptors);
        }

        /// <summary>
        /// Creates a DELETE request resource with interceptor support
        /// </summary>
        protected new HttpResourceRequest<TResponse> Delete<TResponse>(string url)
        {
            return base.Delete<TResponse>(url).WithInterceptors(_interceptors);
        }

        /// <summary>
        /// Creates a DELETE request resource with interceptor support (no response)
        /// </summary>
        protected new HttpResourceRequest Delete(string url)
        {
            return base.Delete(url).WithInterceptors(_interceptors);
        }
    }

    /// <summary>
    /// Extension methods for adding interceptor support to HttpResourceRequest
    /// </summary>
    internal static class InterceptorExtensions
    {
        private const string InterceptorsKey = "Interceptors";

        internal static HttpResourceRequest<T> WithInterceptors<T>(this HttpResourceRequest<T> request, List<IHttpResourceInterceptor> interceptors)
        {
            if (interceptors.Count == 0) return request;

            return request.ConfigureRequest(req =>
            {
                req.Options.Set(new HttpRequestOptionsKey<List<IHttpResourceInterceptor>>(InterceptorsKey), interceptors);
            });
        }

        internal static HttpResourceRequest WithInterceptors(this HttpResourceRequest request, List<IHttpResourceInterceptor> interceptors)
        {
            if (interceptors.Count == 0) return request;

            return request.ConfigureRequest(req =>
            {
                req.Options.Set(new HttpRequestOptionsKey<List<IHttpResourceInterceptor>>(InterceptorsKey), interceptors);
            });
        }
    }
}