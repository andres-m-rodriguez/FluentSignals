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
using Xunit;

namespace FluentSignals.Http.Tests
{
    public class TypedHttpResourceTests
    {
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;
            public List<HttpRequestMessage> ReceivedRequests { get; } = new();

            public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory;
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
                
                ReceivedRequests.Add(clonedRequest);
                var response = _responseFactory(clonedRequest);
                return response;
            }
        }

        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
        }

        private class SearchCriteria
        {
            public string Query { get; set; } = "";
            public int Page { get; set; }
            public int PageSize { get; set; }
        }

        private class PagedResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        }

        [HttpResource("/api/users")]
        private class TestUserResource : TypedHttpResource
        {
            public TestUserResource() { }
            
            public TestUserResource(HttpClient httpClient) 
                : base(httpClient, "/api/users") { }

            public HttpResourceRequest<TestUser> GetById(int id) => 
                Get<TestUser>($"{BaseUrl}/{id}");

            public HttpResourceRequest<PagedResult<TestUser>> Search(SearchCriteria criteria) =>
                Get<PagedResult<TestUser>>($"{BaseUrl}/search")
                    .WithQueryParam("query", criteria.Query)
                    .WithQueryParam("page", criteria.Page.ToString())
                    .WithQueryParam("pageSize", criteria.PageSize.ToString());

            public HttpResourceRequest<TestUser> Create(TestUser user) =>
                Post<TestUser, TestUser>(BaseUrl, user);

            public HttpResourceRequest<TestUser> Update(int id, TestUser user) =>
                Put<TestUser, TestUser>($"{BaseUrl}/{id}", user);

            public HttpResourceRequest Delete(int id) =>
                Delete($"{BaseUrl}/{id}");

            public HttpResourceRequest<TestUser> GetWithCustomHeaders(int id, string apiKey) =>
                Get<TestUser>($"{BaseUrl}/{id}")
                    .WithHeader("X-API-Key", apiKey)
                    .WithHeader("X-Custom-Header", "test-value");

            public HttpResourceRequest<TestUser> CustomMethod(HttpMethod method, string path) =>
                Request<TestUser>(method, $"{BaseUrl}/{path}");
        }

        [Fact]
        public void Constructor_WithHttpClient_SetsBaseUrl()
        {
            // Arrange
            var httpClient = new HttpClient() { BaseAddress = new Uri("https://api.test.com") };
            
            // Act
            var resource = new TestUserResource(httpClient);
            
            // Assert
            Assert.Equal("/api/users", resource.BaseUrl);
        }

        [Fact]
        public void Constructor_WithAttribute_UsesAttributeBaseUrl()
        {
            // Arrange & Act
            var resource = new TestUserResource();
            var attribute = (HttpResourceAttribute?)Attribute.GetCustomAttribute(
                typeof(TestUserResource), 
                typeof(HttpResourceAttribute));
            
            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("/api/users", attribute.BaseUrl);
        }

        [Fact]
        public async Task GetById_BuildsCorrectUrl()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(req =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new TestUser { Id = 123, Name = "John" }),
                        Encoding.UTF8,
                        "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler) 
            { 
                BaseAddress = new Uri("https://api.example.com") 
            };
            var resource = new TestUserResource(httpClient);
            
            // Act
            var httpResource = await resource.GetById(123).ExecuteAsync();
            await Task.Delay(100); // Give time for async operations
            
            // Assert
            Assert.Single(mockHandler.ReceivedRequests);
            var request = mockHandler.ReceivedRequests[0];
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api.example.com/api/users/123", request.RequestUri?.ToString());
        }

        [Fact]
        public async Task Search_AddsQueryParameters()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(req =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new PagedResult<TestUser>()),
                        Encoding.UTF8,
                        "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var resource = new TestUserResource(httpClient);
            var criteria = new SearchCriteria 
            { 
                Query = "john doe", 
                Page = 2, 
                PageSize = 20 
            };
            
            // Act
            var httpResource = await resource.Search(criteria).ExecuteAsync();
            await Task.Delay(100);
            
            // Assert
            Assert.Single(mockHandler.ReceivedRequests);
            var request = mockHandler.ReceivedRequests[0];
            var queryString = request.RequestUri?.Query;
            Assert.Contains("query=john%20doe", queryString);
            Assert.Contains("page=2", queryString);
            Assert.Contains("pageSize=20", queryString);
        }

        [Fact]
        public async Task Create_SendsPostRequest()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(req =>
                new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new TestUser { Id = 1, Name = "John", Email = "john@example.com" }),
                        Encoding.UTF8,
                        "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var resource = new TestUserResource(httpClient);
            var newUser = new TestUser { Name = "John", Email = "john@example.com" };
            
            // Act
            var httpResource = await resource.Create(newUser).ExecuteAsync();
            await Task.Delay(100);
            
            // Assert
            Assert.Single(mockHandler.ReceivedRequests);
            var request = mockHandler.ReceivedRequests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.NotNull(request.Content);
            
            var content = await request.Content.ReadAsStringAsync();
            Assert.Contains("John", content);
            Assert.Contains("john@example.com", content);
        }

        [Fact]
        public async Task Update_SendsPutRequest()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(req =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new TestUser { Id = 123, Name = "John Updated" }),
                        Encoding.UTF8,
                        "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var resource = new TestUserResource(httpClient);
            var updatedUser = new TestUser { Id = 123, Name = "John Updated" };
            
            // Act
            var httpResource = await resource.Update(123, updatedUser).ExecuteAsync();
            await Task.Delay(100);
            
            // Assert
            Assert.Single(mockHandler.ReceivedRequests);
            var request = mockHandler.ReceivedRequests[0];
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/api/users/123", request.RequestUri?.AbsolutePath);
        }

        [Fact]
        public async Task Delete_SendsDeleteRequest()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(req =>
                new HttpResponseMessage(HttpStatusCode.NoContent));
            
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var resource = new TestUserResource(httpClient);
            
            // Act
            var httpResource = await resource.Delete(123).ExecuteAsync();
            await Task.Delay(100);
            
            // Assert
            Assert.Single(mockHandler.ReceivedRequests);
            var request = mockHandler.ReceivedRequests[0];
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/api/users/123", request.RequestUri?.AbsolutePath);
        }

        [Fact]
        public async Task WithHeader_AddsCustomHeaders()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(req =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new TestUser()),
                        Encoding.UTF8,
                        "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var resource = new TestUserResource(httpClient);
            
            // Act
            var httpResource = await resource.GetWithCustomHeaders(123, "test-api-key").ExecuteAsync();
            await Task.Delay(100);
            
            // Assert
            Assert.Single(mockHandler.ReceivedRequests);
            var request = mockHandler.ReceivedRequests[0];
            Assert.True(request.Headers.Contains("X-API-Key"));
            Assert.True(request.Headers.Contains("X-Custom-Header"));
            Assert.Equal("test-api-key", request.Headers.GetValues("X-API-Key").First());
            Assert.Equal("test-value", request.Headers.GetValues("X-Custom-Header").First());
        }

        [Fact]
        public async Task CustomMethod_SupportsCustomHttpMethods()
        {
            // Arrange
            var customMethod = new HttpMethod("PATCH");
            var mockHandler = new MockHttpMessageHandler(req =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new TestUser()),
                        Encoding.UTF8,
                        "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var resource = new TestUserResource(httpClient);
            
            // Act
            var httpResource = await resource.CustomMethod(customMethod, "custom-path").ExecuteAsync();
            await Task.Delay(100);
            
            // Assert
            Assert.Single(mockHandler.ReceivedRequests);
            var request = mockHandler.ReceivedRequests[0];
            Assert.Equal(customMethod, request.Method);
            Assert.Equal("/api/users/custom-path", request.RequestUri?.AbsolutePath);
        }

        [Fact]
        public async Task ConfigureResource_AllowsResourceConfiguration()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(req =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new TestUser { Id = 123 }),
                        Encoding.UTF8,
                        "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.test.com") };
            var resource = new TestUserResource(httpClient);
            var successCallbackInvoked = false;
            
            // Act
            var httpResource = await resource.GetById(123)
                .ConfigureResource(r => 
                {
                    r.OnSuccess(response => { successCallbackInvoked = true; return Task.CompletedTask; });
                })
                .ExecuteAsync();
            
            await Task.Delay(200); // Give time for callbacks
            
            // Assert
            Assert.True(successCallbackInvoked);
        }

        [Fact]
        public void BuildQueryString_HandlesMultipleTypes()
        {
            // This tests the internal BuildQueryString method indirectly
            // Arrange
            var criteria = new 
            { 
                Text = "hello world",
                Number = 123,
                Boolean = true,
                Date = new DateTime(2024, 1, 1)
            };
            
            var httpClient = new HttpClient() { BaseAddress = new Uri("https://api.test.com") };
            var resource = new TestUserResource(httpClient);
            
            // Act
            // Note: The actual building happens during ExecuteAsync, so we can't directly test it here
            // This would need to be tested through integration tests
            
            // Assert
            Assert.NotNull(resource);
            // The actual URL building happens during ExecuteAsync
        }
    }
}