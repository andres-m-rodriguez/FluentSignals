# FluentSignals.Http - AI Guide

This guide helps AI assistants understand and use the FluentSignals.Http library, which provides reactive HTTP client functionality with typed resources, interceptors, and caching.

## Overview

FluentSignals.Http extends FluentSignals with:
- **HttpResource** - Reactive HTTP client wrapper
- **TypedHttpResource** - Base class for strongly-typed API clients
- **Interceptors** - Cross-cutting concerns (auth, logging, retry)
- **Caching** - Multiple cache provider support
- **Request/Response handling** - Status-based handlers

## Core Components

### 1. HttpResource

```csharp
// Create HttpResource
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };
var resource = new HttpResource(httpClient);

// Make requests
var response = await resource.GetAsync<User>("/users/1");
if (response.IsSuccess)
{
    var user = response.Data;
}

// Access reactive properties
resource.IsLoading.Subscribe(loading => Console.WriteLine($"Loading: {loading}"));
resource.Error.Subscribe(error => Console.WriteLine($"Error: {error?.Message}"));
```

### 2. HttpResourceOptions

```csharp
var options = new HttpResourceOptions
{
    BaseUrl = "https://api.example.com",
    Timeout = TimeSpan.FromSeconds(30),
    DefaultHeaders = new Dictionary<string, string>
    {
        ["X-API-Version"] = "1.0",
        ["User-Agent"] = "MyApp/1.0"
    },
    RetryOptions = new RetryOptions
    {
        MaxRetries = 3,
        DelayMilliseconds = 1000,
        MaxDelayMilliseconds = 30000,
        BackoffMultiplier = 2
    },
    JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }
};

var resource = new HttpResource(httpClient, options);
```

### 3. TypedHttpResource

```csharp
public class UserApiResource : TypedHttpResource
{
    // GET /users
    public async Task<List<User>?> GetUsersAsync()
    {
        var response = await Get<List<User>>("/users").ExecuteAsync();
        return response.Data;
    }
    
    // GET /users/{id}
    public async Task<User?> GetUserAsync(int id)
    {
        var response = await Get<User>($"/users/{id}").ExecuteAsync();
        return response.Data;
    }
    
    // POST /users
    public async Task<User?> CreateUserAsync(CreateUserDto dto)
    {
        var response = await Post<CreateUserDto, User>("/users", dto).ExecuteAsync();
        return response.Data;
    }
    
    // PUT /users/{id}
    public async Task<User?> UpdateUserAsync(int id, UpdateUserDto dto)
    {
        var response = await Put<UpdateUserDto, User>($"/users/{id}", dto).ExecuteAsync();
        return response.Data;
    }
    
    // DELETE /users/{id}
    public async Task<bool> DeleteUserAsync(int id)
    {
        var response = await Delete($"/users/{id}").ExecuteAsync();
        return response.IsSuccess;
    }
    
    // PATCH /users/{id}
    public async Task<User?> PatchUserAsync(int id, JsonPatchDocument<User> patch)
    {
        var response = await Patch<JsonPatchDocument<User>, User>($"/users/{id}", patch)
            .ExecuteAsync();
        return response.Data;
    }
}
```

### 4. HttpResourceRequest Fluent API

```csharp
public class OrderApiResource : TypedHttpResource
{
    public async Task<Order?> GetOrderAsync(int id, string apiKey)
    {
        var response = await Get<Order>($"/orders/{id}")
            .WithHeader("X-API-Key", apiKey)
            .WithHeader("Accept", "application/json")
            .WithQueryParam("include", "items,customer")
            .WithTimeout(TimeSpan.FromSeconds(10))
            .OnSuccess(order => Console.WriteLine($"Order {order.Id} loaded"))
            .OnNotFound(_ => Console.WriteLine("Order not found"))
            .OnError(ex => Console.WriteLine($"Error: {ex.Message}"))
            .ExecuteAsync();
            
        return response.Data;
    }
}
```

## Status Handlers

### Global Status Handlers

