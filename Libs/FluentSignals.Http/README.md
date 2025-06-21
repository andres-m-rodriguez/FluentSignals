# FluentSignals.Http

HTTP client extensions for FluentSignals providing reactive HTTP resources, typed API clients, interceptors, caching, and advanced features for building robust HTTP integrations.

## Installation

```bash
dotnet add package FluentSignals.Http
```

## Project Structure

```
FluentSignals.Http/
├── Contracts/          # Interfaces and contracts
│   ├── ITypedHttpResource.cs
│   └── ITypedHttpResourceFactory.cs
├── Resources/          # Core HTTP resources
│   ├── HttpResource.cs
│   ├── TypedHttpResource.cs
│   ├── TypedHttpResourceFactory.cs
│   └── HttpResourceAttribute.cs
├── Types/              # Data types and models
│   ├── HttpResponse.cs
│   └── HttpError.cs
├── Options/            # Configuration options
│   ├── HttpResourceOptions.cs
│   └── RetryOptions.cs
├── Interceptors/       # Request/response interceptors
│   ├── BearerTokenInterceptor.cs
│   ├── LoggingInterceptor.cs
│   └── RetryInterceptor.cs
├── Caching/            # Optional caching support
│   ├── ICacheProvider.cs
│   ├── CachingExtensions.cs
│   └── Providers/
│       ├── NoCacheProvider.cs
│       ├── InMemoryCacheProvider.cs
│       └── HybridCacheProvider.cs
└── Extensions/         # Extension methods
    └── ServiceCollectionExtensions.cs
```

## Features

- 🔄 **Reactive HTTP Resources** - HTTP clients with reactive state management
- 📝 **Typed API Clients** - Strongly-typed HTTP resources with TypedHttpResource
- 🔌 **Interceptors** - Extensible request/response pipeline
- 💾 **Response Caching** - Built-in caching with configurable providers
- 📄 **Pagination Support** - First-class pagination helpers
- ⚡ **Bulk Operations** - Efficient batch processing
- 🧪 **Testing Utilities** - Mock HTTP resources for unit testing
- 🔁 **Retry Policies** - Configurable retry logic with exponential backoff
- 🎯 **Status Code Handlers** - Type-safe response handling

## Quick Start

### Basic HTTP Resource

```csharp
using FluentSignals.Http;

// Create an HTTP resource
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
var resource = new HttpResource(httpClient);

// Subscribe to reactive updates
resource.Subscribe(response =>
{
    if (response?.IsSuccess == true)
    {
        Console.WriteLine($"Success! Data: {response.Content}");
    }
});

// Make requests
await resource.GetAsync<List<User>>("users");
await resource.PostAsync<CreateUserDto, User>("users", new CreateUserDto { Name = "John" });
```

### Typed HTTP Resources

```csharp
// Define a typed API client
[HttpResource("/api/users")]
public class UserApiClient : TypedHttpResource
{
    public HttpResourceRequest<User> GetById(int id) => 
        Get<User>($"{BaseUrl}/{id}");
    
    public HttpResourceRequest<PagedResult<User>> GetAll(int page = 1, int pageSize = 20) =>
        Get<PagedResult<User>>(BaseUrl)
            .WithQueryParam("page", page.ToString())
            .WithQueryParam("pageSize", pageSize.ToString());
    
    public HttpResourceRequest<User> Create(CreateUserDto dto) =>
        Post<CreateUserDto, User>(BaseUrl, dto);
    
    public HttpResourceRequest<User> Update(int id, UpdateUserDto dto) =>
        Put<UpdateUserDto, User>($"{BaseUrl}/{id}", dto);
    
    public HttpResourceRequest Delete(int id) =>
        Delete($"{BaseUrl}/{id}");
}

// Register with DI
services.AddHttpClient();
services.AddTypedHttpResource<UserApiClient>();

// Use via dependency injection
public class UserService
{
    private readonly UserApiClient _api;
    
    public UserService(UserApiClient api) => _api = api;
    
    public async Task<User?> GetUserAsync(int id)
    {
        var resource = await _api.GetById(id).ExecuteAsync();
        return resource.Value?.Data;
    }
}
```

