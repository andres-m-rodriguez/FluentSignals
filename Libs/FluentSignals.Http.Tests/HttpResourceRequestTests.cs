using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentSignals.Http.Resources;
using FluentSignals.Http.Options;
using FluentSignals.Http.Types;
using Xunit;

namespace FluentSignals.Http.Tests
{
    public class HttpResourceRequestTests
    {
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;
            public HttpRequestMessage? LastRequest { get; private set; }

            public MockHttpMessageHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Clone the request to prevent disposal issues
                var clonedRequest = new HttpRequestMessage(request.Method, request.RequestUri)
                {
                    Version = request.Version
                };
                
                foreach (var header in request.Headers)
                {
                    clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                
                if (request.Content != null)
                {
                    var content = await request.Content.ReadAsStringAsync();
                    clonedRequest.Content = new StringContent(content, Encoding.UTF8, request.Content.Headers.ContentType?.MediaType ?? "application/json");
                }
                
                LastRequest = clonedRequest;
                return _response;
            }
        }

        private class TestData
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
        }

        [Fact]
        public void WithHeader_AddsHeaderToRequest()
        {
            // Arrange
            var httpClient = new HttpClient();
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test");
            
            // Act
            var result = request.WithHeader("X-Custom-Header", "test-value");
            
            // Assert
            Assert.Same(request, result); // Should return same instance for chaining
        }

        [Fact]
        public void WithHeaders_AddsMultipleHeaders()
        {
            // Arrange
            var httpClient = new HttpClient();
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test");
            var headers = new Dictionary<string, string>
            {
                { "X-Header-1", "value1" },
                { "X-Header-2", "value2" },
                { "X-Header-3", "value3" }
            };
            
            // Act
            var result = request.WithHeaders(headers);
            
            // Assert
            Assert.Same(request, result);
        }

        [Fact]
        public void WithQueryParam_AddsQueryParameter()
        {
            // Arrange
            var httpClient = new HttpClient();
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test");
            
            // Act
            var result = request.WithQueryParam("search", "test query");
            
            // Assert
            Assert.Same(request, result);
        }

        [Fact]
        public void WithQueryParams_AddsMultipleQueryParameters()
        {
            // Arrange
            var httpClient = new HttpClient();
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test");
            var queryParams = new Dictionary<string, string>
            {
                { "page", "1" },
                { "size", "10" },
                { "sort", "name" }
            };
            
            // Act
            var result = request.WithQueryParams(queryParams);
            
            // Assert
            Assert.Same(request, result);
        }

        [Fact]
        public async Task BuildUrl_ConstructsUrlWithQueryParameters()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test");
            
            // Act
            await request
                .WithQueryParam("search", "hello world")
                .WithQueryParam("page", "2")
                .WithQueryParam("active", "true")
                .ExecuteAsync();
            
            await Task.Delay(100);
            
            // Assert
            var sentRequest = mockHandler.LastRequest;
            Assert.NotNull(sentRequest);
            var query = sentRequest.RequestUri?.Query;
            Assert.Contains("search=hello%20world", query);
            Assert.Contains("page=2", query);
            Assert.Contains("active=true", query);
        }

        [Fact]
        public async Task BuildUrl_HandlesExistingQueryString()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test?existing=param");
            
            // Act
            await request
                .WithQueryParam("new", "value")
                .ExecuteAsync();
            
            await Task.Delay(100);
            
            // Assert
            var sentRequest = mockHandler.LastRequest;
            Assert.NotNull(sentRequest);
            var query = sentRequest.RequestUri?.Query;
            Assert.Contains("existing=param", query);
            Assert.Contains("new=value", query);
        }

        [Fact]
        public void ConfigureResource_SetsConfiguration()
        {
            // Arrange
            var httpClient = new HttpClient();
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test");
            var configurationCalled = false;
            
            // Act
            var result = request.ConfigureResource(r => configurationCalled = true);
            
            // Assert
            Assert.Same(request, result);
            // Configuration is called during ExecuteAsync, not immediately
            Assert.False(configurationCalled);
        }

        [Fact]
        public async Task ExecuteAsync_AppliesHeaders()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test");
            
            // Act
            await request
                .WithHeader("X-API-Key", "secret")
                .WithHeader("X-Request-ID", "12345")
                .ExecuteAsync();
            
            await Task.Delay(100);
            
            // Assert
            var sentRequest = mockHandler.LastRequest;
            Assert.NotNull(sentRequest);
            // Headers should be cleaned up after request
            Assert.False(httpClient.DefaultRequestHeaders.Contains("X-API-Key"));
            Assert.False(httpClient.DefaultRequestHeaders.Contains("X-Request-ID"));
        }

        [Fact]
        public async Task ExecuteAsync_CallsResourceConfiguration()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new TestData { Name = "Test", Value = 42 }),
                        Encoding.UTF8,
                        "application/json")
                });
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test");
            var configurationCalled = false;
            HttpResource? configuredResource = null;
            
            // Act
            var resource = await request.ConfigureResource(r => 
            {
                configurationCalled = true;
                configuredResource = r;
            }).ExecuteAsync();
            
            // Assert
            Assert.True(configurationCalled);
            Assert.NotNull(configuredResource);
            Assert.Same(resource, configuredResource);
        }

        [Fact]
        public async Task TypedRequest_ExecutesWithCorrectType()
        {
            // Arrange
            var responseData = new TestData { Name = "Response", Value = 100 };
            var mockHandler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(responseData),
                        Encoding.UTF8,
                        "application/json")
                });
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var options = new HttpResourceOptions();
            var request = new HttpResourceRequest<TestData>(httpClient, options, HttpMethod.Get, "/test");
            
            // Act
            var resource = await request.ExecuteAsync();
            await Task.Delay(200); // Wait for async processing
            
            // Assert
            Assert.NotNull(resource.Value);
            var response = resource.Value as HttpResponse<TestData>;
            Assert.NotNull(response);
            Assert.NotNull(response.Data);
            Assert.Equal("Response", response.Data.Name);
            Assert.Equal(100, response.Data.Value);
        }

        [Fact]
        public void RequestBuilder_ChainsMethods()
        {
            // Arrange
            var httpClient = new HttpClient();
            var options = new HttpResourceOptions();
            var builder = new RequestBuilder<TestData>(httpClient, options, "/test");
            
            // Act
            var request = builder
                .WithMethod(HttpMethod.Post)
                .WithBody(new TestData { Name = "Test", Value = 42 })
                .WithHeader("Content-Type", "application/json")
                .WithQueryParam("version", "2")
                .Build();
            
            // Assert
            Assert.NotNull(request);
        }

        [Fact]
        public async Task RequestBuilder_BuildsCorrectRequest()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var options = new HttpResourceOptions();
            var builder = new RequestBuilder<TestData>(httpClient, options, "/test");
            var requestData = new TestData { Name = "Test", Value = 42 };
            
            // Act
            var request = builder
                .WithMethod(HttpMethod.Post)
                .WithBody(requestData)
                .WithHeader("X-Custom", "value")
                .WithQueryParam("debug", "true")
                .Build();
            
            await request.ExecuteAsync();
            await Task.Delay(100);
            
            // Assert
            var sentRequest = mockHandler.LastRequest;
            Assert.NotNull(sentRequest);
            Assert.Equal(HttpMethod.Post, sentRequest.Method);
            Assert.Contains("debug=true", sentRequest.RequestUri?.Query);
            Assert.NotNull(sentRequest.Content);
            
            var content = await sentRequest.Content.ReadAsStringAsync();
            Assert.Contains("Test", content);
            Assert.Contains("42", content);
        }
    }
}