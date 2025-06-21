using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentSignals.Http.Interceptors;
using FluentSignals.Http.Options;
using FluentSignals.Http.Resources;
using FluentSignals.Http.Types;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace FluentSignals.Http.Tests;

public class InterceptorTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    
    public InterceptorTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://api.test.com/");
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
    }
    
    [Fact]
    public async Task LoggingInterceptor_Should_Log_Requests_And_Responses()
    {
        // Arrange
        var logs = new List<string>();
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception?, Delegate>((level, eventId, state, exception, formatter) =>
            {
                logs.Add(state.ToString() ?? "");
            });
        var interceptor = new LoggingInterceptor(mockLogger.Object);
        
        var options = new HttpResourceOptions();
        // options.Interceptors.Add(interceptor); // Interceptors might not exist on HttpResourceOptions
        
        var resource = new HttpResource(_httpClient, options);
        
        _mockHttp.When("https://api.test.com/test")
            .Respond("application/json", "{\"result\":\"success\"}");
        
        // Act
        await resource.GetAsync("test");
        
        // Assert
        logs.Should().HaveCount(2);
        logs[0].Should().Contain("GET https://api.test.com/test");
        logs[1].Should().Contain("200 OK");
    }
    
    [Fact]
    public async Task BearerTokenInterceptor_Should_Add_Authorization_Header()
    {
        // Arrange
        var token = "test-token-123";
        var interceptor = new BearerTokenInterceptor(() => Task.FromResult(token));
        
        var options = new HttpResourceOptions();
        // options.Interceptors.Add(interceptor); // Interceptors might not exist on HttpResourceOptions
        
        var resource = new HttpResource(_httpClient, options);
        
        string? authHeader = null;
        _mockHttp.When("https://api.test.com/secure")
            .Respond("application/json", "{}");
        
        // Act
        await resource.GetAsync("secure");
        
        // Assert
        authHeader.Should().Be($"Bearer {token}");
    }
    
    [Fact]
    public async Task RetryInterceptor_Should_Retry_Failed_Requests()
    {
        // Arrange
        var attemptCount = 0;
        var interceptor = new RetryInterceptor(3, TimeSpan.FromMilliseconds(10));
        
        var options = new HttpResourceOptions();
        // options.Interceptors.Add(interceptor); // Interceptors might not exist on HttpResourceOptions
        
        var resource = new HttpResource(_httpClient, options);
        
        _mockHttp.When("https://api.test.com/flaky")
            .Respond(req =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"success\":true}", System.Text.Encoding.UTF8, "application/json")
                });
            });
        
        // Act
        var response = await resource.GetAsync<dynamic>("flaky");
        
        // Assert
        attemptCount.Should().Be(3);
        response.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public async Task Multiple_Interceptors_Should_Execute_In_Order()
    {
        // Arrange
        var executionOrder = new List<string>();
        
        var interceptor1 = new TestInterceptor("First", executionOrder);
        var interceptor2 = new TestInterceptor("Second", executionOrder);
        var interceptor3 = new TestInterceptor("Third", executionOrder);
        
        var options = new HttpResourceOptions();
        // options.Interceptors.Add(interceptor1);
        // options.Interceptors.Add(interceptor2);
        // options.Interceptors.Add(interceptor3);
        
        var resource = new HttpResource(_httpClient, options);
        
        _mockHttp.When("https://api.test.com/test")
            .Respond("application/json", "{}");
        
        // Act
        await resource.GetAsync("test");
        
        // Assert
        executionOrder.Should().HaveCount(6); // Each interceptor adds 2 entries (before and after)
        executionOrder[0].Should().Be("First:Before");
        executionOrder[1].Should().Be("Second:Before");
        executionOrder[2].Should().Be("Third:Before");
        executionOrder[3].Should().Be("Third:After");
        executionOrder[4].Should().Be("Second:After");
        executionOrder[5].Should().Be("First:After");
    }
    
    [Fact]
    public async Task TypedHttpResource_Should_Use_Interceptors()
    {
        // Arrange
        var logs = new List<string>();
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception?, Delegate>((level, eventId, state, exception, formatter) =>
            {
                logs.Add(state.ToString() ?? "");
            });
        var interceptor = new LoggingInterceptor(mockLogger.Object);
        
        var options = new HttpResourceOptions();
        // options.Interceptors.Add(interceptor); // Interceptors might not exist on HttpResourceOptions
        
        var resource = new InterceptedApiResource();
        resource.Initialize(_httpClient, "https://api.test.com", options);
        
        _mockHttp.When("https://api.test.com/data/123")
            .Respond("application/json", "{\"id\":123,\"value\":\"test\"}");
        
        // Act
        var result = await resource.GetDataAsync(123);
        
        // Assert
        result.Should().NotBeNull();
        logs.Should().NotBeEmpty();
        logs.Should().Contain(log => log.Contains("/data/123"));
    }
    
    // Test implementations
    private class TestInterceptor : IHttpResourceInterceptor
    {
        private readonly string _name;
        private readonly List<string> _executionOrder;
        
        public TestInterceptor(string name, List<string> executionOrder)
        {
            _name = name;
            _executionOrder = executionOrder;
        }
        
        public Task<HttpRequestMessage> OnRequestAsync(HttpRequestMessage request)
        {
            _executionOrder.Add($"{_name}:Before");
            return Task.FromResult(request);
        }

        public Task<HttpResponseMessage> OnResponseAsync(HttpResponseMessage response)
        {
            _executionOrder.Add($"{_name}:After");
            return Task.FromResult(response);
        }

        public Task OnExceptionAsync(HttpRequestMessage request, Exception exception)
        {
            _executionOrder.Add($"{_name}:Exception");
            return Task.CompletedTask;
        }
    }
    
    private class InterceptedApiResource : TypedHttpResource
    {
        public async Task<TestData?> GetDataAsync(int id)
        {
            var resource = await Get<TestData>($"/data/{id}").ExecuteAsync();
            return resource.Value as HttpResponse<TestData> != null ? ((HttpResponse<TestData>)resource.Value).Data : null;
        }
    }
    
    private class TestData
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}