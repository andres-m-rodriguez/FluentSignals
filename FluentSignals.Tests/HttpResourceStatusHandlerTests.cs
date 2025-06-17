using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluentSignals.Options.HttpResource;
using FluentSignals.Blazor.Extensions;
using RichardSzalay.MockHttp;
using Xunit;

namespace FluentSignals.Tests;

public class HttpResourceStatusHandlerTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly HttpResource _resource;
    
    public HttpResourceStatusHandlerTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://api.test.com/");
        _resource = new HttpResource(_httpClient);
    }
    
    public void Dispose()
    {
        _resource?.Dispose();
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
    }
    
    [Fact]
    public async Task OnSuccess_Should_Be_Called_For_200_OK()
    {
        // Arrange
        var responseData = new TestResponse { Id = 1, Name = "Test" };
        var json = JsonSerializer.Serialize(responseData);
        
        _mockHttp.When("https://api.test.com/test")
            .Respond("application/json", json);
        
        var successCalled = false;
        TestResponse? capturedData = null;
        
        _resource.OnSuccess<TestResponse>(data =>
        {
            successCalled = true;
            capturedData = data;
            return Task.CompletedTask;
        });
        
        // Act
        await _resource.GetAsync<TestResponse>("test");
        
        // Assert
        successCalled.Should().BeTrue();
        capturedData.Should().NotBeNull();
        capturedData!.Id.Should().Be(1);
        capturedData.Name.Should().Be("Test");
    }
    
    [Fact]
    public async Task OnNotFound_Should_Be_Called_For_404()
    {
        // Arrange
        var errorData = new ErrorResponse { Message = "Resource not found", Code = "NOT_FOUND" };
        var json = JsonSerializer.Serialize(errorData);
        
        _mockHttp.When("https://api.test.com/test")
            .Respond(HttpStatusCode.NotFound, "application/json", json);
        
        var notFoundCalled = false;
        ErrorResponse? capturedError = null;
        
        _resource.OnNotFound<ErrorResponse>(error =>
        {
            notFoundCalled = true;
            capturedError = error;
            return Task.CompletedTask;
        });
        
        // Act
        await _resource.GetAsync("test");
        
        // Assert
        notFoundCalled.Should().BeTrue();
        capturedError.Should().NotBeNull();
        capturedError!.Message.Should().Be("Resource not found");
        capturedError.Code.Should().Be("NOT_FOUND");
    }
    
    [Fact]
    public async Task OnBadRequest_Should_Be_Called_For_400_With_Predicate()
    {
        // Arrange
        var errorData1 = new ErrorResponse { Message = "Validation failed", Code = "VALIDATION_ERROR" };
        var errorData2 = new ErrorResponse { Message = "Account locked", Code = "AUTH004" };
        
        _mockHttp.When("https://api.test.com/test1")
            .Respond(HttpStatusCode.BadRequest, "application/json", JsonSerializer.Serialize(errorData1));
            
        _mockHttp.When("https://api.test.com/test2")
            .Respond(HttpStatusCode.BadRequest, "application/json", JsonSerializer.Serialize(errorData2));
        
        var auth004Called = false;
        var validationErrorCalled = false;
        
        _resource.OnBadRequest<ErrorResponse>(error =>
        {
            auth004Called = true;
            return Task.CompletedTask;
        }, error => error.Code == "AUTH004");
        
        _resource.OnBadRequest<ErrorResponse>(error =>
        {
            validationErrorCalled = true;
            return Task.CompletedTask;
        }, error => error.Code == "VALIDATION_ERROR");
        
        // Act
        await _resource.GetAsync("test1");
        await _resource.GetAsync("test2");
        
        // Assert
        validationErrorCalled.Should().BeTrue();
        auth004Called.Should().BeTrue();
    }
    
    [Fact]
    public async Task OnUnauthorized_Should_Be_Called_For_401()
    {
        // Arrange
        var errorData = new ErrorResponse { Message = "Unauthorized", Code = "UNAUTHORIZED" };
        var json = JsonSerializer.Serialize(errorData);
        
        _mockHttp.When("https://api.test.com/test")
            .Respond(HttpStatusCode.Unauthorized, "application/json", json);
        
        var unauthorizedCalled = false;
        
        _resource.OnUnauthorized(response =>
        {
            unauthorizedCalled = true;
            return Task.CompletedTask;
        });
        
        // Act
        await _resource.GetAsync("test");
        
        // Assert
        unauthorizedCalled.Should().BeTrue();
    }
    
    [Fact]
    public async Task OnServerError_Should_Be_Called_For_500_Errors()
    {
        // Arrange
        _mockHttp.When("https://api.test.com/test500")
            .Respond(HttpStatusCode.InternalServerError);
            
        _mockHttp.When("https://api.test.com/test503")
            .Respond(HttpStatusCode.ServiceUnavailable);
        
        var serverErrorCount = 0;
        
        _resource.OnServerError(response =>
        {
            serverErrorCount++;
            return Task.CompletedTask;
        });
        
        // Act
        await _resource.GetAsync("test500");
        await _resource.GetAsync("test503");
        
        // Assert
        serverErrorCount.Should().Be(2);
    }
    
    [Fact]
    public async Task Multiple_Handlers_Should_Be_Called_For_Same_Status()
    {
        // Arrange
        var responseData = new TestResponse { Id = 1, Name = "Test" };
        var json = JsonSerializer.Serialize(responseData);
        
        _mockHttp.When("https://api.test.com/test")
            .Respond("application/json", json);
        
        var handler1Called = false;
        var handler2Called = false;
        
        _resource.OnSuccess(response =>
        {
            handler1Called = true;
            return Task.CompletedTask;
        });
        
        _resource.OnSuccess<TestResponse>(data =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        });
        
        // Act
        await _resource.GetAsync<TestResponse>("test");
        
        // Assert
        handler1Called.Should().BeTrue();
        handler2Called.Should().BeTrue();
    }
    
    [Fact]
    public async Task OnStatusCode_Should_Work_For_Custom_Status()
    {
        // Arrange
        _mockHttp.When("https://api.test.com/test")
            .Respond(HttpStatusCode.TooManyRequests);
        
        var rateLimitCalled = false;
        
        _resource.OnStatusCode(HttpStatusCode.TooManyRequests, response =>
        {
            rateLimitCalled = true;
            return Task.CompletedTask;
        });
        
        // Act
        await _resource.GetAsync("test");
        
        // Assert
        rateLimitCalled.Should().BeTrue();
    }
    
    [Fact]
    public async Task POST_Request_Should_Trigger_Success_Handler()
    {
        // Arrange
        var requestData = new TestRequest { Value = "test" };
        var responseData = new TestResponse { Id = 1, Name = "Created" };
        var json = JsonSerializer.Serialize(responseData);
        
        _mockHttp.When(HttpMethod.Post, "https://api.test.com/test")
            .Respond(HttpStatusCode.Created, "application/json", json);
        
        var successCalled = false;
        TestResponse? capturedData = null;
        
        _resource.OnSuccess<TestResponse>(data =>
        {
            successCalled = true;
            capturedData = data;
            return Task.CompletedTask;
        });
        
        // Act
        await _resource.PostAsync<TestRequest, TestResponse>("test", requestData);
        
        // Assert
        successCalled.Should().BeTrue();
        capturedData.Should().NotBeNull();
        capturedData!.Name.Should().Be("Created");
        _resource.LastStatusCode.Value.Should().Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task Handler_Should_Not_Be_Called_When_Type_Does_Not_Match()
    {
        // Arrange
        var errorData = new ErrorResponse { Message = "Not found", Code = "404" };
        var json = JsonSerializer.Serialize(errorData);
        
        _mockHttp.When("https://api.test.com/test")
            .Respond(HttpStatusCode.NotFound, "application/json", json);
        
        var wrongTypeCalled = false;
        
        // Register handler for different type
        _resource.OnNotFound<TestResponse>(data =>
        {
            wrongTypeCalled = true;
            return Task.CompletedTask;
        });
        
        // Act
        await _resource.GetAsync("test");
        
        // Assert
        wrongTypeCalled.Should().BeFalse();
    }
    
    [Fact]
    public async Task Loading_State_Should_Be_Updated_During_Request()
    {
        // Arrange
        var loadingStates = new List<bool>();
        using var subscription = _resource.IsLoading.SubscribeDisposable(isLoading => loadingStates.Add(isLoading));
        
        _mockHttp.When("https://api.test.com/test")
            .Respond("application/json", "{}");
        
        // Act
        await _resource.GetAsync("test");
        
        // Assert
        loadingStates.Should().Contain(true);  // Should have been loading
        loadingStates.Should().Contain(false); // Should have finished loading
        _resource.IsLoading.Value.Should().BeFalse();
    }
    
    [Fact]
    public async Task TypedHandler_ShouldOnlyBeCalledOnce_NotTwice()
    {
        // Arrange
        var errorData = new UserAlreadyExistsError 
        { 
            ConflictType = "Username", 
            Message = "Username already exists" 
        };
        var json = JsonSerializer.Serialize(errorData);
        
        _mockHttp.When(HttpMethod.Post, "https://api.test.com/register")
            .Respond(HttpStatusCode.Conflict, "application/json", json);
        
        var callCount = 0;
        
        _resource.OnConflict<UserAlreadyExistsError>(error =>
        {
            callCount++;
            return Task.CompletedTask;
        });
        
        // Act
        await _resource.PostAsync("register", new { username = "test" });
        
        // Assert
        callCount.Should().Be(1, "typed handler should only be called once, not twice");
    }
    
    [Fact]
    public async Task TypedAndNonTypedHandlers_ShouldBothBeCalled_ButOnlyOnce()
    {
        // Arrange
        var errorData = new UserAlreadyExistsError 
        { 
            ConflictType = "Email", 
            Message = "Email already exists" 
        };
        var json = JsonSerializer.Serialize(errorData);
        
        _mockHttp.When(HttpMethod.Post, "https://api.test.com/register")
            .Respond(HttpStatusCode.Conflict, "application/json", json);
        
        var typedCallCount = 0;
        var nonTypedCallCount = 0;
        
        // Register both typed and non-typed handlers
        _resource
            .OnConflict<UserAlreadyExistsError>(error =>
            {
                typedCallCount++;
                return Task.CompletedTask;
            })
            .OnConflict(response =>
            {
                nonTypedCallCount++;
                return Task.CompletedTask;
            });
        
        // Act
        await _resource.PostAsync("register", new { email = "test@test.com" });
        
        // Assert
        typedCallCount.Should().Be(1, "typed handler should be called exactly once");
        nonTypedCallCount.Should().Be(1, "non-typed handler should be called exactly once");
    }
    
    [Fact]
    public async Task TypedHandler_WithGetAsync_ShouldOnlyBeCalledOnce()
    {
        // Arrange
        var errorData = new UserAlreadyExistsError 
        { 
            ConflictType = "Username", 
            Message = "Username already exists" 
        };
        var json = JsonSerializer.Serialize(errorData);
        
        _mockHttp.When("https://api.test.com/check-username")
            .Respond(HttpStatusCode.Conflict, "application/json", json);
        
        var callCount = 0;
        
        _resource.OnConflict<UserAlreadyExistsError>(error =>
        {
            callCount++;
            return Task.CompletedTask;
        });
        
        // Act - Use typed GetAsync
        await _resource.GetAsync<object>("check-username");
        
        // Assert
        callCount.Should().Be(1, "typed handler should only be called once even with typed GetAsync");
    }
    
    // Test models
    private class TestRequest
    {
        public string Value { get; set; } = string.Empty;
    }
    
    private class TestResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    private class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
    
    private class UserAlreadyExistsError
    {
        public string ConflictType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}