```csharp
var resource = new HttpResource(httpClient);

// Configure global handlers
resource
    .OnSuccess(response => 
    {
        Console.WriteLine("Request successful");
        return Task.CompletedTask;
    })
    .OnSuccess<User>(user => 
    {
        Console.WriteLine($"User loaded: {user.Name}");
        return Task.CompletedTask;
    })
    .OnBadRequest<ValidationError>(error =>
    {
        Console.WriteLine($"Validation failed: {string.Join(", ", error.Errors)}");
        return Task.CompletedTask;
    })
    .OnUnauthorized(async response =>
    {
        // Refresh token or redirect to login
        await RefreshAuthToken();
    })
    .OnForbidden(response =>
    {
        Console.WriteLine("Access denied");
        return Task.CompletedTask;
    })
    .OnNotFound(response =>
    {
        Console.WriteLine("Resource not found");
        return Task.CompletedTask;
    })
    .OnConflict<ConflictError>(error =>
    {
        Console.WriteLine($"Conflict: {error.Message}");
        return Task.CompletedTask;
    })
    .OnServerError(response =>
    {
        Console.WriteLine($"Server error: {response.StatusCode}");
        return Task.CompletedTask;
    })
    .OnStatusCode(HttpStatusCode.TooManyRequests, response =>
    {
        Console.WriteLine("Rate limit exceeded");
        return Task.CompletedTask;
    });
```

### Conditional Handlers

```csharp
// Handler with predicate
resource.OnBadRequest<ApiError>(
    handler: error => Console.WriteLine($"Specific error: {error.Code}"),
    predicate: error => error.Code == "SPECIFIC_ERROR"
);

// Multiple handlers for same status
resource
    .OnServerError(response =>
    {
        // Log error
        return Task.CompletedTask;
    })
    .OnServerError(response =>
    {
        // Send alert
        return Task.CompletedTask;
    });
```

## Interceptors

### Built-in Interceptors

```csharp
// Bearer Token Interceptor
var authInterceptor = new BearerTokenInterceptor(async () =>
{
    // Get token from your auth service
    return await authService.GetAccessTokenAsync();
});

// Logging Interceptor
var loggingInterceptor = new LoggingInterceptor(message =>
{
    logger.LogInformation($"[HTTP] {message}");
});

// Retry Interceptor
var retryInterceptor = new RetryInterceptor(
    maxRetries: 3,
    delayMilliseconds: 1000,
    shouldRetry: response => 
        response.StatusCode == HttpStatusCode.ServiceUnavailable ||
        response.StatusCode == HttpStatusCode.TooManyRequests
);

// Configure resource with interceptors
var options = new HttpResourceOptions();
options.Interceptors.Add(authInterceptor);
options.Interceptors.Add(loggingInterceptor);
options.Interceptors.Add(retryInterceptor);
```

### Custom Interceptor

```csharp
public class CustomHeaderInterceptor : IHttpResourceInterceptor
{
    private readonly string _headerName;
    private readonly Func<Task<string>> _valueProvider;
    
    public CustomHeaderInterceptor(string headerName, Func<Task<string>> valueProvider)
    {
        _headerName = headerName;
        _valueProvider = valueProvider;
    }
    
    public async Task<HttpResponseMessage> InterceptAsync(
        HttpRequestMessage request,
        Func<HttpRequestMessage, Task<HttpResponseMessage>> next)
    {
        // Before request
        var headerValue = await _valueProvider();
        request.Headers.Add(_headerName, headerValue);
        
        // Execute request
        var response = await next(request);
        
        // After response
        if (response.Headers.TryGetValues("X-Custom-Response", out var values))
        {
            Console.WriteLine($"Custom response: {values.First()}");
        }
        
        return response;
    }
}
```

### TypedHttpResource with Interceptors

```csharp
public class SecureApiResource : TypedHttpResourceWithInterceptors
{
    public override void Initialize(HttpClient httpClient, string baseUrl, HttpResourceOptions options)
    {
        base.Initialize(httpClient, baseUrl, options);
        
        // Add interceptors specific to this resource
        AddInterceptor(new BearerTokenInterceptor(GetTokenAsync));
        AddInterceptor(new LoggingInterceptor(LogMessage));
    }
    
    private async Task<string> GetTokenAsync()
    {
        // Your token logic
        return "your-token";
    }
    
    private void LogMessage(string message)
    {
        Console.WriteLine($"[SecureAPI] {message}");
    }
}
```

## Caching

### Cache Providers