## Advanced Features

### Interceptors

```csharp
public class MyApiClient : TypedHttpResourceWithInterceptors
{
    public MyApiClient(HttpClient httpClient, ITokenProvider tokenProvider, ILogger<MyApiClient> logger)
        : base(httpClient, "/api")
    {
        // Add authentication
        AddInterceptor(new BearerTokenInterceptor(tokenProvider.GetTokenAsync));
        
        // Add logging
        AddInterceptor(new LoggingInterceptor(logger));
        
        // Add retry logic
        AddInterceptor(new RetryInterceptor(maxRetries: 3));
    }
}
```

### Response Caching (Optional)

FluentSignals.Http provides flexible caching options:

```csharp
// Option 1: No caching (default)
var noCacheProvider = new NoCacheProvider();
var user = await apiClient.GetUser(123)
    .WithCache(noCacheProvider, "user_123", TimeSpan.FromMinutes(5)) // No-op
    .ExecuteAsync();

// Option 2: In-memory caching
var memoryCache = new InMemoryCacheProvider();
var user = await apiClient.GetUser(123)
    .WithCache(memoryCache, "user_123", TimeSpan.FromMinutes(5))
    .ExecuteAsync();

// Option 3: Hybrid caching (requires Microsoft.Extensions.Caching.Hybrid)
services.AddHybridCache();
var hybridCache = new HybridCacheProvider(serviceProvider.GetRequiredService<HybridCache>());
var user = await apiClient.GetUser(123)
    .WithCache(hybridCache, TimeSpan.FromMinutes(5)) // Auto-generated key
    .ExecuteAsync();

// Configure caching per resource
public class CachedApiClient : TypedHttpResource
{
    private readonly ICacheProvider _cacheProvider;
    
    public CachedApiClient(HttpClient httpClient, ICacheProvider cacheProvider)
        : base(httpClient, "/api")
    {
        _cacheProvider = cacheProvider;
    }
    
    public HttpResourceRequest<User> GetUser(int id) =>
        Get<User>($"{BaseUrl}/users/{id}")
            .WithCache(_cacheProvider, $"user_{id}", TimeSpan.FromMinutes(10));
}
```

### Pagination

```csharp
var pagedUsers = await apiClient.GetUsers()
    .WithPaging(page: 2, pageSize: 50, sortBy: "name")
    .ExecuteAsync();
```

### Bulk Operations

```csharp
public class BulkApiClient : TypedHttpResourceWithBulk
{
    public async Task<BulkResult<User>> ImportUsers(List<CreateUserDto> users)
    {
        return await ExecuteBulkParallelAsync<CreateUserDto, User>(
            "/api/users/import",
            users,
            batchSize: 100,
            maxParallelism: 4,
            onProgress: p => Console.WriteLine($"Progress: {p.PercentComplete}%"));
    }
}
```

### Fluent Configuration

```csharp
var result = await apiClient
    .SearchUsers("john")
    .WithPaging(1, 20)
    .WithCache(cache, TimeSpan.FromMinutes(5))
    .WithBearerToken(token)
    .WithTimeout(TimeSpan.FromSeconds(30))
    .WithRetry(3)
    .WithCancellation(cancellationToken)
    .ExecuteAsync();
```

## TypedHttpResource Guide

See [TypedHttpResource.md](docs/TypedHttpResource.md) for comprehensive documentation on creating typed API clients.

## Testing

```csharp
// Mock successful response
var mockRequest = new MockHttpResourceRequest<User>(
    new User { Id = 1, Name = "Test" }, 
    HttpStatusCode.OK);

// Mock error
var errorRequest = new MockHttpResourceRequest<User>(
    new HttpRequestException("Network error"));

// Mock with delay
var slowRequest = new MockHttpResourceRequest<User>(
    userData, HttpStatusCode.OK, delay: TimeSpan.FromSeconds(2));
```

## License

Part of the FluentSignals project. See LICENSE for details.