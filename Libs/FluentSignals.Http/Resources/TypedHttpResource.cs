using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentSignals.Http.Contracts;
using FluentSignals.Http.Options;
using FluentSignals.Http.Types;

namespace FluentSignals.Http.Resources
{
    /// <summary>
    /// Base class for creating typed HTTP resources with strongly-typed methods
    /// </summary>
    public abstract class TypedHttpResource : ITypedHttpResource
    {
        protected HttpClient _httpClient;
        protected HttpResourceOptions _options;
        
        /// <summary>
        /// Gets the base URL for this resource
        /// </summary>
        public string BaseUrl { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the TypedHttpResource class
        /// </summary>
        /// <param name="httpClient">The HTTP client to use</param>
        /// <param name="baseUrl">The base URL for this resource (e.g., "/api/users")</param>
        protected TypedHttpResource(HttpClient httpClient, string baseUrl)
            : this(httpClient, baseUrl, new HttpResourceOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the TypedHttpResource class with options
        /// </summary>
        /// <param name="httpClient">The HTTP client to use</param>
        /// <param name="baseUrl">The base URL for this resource (e.g., "/api/users")</param>
        /// <param name="options">The HTTP resource options</param>
        protected TypedHttpResource(HttpClient httpClient, string baseUrl, HttpResourceOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Default constructor for factory-based creation
        /// </summary>
        protected TypedHttpResource()
        {
            // These will be set by the factory
            _httpClient = null!;
            _options = null!;
            BaseUrl = null!;
        }

        /// <summary>
        /// Initializes the resource with the provided dependencies
        /// This method is called by the factory after construction
        /// </summary>
        public virtual void Initialize(HttpClient httpClient, string baseUrl, HttpResourceOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Creates a GET request resource
        /// </summary>
        protected HttpResourceRequest<T> Get<T>(string url)
        {
            return new HttpResourceRequest<T>(_httpClient, _options, HttpMethod.Get, url);
        }

        /// <summary>
        /// Creates a GET request resource with query parameters
        /// </summary>
        protected HttpResourceRequest<T> Get<T>(string url, object queryParams)
        {
            var queryString = BuildQueryString(queryParams);
            var fullUrl = string.IsNullOrEmpty(queryString) ? url : $"{url}?{queryString}";
            return Get<T>(fullUrl);
        }

        /// <summary>
        /// Creates a POST request resource with typed request body
        /// </summary>
        protected HttpResourceRequest<TResponse> Post<TRequest, TResponse>(string url, TRequest data)
        {
            return new HttpResourceRequest<TResponse>(_httpClient, _options, HttpMethod.Post, url, data);
        }

        /// <summary>
        /// Creates a POST request resource
        /// </summary>
        protected HttpResourceRequest<TResponse> Post<TResponse>(string url, object data)
        {
            return new HttpResourceRequest<TResponse>(_httpClient, _options, HttpMethod.Post, url, data);
        }

        /// <summary>
        /// Creates a POST request resource without response data but with typed request
        /// </summary>
        protected HttpResourceRequest Post<TRequest>(string url, TRequest data)
        {
            return new HttpResourceRequest(_httpClient, _options, HttpMethod.Post, url, data);
        }

        /// <summary>
        /// Creates a POST request resource without response data
        /// </summary>
        protected HttpResourceRequest Post(string url, object data)
        {
            return new HttpResourceRequest(_httpClient, _options, HttpMethod.Post, url, data);
        }

        /// <summary>
        /// Creates a PUT request resource with typed request body
        /// </summary>
        protected HttpResourceRequest<TResponse> Put<TRequest, TResponse>(string url, TRequest data)
        {
            return new HttpResourceRequest<TResponse>(_httpClient, _options, HttpMethod.Put, url, data);
        }

        /// <summary>
        /// Creates a PUT request resource
        /// </summary>
        protected HttpResourceRequest<TResponse> Put<TResponse>(string url, object data)
        {
            return new HttpResourceRequest<TResponse>(_httpClient, _options, HttpMethod.Put, url, data);
        }

        /// <summary>
        /// Creates a PUT request resource without response data but with typed request
        /// </summary>
        protected HttpResourceRequest Put<TRequest>(string url, TRequest data)
        {
            return new HttpResourceRequest(_httpClient, _options, HttpMethod.Put, url, data);
        }

        /// <summary>
        /// Creates a PUT request resource without response data
        /// </summary>
        protected HttpResourceRequest Put(string url, object data)
        {
            return new HttpResourceRequest(_httpClient, _options, HttpMethod.Put, url, data);
        }

        /// <summary>
        /// Creates a PATCH request resource with typed request body
        /// </summary>
        protected HttpResourceRequest<TResponse> Patch<TRequest, TResponse>(string url, TRequest data)
        {
            return new HttpResourceRequest<TResponse>(_httpClient, _options, HttpMethod.Patch, url, data);
        }

        /// <summary>
        /// Creates a PATCH request resource
        /// </summary>
        protected HttpResourceRequest<TResponse> Patch<TResponse>(string url, object data)
        {
            return new HttpResourceRequest<TResponse>(_httpClient, _options, HttpMethod.Patch, url, data);
        }

        /// <summary>
        /// Creates a DELETE request resource
        /// </summary>
        protected HttpResourceRequest Delete(string url)
        {
            return new HttpResourceRequest(_httpClient, _options, HttpMethod.Delete, url);
        }

        /// <summary>
        /// Creates a DELETE request resource with response
        /// </summary>
        protected HttpResourceRequest<TResponse> Delete<TResponse>(string url)
        {
            return new HttpResourceRequest<TResponse>(_httpClient, _options, HttpMethod.Delete, url);
        }

        /// <summary>
        /// Builds a query string from an object
        /// </summary>
        private string BuildQueryString(object queryParams)
        {
            if (queryParams == null) return string.Empty;

            var properties = queryParams.GetType().GetProperties();
            var queryParts = new List<string>();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(queryParams);
                if (value != null)
                {
                    var stringValue = value is string str ? str : JsonSerializer.Serialize(value);
                    queryParts.Add($"{Uri.EscapeDataString(prop.Name)}={Uri.EscapeDataString(stringValue)}");
                }
            }

            return string.Join("&", queryParts);
        }

        /// <summary>
        /// Creates a new HTTP resource with the default options
        /// </summary>
        protected HttpResource CreateResource()
        {
            return new HttpResource(_httpClient, _options);
        }

        /// <summary>
        /// Creates a new HTTP resource with custom options
        /// </summary>
        protected HttpResource CreateResource(Action<HttpResourceOptions> configure)
        {
            var options = _options.Clone();
            configure(options);
            return new HttpResource(_httpClient, options);
        }

        /// <summary>
        /// Creates a custom HTTP request with full type safety
        /// </summary>
        protected HttpResourceRequest<TResponse> Request<TResponse>(HttpMethod method, string url)
        {
            return new HttpResourceRequest<TResponse>(_httpClient, _options, method, url);
        }

        /// <summary>
        /// Creates a custom HTTP request with typed request body and response
        /// </summary>
        protected HttpResourceRequest<TResponse> Request<TRequest, TResponse>(HttpMethod method, string url, TRequest data)
        {
            return new HttpResourceRequest<TResponse>(_httpClient, _options, method, url, data);
        }

        /// <summary>
        /// Creates a custom HTTP request without response type
        /// </summary>
        protected HttpResourceRequest Request(HttpMethod method, string url)
        {
            return new HttpResourceRequest(_httpClient, _options, method, url);
        }

        /// <summary>
        /// Creates a custom HTTP request with typed request body
        /// </summary>
        protected HttpResourceRequest Request<TRequest>(HttpMethod method, string url, TRequest data)
        {
            return new HttpResourceRequest(_httpClient, _options, method, url, data);
        }

        /// <summary>
        /// Creates a request builder for complex scenarios
        /// </summary>
        protected RequestBuilder<TResponse> BuildRequest<TResponse>(string url)
        {
            return new RequestBuilder<TResponse>(_httpClient, _options, url);
        }
    }

    /// <summary>
    /// Fluent request builder for complex request scenarios
    /// </summary>
    public class RequestBuilder<TResponse>
    {
        private readonly HttpClient _httpClient;
        private readonly HttpResourceOptions _options;
        private readonly string _url;
        private HttpMethod _method = HttpMethod.Get;
        private object? _data;
        private readonly Dictionary<string, string> _headers = new();
        private readonly Dictionary<string, string> _queryParams = new();

        public RequestBuilder(HttpClient httpClient, HttpResourceOptions options, string url)
        {
            _httpClient = httpClient;
            _options = options;
            _url = url;
        }

        public RequestBuilder<TResponse> WithMethod(HttpMethod method)
        {
            _method = method;
            return this;
        }

        public RequestBuilder<TResponse> WithBody<TRequest>(TRequest data)
        {
            _data = data;
            return this;
        }

        public RequestBuilder<TResponse> WithHeader(string name, string value)
        {
            _headers[name] = value;
            return this;
        }

        public RequestBuilder<TResponse> WithQueryParam(string name, string value)
        {
            _queryParams[name] = value;
            return this;
        }

        public HttpResourceRequest<TResponse> Build()
        {
            var request = new HttpResourceRequest<TResponse>(_httpClient, _options, _method, _url, _data);
            
            foreach (var header in _headers)
                request.WithHeader(header.Key, header.Value);
            
            foreach (var param in _queryParams)
                request.WithQueryParam(param.Key, param.Value);
            
            return request;
        }
    }

    /// <summary>
    /// Represents a lazy HTTP resource request that can be executed later
    /// </summary>
    public class HttpResourceRequest
    {
        internal readonly HttpClient _httpClient;
        internal readonly HttpResourceOptions _options;
        internal readonly HttpMethod _method;
        internal readonly string _url;
        internal readonly object? _data;
        internal readonly Dictionary<string, string> _headers = new();
        internal readonly Dictionary<string, string> _queryParams = new();
        internal Action<HttpResource>? _configureResource;
        internal Action<HttpRequestMessage>? _configureRequest;

        public HttpResourceRequest(HttpClient httpClient, HttpResourceOptions options, HttpMethod method, string url, object? data = null)
        {
            _httpClient = httpClient;
            _options = options;
            _method = method;
            _url = url;
            _data = data;
        }

        /// <summary>
        /// Adds a header to the request
        /// </summary>
        public HttpResourceRequest WithHeader(string name, string value)
        {
            _headers[name] = value;
            return this;
        }

        /// <summary>
        /// Adds multiple headers to the request
        /// </summary>
        public HttpResourceRequest WithHeaders(Dictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                _headers[header.Key] = header.Value;
            }
            return this;
        }

        /// <summary>
        /// Adds a query parameter to the request
        /// </summary>
        public HttpResourceRequest WithQueryParam(string name, string value)
        {
            _queryParams[name] = value;
            return this;
        }

        /// <summary>
        /// Adds multiple query parameters to the request
        /// </summary>
        public HttpResourceRequest WithQueryParams(Dictionary<string, string> queryParams)
        {
            foreach (var param in queryParams)
            {
                _queryParams[param.Key] = param.Value;
            }
            return this;
        }

        /// <summary>
        /// Configures the HttpResource before executing the request
        /// </summary>
        public HttpResourceRequest ConfigureResource(Action<HttpResource> configure)
        {
            _configureResource = configure;
            return this;
        }

        /// <summary>
        /// Configures the HttpRequestMessage before sending
        /// </summary>
        public HttpResourceRequest ConfigureRequest(Action<HttpRequestMessage> configure)
        {
            _configureRequest = configure;
            return this;
        }

        /// <summary>
        /// Builds the final URL with query parameters
        /// </summary>
        internal string BuildUrl()
        {
            if (_queryParams.Count == 0)
                return _url;

            var queryString = string.Join("&", _queryParams.Select(kvp => 
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            
            return _url.Contains('?') ? $"{_url}&{queryString}" : $"{_url}?{queryString}";
        }

        /// <summary>
        /// Executes the request and returns the configured HttpResource
        /// </summary>
        public virtual async Task<HttpResource> ExecuteAsync()
        {
            var resource = new HttpResource(_httpClient, _options);
            
            // Apply resource configuration
            _configureResource?.Invoke(resource);
            
            // Apply headers to HttpClient
            var originalHeaders = new Dictionary<string, IEnumerable<string>>();
            foreach (var header in _headers)
            {
                if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                {
                    originalHeaders[header.Key] = _httpClient.DefaultRequestHeaders.GetValues(header.Key);
                    _httpClient.DefaultRequestHeaders.Remove(header.Key);
                }
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            try
            {
                await ExecuteOn(resource);
            }
            finally
            {
                // Restore original headers
                foreach (var header in _headers.Keys)
                {
                    _httpClient.DefaultRequestHeaders.Remove(header);
                }
                foreach (var header in originalHeaders)
                {
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            
            return resource;
        }

        /// <summary>
        /// Creates the HttpResource without executing the request
        /// </summary>
        public HttpResource CreateResource()
        {
            return new HttpResource(_httpClient, _options);
        }

        /// <summary>
        /// Configures and executes the request with custom options
        /// </summary>
        public async Task<HttpResource> ExecuteAsync(Action<HttpResource> configure)
        {
            _configureResource = configure;
            return await ExecuteAsync();
        }

        /// <summary>
        /// Executes the request on an existing resource
        /// </summary>
        protected virtual async Task ExecuteOn(HttpResource resource)
        {
            var url = BuildUrl();
            
            if (_method == HttpMethod.Get)
            {
                await resource.GetAsync(url);
            }
            else if (_method == HttpMethod.Post)
            {
                await resource.PostAsync(url, _data!);
            }
            else if (_method == HttpMethod.Put)
            {
                await resource.PutAsync(url, _data!);
            }
            else if (_method == HttpMethod.Delete)
            {
                await resource.DeleteAsync(url);
            }
            else if (_method == HttpMethod.Patch)
            {
                await resource.PatchAsync(url, _data!);
            }
            else
            {
                // Handle custom HTTP methods
                await resource.SendAsync(_method, url, _data);
            }
        }
    }

    /// <summary>
    /// Represents a lazy HTTP resource request with typed response
    /// </summary>
    public class HttpResourceRequest<T> : HttpResourceRequest
    {
        public HttpResourceRequest(HttpClient httpClient, HttpResourceOptions options, HttpMethod method, string url, object? data = null)
            : base(httpClient, options, method, url, data)
        {
        }

        /// <summary>
        /// Adds a header to the request
        /// </summary>
        public new HttpResourceRequest<T> WithHeader(string name, string value)
        {
            base.WithHeader(name, value);
            return this;
        }

        /// <summary>
        /// Adds multiple headers to the request
        /// </summary>
        public new HttpResourceRequest<T> WithHeaders(Dictionary<string, string> headers)
        {
            base.WithHeaders(headers);
            return this;
        }

        /// <summary>
        /// Adds a query parameter to the request
        /// </summary>
        public new HttpResourceRequest<T> WithQueryParam(string name, string value)
        {
            base.WithQueryParam(name, value);
            return this;
        }

        /// <summary>
        /// Adds multiple query parameters to the request
        /// </summary>
        public new HttpResourceRequest<T> WithQueryParams(Dictionary<string, string> queryParams)
        {
            base.WithQueryParams(queryParams);
            return this;
        }

        /// <summary>
        /// Configures the HttpResource before executing the request
        /// </summary>
        public new HttpResourceRequest<T> ConfigureResource(Action<HttpResource> configure)
        {
            base.ConfigureResource(configure);
            return this;
        }

        /// <summary>
        /// Configures the HttpRequestMessage before sending
        /// </summary>
        public new HttpResourceRequest<T> ConfigureRequest(Action<HttpRequestMessage> configure)
        {
            base.ConfigureRequest(configure);
            return this;
        }

        /// <summary>
        /// Executes the request on an existing resource with typed response
        /// </summary>
        protected override async Task ExecuteOn(HttpResource resource)
        {
            var url = BuildUrl();
            
            if (_method == HttpMethod.Get)
            {
                await resource.GetAsync<T>(url);
            }
            else if (_method == HttpMethod.Post)
            {
                await resource.PostAsync<object, T>(url, _data!);
            }
            else if (_method == HttpMethod.Put)
            {
                await resource.PutAsync<object, T>(url, _data!);
            }
            else if (_method == HttpMethod.Delete)
            {
                await resource.DeleteAsync<T>(url);
            }
            else if (_method == HttpMethod.Patch && _data != null)
            {
                await resource.PatchAsync<object, T>(url, _data);
            }
            else
            {
                // Handle custom HTTP methods
                await resource.SendAsync<T>(_method, url, _data);
            }
        }
    }
}