```csharp
// No caching (default)
var noCacheProvider = NoCacheProvider.Instance;

// Memory cache
services.AddMemoryCache();
var memoryCache = services.GetRequiredService<IMemoryCache>();
var memoryCacheProvider = new MemoryCacheProvider(memoryCache);

// Distributed cache (Redis, SQL Server, etc.)
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
var distributedCache = services.GetRequiredService<IDistributedCache>();
var distributedCacheProvider = new DistributedCacheProvider(distributedCache);

// Hybrid cache (preview)
services.AddHybridCache();
var hybridCache = services.GetRequiredService<HybridCache>();
var hybridCacheProvider = new HybridCacheProvider(hybridCache);
```

### Using Cached Requests

```csharp
public class CachedApiResource : TypedHttpResource
{
    private readonly ICacheProvider _cacheProvider;
    
    public CachedApiResource(ICacheProvider cacheProvider)
    {
        _cacheProvider = cacheProvider;
    }
    
    public async Task<Product?> GetProductAsync(int id)
    {
        var cacheKey = $"product:{id}";
        
        // Try cache first
        var cached = await _cacheProvider.GetAsync<Product>(cacheKey);
        if (cached != null)
            return cached;
        
        // Fetch from API
        var response = await Get<Product>($"/products/{id}").ExecuteAsync();
        
        if (response.IsSuccess && response.Data != null)
        {
            // Cache for 5 minutes
            await _cacheProvider.SetAsync(cacheKey, response.Data, TimeSpan.FromMinutes(5));
        }
        
        return response.Data;
    }
}
```

### CachedHttpResourceRequest

```csharp
public class CachedProductResource : TypedHttpResource
{
    public async Task<Product?> GetProductAsync(int id)
    {
        var request = Get<Product>($"/products/{id}");
        var cachedRequest = new CachedHttpResourceRequest<Product>(
            request,
            cacheProvider: new MemoryCacheProvider(memoryCache),
            cacheKey: $"product:{id}",
            expiration: TimeSpan.FromMinutes(10)
        );
        
        var response = await cachedRequest.ExecuteAsync();
        return response.Data;
    }
}
```

## Bulk Operations

### TypedHttpResourceWithBulk

```csharp
public class BulkApiResource : TypedHttpResourceWithBulk
{
    // Bulk create with automatic batching
    public async Task<BulkResult<User>> CreateUsersAsync(List<CreateUserDto> users)
    {
        return await BulkPost<CreateUserDto, User>("/users/bulk", users)
            .WithBatchSize(50)
            .WithParallelism(3)
            .OnBatchSuccess((batch, responses) =>
            {
                Console.WriteLine($"Batch of {batch.Count} succeeded");
            })
            .OnBatchError((batch, error) =>
            {
                Console.WriteLine($"Batch failed: {error.Message}");
            })
            .ExecuteAsync();
    }
    
    // Bulk update
    public async Task<BulkResult<User>> UpdateUsersAsync(List<User> users)
    {
        return await BulkPut<User, User>("/users/bulk", users)
            .WithBatchSize(25)
            .ExecuteAsync();
    }
    
    // Bulk delete
    public async Task<BulkResult<bool>> DeleteUsersAsync(List<int> userIds)
    {
        return await BulkRequest<int, bool>(
            "/users/bulk/delete",
            userIds,
            HttpMethod.Delete)
            .WithBatchSize(100)
            .ExecuteAsync();
    }
}
```

## Service Registration

### Basic Registration

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add HTTP client
builder.Services.AddHttpClient();

// Add FluentSignals.Http
builder.Services.AddFluentSignalsHttp(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Timeout = TimeSpan.FromSeconds(30);
});

// Register typed resources
builder.Services.AddTypedHttpResource<UserApiResource>();
builder.Services.AddTypedHttpResource<OrderApiResource>("https://orders.api.com");

// Register with specific lifetime
builder.Services.AddTypedHttpResource<ProductApiResource>(ServiceLifetime.Singleton);

// Register from assembly
builder.Services.AddTypedHttpResourcesFromAssemblyContaining<UserApiResource>();
```

### With HttpClient Configuration

```csharp
// Configure named HttpClient
builder.Services.AddHttpClient<UserApiResource>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.DefaultRequestHeaders.Add("X-API-Version", "2.0");
});

