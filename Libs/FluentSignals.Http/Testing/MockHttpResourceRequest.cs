using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentSignals.Http.Options;
using FluentSignals.Http.Resources;
using FluentSignals.Http.Types;

namespace FluentSignals.Http.Testing
{
    /// <summary>
    /// Mock HTTP resource request for testing
    /// </summary>
    public class MockHttpResourceRequest<T> : HttpResourceRequest<T>
    {
        private readonly T? _mockData;
        private readonly HttpStatusCode _statusCode;
        private readonly Exception? _exception;
        private readonly TimeSpan _delay;

        /// <summary>
        /// Creates a successful mock request
        /// </summary>
        public MockHttpResourceRequest(T mockData, HttpStatusCode statusCode = HttpStatusCode.OK, TimeSpan? delay = null) 
            : base(new HttpClient(), new HttpResourceOptions(), HttpMethod.Get, "mock://test")
        {
            _mockData = mockData;
            _statusCode = statusCode;
            _delay = delay ?? TimeSpan.Zero;
        }

        /// <summary>
        /// Creates a failed mock request
        /// </summary>
        public MockHttpResourceRequest(Exception exception, TimeSpan? delay = null) 
            : base(new HttpClient(), new HttpResourceOptions(), HttpMethod.Get, "mock://test")
        {
            _exception = exception;
            _delay = delay ?? TimeSpan.Zero;
        }

        public override async Task<HttpResource> ExecuteAsync()
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay);
            }

            var resource = CreateResource();
            
            if (_exception != null)
            {
                resource.Error.Value = _exception;
            }
            else
            {
                // Create a mock response message to get headers
                using var responseMessage = new HttpResponseMessage(_statusCode);
                var headers = responseMessage.Headers;
                var content = System.Text.Json.JsonSerializer.Serialize(_mockData);
                resource.Value = new FluentSignals.Http.Types.HttpResponse<T>(_statusCode, headers, content, _mockData);
            }
            
            return resource;
        }
    }

    /// <summary>
    /// Mock HTTP resource request without typed response
    /// </summary>
    public class MockHttpResourceRequest : HttpResourceRequest
    {
        private readonly HttpStatusCode _statusCode;
        private readonly Exception? _exception;
        private readonly TimeSpan _delay;

        /// <summary>
        /// Creates a successful mock request
        /// </summary>
        public MockHttpResourceRequest(HttpStatusCode statusCode = HttpStatusCode.OK, TimeSpan? delay = null) 
            : base(new HttpClient(), new HttpResourceOptions(), HttpMethod.Get, "mock://test")
        {
            _statusCode = statusCode;
            _delay = delay ?? TimeSpan.Zero;
        }

        /// <summary>
        /// Creates a failed mock request
        /// </summary>
        public MockHttpResourceRequest(Exception exception, TimeSpan? delay = null) 
            : base(new HttpClient(), new HttpResourceOptions(), HttpMethod.Get, "mock://test")
        {
            _exception = exception;
            _delay = delay ?? TimeSpan.Zero;
        }

        public override async Task<HttpResource> ExecuteAsync()
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay);
            }

            var resource = CreateResource();
            
            if (_exception != null)
            {
                resource.Error.Value = _exception;
            }
            else
            {
                // Create a mock response message to get headers
                using var responseMessage = new HttpResponseMessage(_statusCode);
                var headers = responseMessage.Headers;
                resource.Value = new FluentSignals.Http.Types.HttpResponse(_statusCode, headers, string.Empty);
            }
            
            return resource;
        }
    }

    /// <summary>
    /// Extension methods for creating mock requests
    /// </summary>
    public static class MockExtensions
    {
        /// <summary>
        /// Creates a mock version of this request for testing
        /// </summary>
        public static MockHttpResourceRequest<T> AsMock<T>(this HttpResourceRequest<T> request, T data, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new MockHttpResourceRequest<T>(data, statusCode);
        }

        /// <summary>
        /// Creates a failed mock version of this request for testing
        /// </summary>
        public static MockHttpResourceRequest<T> AsMockError<T>(this HttpResourceRequest<T> request, Exception exception)
        {
            return new MockHttpResourceRequest<T>(exception);
        }
    }
}