// Configure with Polly
builder.Services.AddHttpClient<OrderApiResource>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

## Testing

### Using MockHttpResourceRequest

```csharp
[Fact]
public async Task Should_Return_User()
{
    // Arrange
    var expectedUser = new User { Id = 1, Name = "Test User" };
    var mockRequest = new MockHttpResourceRequest<User>(
        expectedUser,
        HttpStatusCode.OK
    );
    
    // Act
    var result = await mockRequest.ExecuteAsync();
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(expectedUser.Id, result.Value.Id);
}

[Fact]
public async Task Should_Handle_Error()
{
    // Arrange
    var exception = new HttpRequestException("Network error");
    var mockRequest = new MockHttpResourceRequest<User>(exception);
    
    // Act & Assert
    await Assert.ThrowsAsync<HttpRequestException>(
        () => mockRequest.ExecuteAsync()
    );
}
```

### Testing TypedHttpResource

```csharp
public class UserApiResourceTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly UserApiResource _resource;
    
    public UserApiResourceTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.test.com");
        
        _resource = new UserApiResource();
        _resource.Initialize(httpClient, "https://api.test.com", new HttpResourceOptions());
    }
    
    [Fact]
    public async Task GetUserAsync_Should_Return_User()
    {
        // Arrange
        var expectedUser = new User { Id = 1, Name = "John" };
        _mockHttp.When("https://api.test.com/users/1")
            .Respond("application/json", JsonSerializer.Serialize(expectedUser));
        
        // Act
        var user = await _resource.GetUserAsync(1);
        
        // Assert
        Assert.NotNull(user);
        Assert.Equal(expectedUser.Id, user.Id);
        Assert.Equal(expectedUser.Name, user.Name);
    }
}
```

## Best Practices

### 1. Use TypedHttpResource for Type Safety

```csharp
// Bad - Manual HTTP calls
var response = await httpClient.GetAsync("/users/1");
var json = await response.Content.ReadAsStringAsync();
var user = JsonSerializer.Deserialize<User>(json);

// Good - Typed resource
public class UserApi : TypedHttpResource
{
    public async Task<User?> GetUserAsync(int id) =>
        (await Get<User>($"/users/{id}").ExecuteAsync()).Data;
}
```

### 2. Configure Once, Use Everywhere

```csharp
// Configure globally
builder.Services.AddFluentSignalsHttp(options =>
{
    options.DefaultHeaders["X-API-Key"] = Configuration["ApiKey"];
    options.Interceptors.Add(new BearerTokenInterceptor(tokenProvider));
});

// All resources inherit configuration
builder.Services.AddTypedHttpResource<UserApi>();
builder.Services.AddTypedHttpResource<OrderApi>();
```

### 3. Handle All Response Scenarios

```csharp
public async Task<User?> GetUserSafelyAsync(int id)
{
    var response = await Get<User>($"/users/{id}")
        .OnNotFound(_ => logger.LogWarning($"User {id} not found"))
        .OnServerError(_ => logger.LogError("Server error fetching user"))
        .ExecuteAsync();
        
    return response.IsSuccess ? response.Data : null;
}
```

### 4. Use Interceptors for Cross-Cutting Concerns

```csharp
// Don't repeat auth logic in every method
// Use interceptor instead
public class ApiResource : TypedHttpResourceWithInterceptors
{
    public override void Initialize(HttpClient httpClient, string baseUrl, HttpResourceOptions options)
    {
        base.Initialize(httpClient, baseUrl, options);
        AddInterceptor(new BearerTokenInterceptor(GetTokenAsync));
    }
}
```

## Key Takeaways for AI Assistants

1. **TypedHttpResource is the preferred pattern** - Type-safe API clients
2. **Use fluent API for request configuration** - Headers, query params, timeouts
3. **Status handlers provide elegant error handling** - No try-catch needed
4. **Interceptors handle cross-cutting concerns** - Auth, logging, retry
5. **Caching is pluggable** - Memory, distributed, or hybrid
6. **Bulk operations handle batching automatically** - Built-in parallelism
7. **Always check IsSuccess before accessing Data** - Null safety
8. **Register in DI for easy testing** - Mock HTTP handlers in tests