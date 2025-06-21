# TypedHttpResource Comprehensive Guide

TypedHttpResource is a powerful base class for creating strongly-typed, reactive HTTP API clients in FluentSignals. This guide covers everything from basic usage to advanced patterns.

## Table of Contents

1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Basic Implementation](#basic-implementation)
4. [Dependency Injection](#dependency-injection)
5. [Request Configuration](#request-configuration)
6. [Response Handling](#response-handling)
7. [Advanced Features](#advanced-features)
8. [Best Practices](#best-practices)
9. [Complete Examples](#complete-examples)

## Introduction

TypedHttpResource provides:
- Type-safe API client methods
- Reactive state management
- Built-in error handling
- Request/response interceptors
- Caching capabilities
- Testing support

## Getting Started

### Installation

```bash
dotnet add package FluentSignals.Http
```

### Basic Concepts

TypedHttpResource extends the reactive signal pattern to HTTP operations. Each request returns an `HttpResourceRequest<T>` that can be:
- Configured with additional options
- Executed to return an `HttpResource`
- Subscribed to for reactive updates

## Basic Implementation

### Simple API Client

```csharp
using FluentSignals.Http;

public class TodoApiClient : TypedHttpResource
{
    // Constructor for direct instantiation
    public TodoApiClient(HttpClient httpClient) 
        : base(httpClient, "/api/todos")
    {
    }
    
    // GET all todos
    public HttpResourceRequest<List<Todo>> GetAll() => 
        Get<List<Todo>>(BaseUrl);
    
    // GET single todo
    public HttpResourceRequest<Todo> GetById(int id) => 
        Get<Todo>($"{BaseUrl}/{id}");
    
    // POST new todo
    public HttpResourceRequest<Todo> Create(CreateTodoDto dto) => 
        Post<CreateTodoDto, Todo>(BaseUrl, dto);
    
    // PUT update todo
    public HttpResourceRequest<Todo> Update(int id, UpdateTodoDto dto) => 
        Put<UpdateTodoDto, Todo>($"{BaseUrl}/{id}", dto);
    
    // DELETE todo
    public HttpResourceRequest Delete(int id) => 
        Delete($"{BaseUrl}/{id}");
}
```

### Using the Client

```csharp
// Direct instantiation
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };
var todoApi = new TodoApiClient(httpClient);

// Execute a request
var resource = await todoApi.GetAll().ExecuteAsync();

// Subscribe to updates
resource.Subscribe(response => 
{
    if (response?.IsSuccess == true)
    {
        var todos = response.Data;
        Console.WriteLine($"Loaded {todos.Count} todos");
    }
});

// Access reactive properties
resource.IsLoading.Subscribe(loading => Console.WriteLine($"Loading: {loading}"));
resource.Error.Subscribe(error => Console.WriteLine($"Error: {error?.Message}"));
```

## Dependency Injection

### With HttpResourceAttribute

```csharp
[HttpResource("/api/todos")]
public class TodoApiClient : TypedHttpResource
{
    // Parameterless constructor for DI
    public TodoApiClient() { }
    
    // Your API methods...
}

// Registration
services.AddHttpClient();
services.AddTypedHttpResource<TodoApiClient>();

// Usage
public class TodoService
{
    private readonly TodoApiClient _api;
    
    public TodoService(TodoApiClient api) => _api = api;
    
    public async Task<List<Todo>> GetTodosAsync()
    {
        var resource = await _api.GetAll().ExecuteAsync();
        return resource.Value?.Data ?? new List<Todo>();
    }
}
```

### Advanced DI Configuration

```csharp
// Register with custom configuration
services.AddTypedHttpResource<TodoApiClient>(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.DefaultHeaders["X-Api-Version"] = "2.0";
    options.RetryOptions = new RetryOptions
    {
        MaxRetryAttempts = 3,
        InitialRetryDelay = 1000,
        UseExponentialBackoff = true
    };
});

// Or use factory pattern
services.AddSingleton<ITypedHttpResourceFactory<TodoApiClient>, TypedHttpResourceFactory<TodoApiClient>>();
```

## Request Configuration

### Query Parameters

```csharp
public class SearchableApiClient : TypedHttpResource
{
    // Using query object
    public HttpResourceRequest<PagedResult<User>> Search(SearchCriteria criteria)
    {
        return Get<PagedResult<User>>($"{BaseUrl}/search", criteria);
    }
    
    // Using fluent API
    public HttpResourceRequest<PagedResult<User>> SearchFluent(string query, int page, int pageSize)
    {
        return Get<PagedResult<User>>($"{BaseUrl}/search")
            .WithQueryParam("q", query)
            .WithQueryParam("page", page.ToString())
            .WithQueryParam("pageSize", pageSize.ToString());
    }
    
    // Multiple parameters at once
    public HttpResourceRequest<List<Product>> GetProducts(ProductFilter filter)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["category"] = filter.Category,
            ["minPrice"] = filter.MinPrice.ToString(),
            ["maxPrice"] = filter.MaxPrice.ToString(),
            ["inStock"] = filter.InStock.ToString()
        };
        
        return Get<List<Product>>($"{BaseUrl}/products")
            .WithQueryParams(queryParams);
    }
}
```

### Headers

```csharp
public class SecureApiClient : TypedHttpResource
{
    // Add headers to specific requests
    public HttpResourceRequest<SecureData> GetSecureData(string id)
    {
        return Get<SecureData>($"{BaseUrl}/secure/{id}")
            .WithHeader("X-Request-ID", Guid.NewGuid().ToString())
            .WithHeader("X-Client-Version", "1.0.0")
            .WithBearerToken(GetAccessToken());
    }
    
    // Multiple headers
    public HttpResourceRequest<Report> GenerateReport(ReportRequest request)
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Report-Type"] = request.Type,
            ["X-Include-Details"] = request.IncludeDetails.ToString(),
            ["Accept"] = "application/pdf"
        };
        
        return Post<ReportRequest, Report>($"{BaseUrl}/reports", request)
            .WithHeaders(headers);
    }
}
```

### Request Builder Pattern

```csharp
public class AdvancedApiClient : TypedHttpResource
{
    // Complex request using builder
    public HttpResourceRequest<ImportResult> ImportData(ImportRequest request)
    {
        return BuildRequest<ImportResult>($"{BaseUrl}/import")
            .WithMethod(HttpMethod.Post)
            .WithBody(request)
            .WithHeader("Content-Type", "application/json")
            .WithHeader("X-Import-Mode", request.Mode.ToString())
            .WithQueryParam("validate", request.ValidateFirst.ToString())
            .Build();
    }
    
    // Custom HTTP methods
    public HttpResourceRequest<MergeResult> MergeRecords(int sourceId, int targetId, MergeOptions options)
    {
        return Request<MergeOptions, MergeResult>(
                new HttpMethod("MERGE"), 
                $"{BaseUrl}/records/{sourceId}/merge/{targetId}", 
                options)
            .WithHeader("X-Merge-Strategy", options.Strategy.ToString());
    }
}
```

## Response Handling

### Status Code Handlers

```csharp
public class RobustApiClient : TypedHttpResource
{
    public async Task<User?> GetUserWithHandling(int id)
    {
        var resource = await GetUser(id)
            .ConfigureResource(r =>
            {
                r.OnSuccess(async response => 
                {
                    await LogActivity($"User {id} retrieved successfully");
                });
                
                r.OnNotFound(async response =>
                {
                    await LogActivity($"User {id} not found");
                    await CreateDefaultUser(id);
                });
                
                r.OnUnauthorized(async response =>
                {
                    await RefreshToken();
                    // Retry the request
                    await r.Refresh();
                });
                
                r.OnServerError(async response =>
                {
                    await NotifyAdministrator($"Server error: {response.StatusCode}");
                });
            })
            .ExecuteAsync();
            
        return resource.Value?.Data;
    }
    
    private HttpResourceRequest<User> GetUser(int id) => 
        Get<User>($"{BaseUrl}/users/{id}");
}
```

### Typed Error Handling

```csharp
public class ErrorAwareApiClient : TypedHttpResource
{
    public async Task<Result<User>> GetUserSafely(int id)
    {
        var resource = await Get<User>($"{BaseUrl}/users/{id}")
            .ConfigureResource(r =>
            {
                // Handle specific error types
                r.OnBadRequest<ValidationError>(async (error, retry) =>
                {
                    Console.WriteLine($"Validation failed: {error.Message}");
                    foreach (var field in error.Fields)
                    {
                        Console.WriteLine($"  - {field.Name}: {field.Error}");
                    }
                });
                
                r.OnConflict<ConflictError>(async (error, retry) =>
                {
                    if (error.CanRetry)
                    {
                        await Task.Delay(1000);
                        await retry();
                    }
                });
            })
            .ExecuteAsync();
            
        if (resource.Value?.IsSuccess == true)
            return Result<User>.Success(resource.Value.Data);
        else
            return Result<User>.Failure(resource.Error.Value?.Message ?? "Unknown error");
    }
}

public class ValidationError
{
    public string Message { get; set; }
    public List<FieldError> Fields { get; set; }
}

public class ConflictError
{
    public string ConflictingResource { get; set; }
    public bool CanRetry { get; set; }
}
```

## Advanced Features

### Interceptors

```csharp
public class InterceptedApiClient : TypedHttpResourceWithInterceptors
{
    public InterceptedApiClient(
        HttpClient httpClient,
        IAuthService authService,
        ILogger<InterceptedApiClient> logger,
        ITelemetryService telemetry)
        : base(httpClient, "/api/v2")
    {
        // Authentication
        AddInterceptor(new BearerTokenInterceptor(authService.GetTokenAsync));
        
        // Logging
        AddInterceptor(new LoggingInterceptor(logger, logBody: true));
        
        // Retry with custom logic
        AddInterceptor(new RetryInterceptor(
            maxRetries: 5,
            delay: TimeSpan.FromSeconds(1),
            shouldRetry: response => 
                response.StatusCode == HttpStatusCode.TooManyRequests ||
                response.StatusCode >= HttpStatusCode.InternalServerError));
        
        // Custom telemetry
        AddInterceptor(new TelemetryInterceptor(telemetry));
    }
}

// Custom interceptor
public class TelemetryInterceptor : IHttpResourceInterceptor
{
    private readonly ITelemetryService _telemetry;
    
    public TelemetryInterceptor(ITelemetryService telemetry) => _telemetry = telemetry;
    
    public async Task<HttpRequestMessage> OnRequestAsync(HttpRequestMessage request)
    {
        var correlationId = Guid.NewGuid().ToString();
        request.Headers.Add("X-Correlation-ID", correlationId);
        
        _telemetry.TrackRequest(new RequestTelemetry
        {
            CorrelationId = correlationId,
            Method = request.Method.Method,
            Uri = request.RequestUri.ToString(),
            Timestamp = DateTime.UtcNow
        });
        
        return request;
    }
    
    public async Task<HttpResponseMessage> OnResponseAsync(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-Correlation-ID", out var correlationIds))
        {
            _telemetry.TrackResponse(new ResponseTelemetry
            {
                CorrelationId = correlationIds.First(),
                StatusCode = (int)response.StatusCode,
                Duration = response.Headers.Age ?? TimeSpan.Zero
            });
        }
        
        return response;
    }
    
    public async Task OnExceptionAsync(HttpRequestMessage request, Exception exception)
    {
        _telemetry.TrackException(exception, new Dictionary<string, string>
        {
            ["RequestUri"] = request.RequestUri?.ToString() ?? "Unknown",
            ["Method"] = request.Method.Method
        });
    }
}
```

### Caching

```csharp
public class CachedApiClient : TypedHttpResource
{
    private readonly IResponseCache _cache;
    
    public CachedApiClient(HttpClient httpClient, IResponseCache cache)
        : base(httpClient, "/api")
    {
        _cache = cache;
    }
    
    // Cache with explicit key
    public HttpResourceRequest<UserProfile> GetUserProfile(int userId)
    {
        return Get<UserProfile>($"{BaseUrl}/users/{userId}/profile")
            .WithCache(_cache, $"user_profile_{userId}", TimeSpan.FromMinutes(15));
    }
    
    // Cache with auto-generated key
    public HttpResourceRequest<List<Category>> GetCategories()
    {
        return Get<List<Category>>($"{BaseUrl}/categories")
            .WithCache(_cache, TimeSpan.FromHours(1));
    }
    
    // Conditional caching
    public HttpResourceRequest<SearchResults> Search(string query, bool useCache = true)
    {
        var request = Get<SearchResults>($"{BaseUrl}/search")
            .WithQueryParam("q", query);
            
        if (useCache && !string.IsNullOrEmpty(query))
        {
            request = request.WithCache(_cache, $"search_{query}", TimeSpan.FromMinutes(5));
        }
        
        return request;
    }
    
    // Cache invalidation
    public async Task<User> UpdateUserAsync(int id, UpdateUserDto dto)
    {
        // Update the user
        var resource = await Put<UpdateUserDto, User>($"{BaseUrl}/users/{id}", dto)
            .ExecuteAsync();
            
        // Invalidate related caches
        await _cache.RemoveAsync($"user_profile_{id}");
        await _cache.RemoveAsync($"user_{id}");
        
        return resource.Value?.Data;
    }
}
```

### Pagination

```csharp
public class PaginatedApiClient : TypedHttpResource
{
    // Simple pagination
    public HttpResourceRequest<PagedResult<Product>> GetProducts(int page = 1, int pageSize = 20)
    {
        return Get<PagedResult<Product>>($"{BaseUrl}/products")
            .WithPaging(page, pageSize);
    }
    
    // Advanced pagination with sorting and filtering
    public HttpResourceRequest<PagedResult<Order>> GetOrders(OrderQuery query)
    {
        var pagedRequest = new PagedRequest<Order>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            SortBy = query.SortBy,
            SortDescending = query.SortDescending,
            Filters = new Dictionary<string, string>
            {
                ["status"] = query.Status?.ToString() ?? "",
                ["customerId"] = query.CustomerId?.ToString() ?? "",
                ["minAmount"] = query.MinAmount?.ToString() ?? "",
                ["maxAmount"] = query.MaxAmount?.ToString() ?? ""
            }
        };
        
        return Get<PagedResult<Order>>($"{BaseUrl}/orders")
            .WithPaging(pagedRequest);
    }
    
    // Cursor-based pagination
    public HttpResourceRequest<CursorResult<Event>> GetEvents(string? cursor = null, int limit = 50)
    {
        var request = Get<CursorResult<Event>>($"{BaseUrl}/events")
            .WithQueryParam("limit", limit.ToString());
            
        if (!string.IsNullOrEmpty(cursor))
        {
            request = request.WithQueryParam("cursor", cursor);
        }
        
        return request;
    }
}

public class CursorResult<T>
{
    public List<T> Items { get; set; }
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
}
```

### Bulk Operations

```csharp
public class BulkApiClient : TypedHttpResourceWithBulk
{
    // Simple bulk import
    public async Task<BulkResult<Contact>> ImportContacts(List<ContactDto> contacts)
    {
        return await ExecuteBulkAsync<ContactDto, Contact>(
            $"{BaseUrl}/contacts/import",
            contacts,
            batchSize: 100,
            onProgress: progress =>
            {
                Console.WriteLine($"Import progress: {progress.PercentComplete:F1}% " +
                                $"({progress.ProcessedItems}/{progress.TotalItems})");
            });
    }
    
    // Parallel bulk processing
    public async Task<BulkResult<ProcessedImage>> ProcessImages(List<ImageUpload> images)
    {
        return await ExecuteBulkParallelAsync<ImageUpload, ProcessedImage>(
            $"{BaseUrl}/images/process",
            images,
            batchSize: 10,
            maxParallelism: 4,
            onProgress: progress =>
            {
                if (progress.CurrentBatch % 10 == 0)
                {
                    Console.WriteLine($"Processed {progress.ProcessedItems} images...");
                }
            });
    }
    
    // Bulk with error handling
    public async Task<BulkImportReport> ImportWithReport(List<DataRecord> records)
    {
        var result = await ExecuteBulkAsync<DataRecord, ImportedRecord>(
            $"{BaseUrl}/data/import",
            records,
            batchSize: 500);
            
        var report = new BulkImportReport
        {
            TotalRecords = result.TotalProcessed,
            SuccessfulRecords = result.SuccessCount,
            FailedRecords = result.ErrorCount,
            SuccessRate = result.SuccessRate,
            Errors = result.Errors.Select(e => new ImportError
            {
                BatchIndex = e.BatchIndex,
                RecordCount = e.Items.Count,
                ErrorMessage = e.Error.Message
            }).ToList()
        };
        
        return report;
    }
}
```

### Testing

```csharp
public class TestableApiClient : TypedHttpResource
{
    public HttpResourceRequest<WeatherForecast> GetWeather(string city) =>
        Get<WeatherForecast>($"{BaseUrl}/weather/{city}");
}

// Unit tests
public class ApiClientTests
{
    [Fact]
    public async Task GetWeather_ReturnsExpectedData()
    {
        // Arrange
        var expectedWeather = new WeatherForecast 
        { 
            City = "Seattle", 
            Temperature = 65,
            Conditions = "Partly Cloudy"
        };
        
        var mockRequest = new MockHttpResourceRequest<WeatherForecast>(
            expectedWeather, 
            HttpStatusCode.OK);
        
        // Act
        var resource = await mockRequest.ExecuteAsync();
        
        // Assert
        Assert.NotNull(resource.Value);
        Assert.True(resource.Value.IsSuccess);
        Assert.Equal(expectedWeather.City, resource.Value.Data.City);
    }
    
    [Fact]
    public async Task GetWeather_HandlesNotFound()
    {
        // Arrange
        var mockRequest = new MockHttpResourceRequest<WeatherForecast>(
            new HttpRequestException("404 Not Found"));
        
        // Act
        var resource = await mockRequest.ExecuteAsync();
        
        // Assert
        Assert.NotNull(resource.Error.Value);
        Assert.Contains("404", resource.Error.Value.Message);
    }
    
    [Fact]
    public async Task GetWeather_HandlesTimeout()
    {
        // Arrange
        var mockRequest = new MockHttpResourceRequest<WeatherForecast>(
            new WeatherForecast(),
            HttpStatusCode.OK,
            delay: TimeSpan.FromSeconds(5));
        
        // Act with timeout
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await mockRequest.ExecuteAsync();
        });
    }
}
```

## Best Practices

### 1. Resource Lifecycle Management

```csharp
public class OrderService : IDisposable
{
    private readonly OrderApiClient _api;
    private readonly List<IDisposable> _subscriptions = new();
    
    public OrderService(OrderApiClient api) => _api = api;
    
    public async Task MonitorOrders()
    {
        var resource = await _api.GetActiveOrders().ExecuteAsync();
        
        // Store subscriptions for cleanup
        _subscriptions.Add(
            resource.Subscribe(response =>
            {
                if (response?.IsSuccess == true)
                {
                    ProcessOrders(response.Data);
                }
            }));
            
        _subscriptions.Add(
            resource.Error.Subscribe(error =>
            {
                if (error != null)
                {
                    LogError(error);
                }
            }));
    }
    
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
    }
}
```

### 2. Error Recovery

```csharp
public class ResilientApiClient : TypedHttpResource
{
    public async Task<T?> ExecuteWithFallback<T>(
        HttpResourceRequest<T> request,
        Func<Task<T>> fallback,
        int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var resource = await request.ExecuteAsync();
            
            if (resource.Value?.IsSuccess == true)
            {
                return resource.Value.Data;
            }
            
            if (i < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
        }
        
        // Use fallback
        return await fallback();
    }
}
```

### 3. Configuration Management

```csharp
public class ConfigurableApiClient : TypedHttpResource
{
    private readonly ApiConfiguration _config;
    
    public ConfigurableApiClient(HttpClient httpClient, IOptions<ApiConfiguration> config)
        : base(httpClient, config.Value.BaseUrl)
    {
        _config = config.Value;
    }
    
    protected override HttpResourceRequest<T> Get<T>(string url)
    {
        var request = base.Get<T>(url);
        
        // Apply default configuration
        if (_config.DefaultTimeout.HasValue)
        {
            request = request.WithTimeout(_config.DefaultTimeout.Value);
        }
        
        if (_config.DefaultRetryCount > 0)
        {
            request = request.WithRetry(_config.DefaultRetryCount);
        }
        
        return request;
    }
}

public class ApiConfiguration
{
    public string BaseUrl { get; set; }
    public TimeSpan? DefaultTimeout { get; set; }
    public int DefaultRetryCount { get; set; }
    public Dictionary<string, string> DefaultHeaders { get; set; }
}
```

## Complete Examples

### Full-Featured API Client

```csharp
[HttpResource("/api/v1")]
public class ComprehensiveApiClient : TypedHttpResourceWithInterceptors
{
    private readonly IResponseCache _cache;
    private readonly ILogger<ComprehensiveApiClient> _logger;
    
    public ComprehensiveApiClient(
        HttpClient httpClient,
        IResponseCache cache,
        IAuthService authService,
        ILogger<ComprehensiveApiClient> logger)
        : base(httpClient, "/api/v1")
    {
        _cache = cache;
        _logger = logger;
        
        // Configure interceptors
        AddInterceptor(new BearerTokenInterceptor(authService.GetTokenAsync));
        AddInterceptor(new LoggingInterceptor(logger));
        AddInterceptor(new RetryInterceptor(3, TimeSpan.FromSeconds(1)));
    }
    
    // Cached user retrieval with error handling
    public async Task<User?> GetUserAsync(int id)
    {
        try
        {
            var resource = await Get<User>($"{BaseUrl}/users/{id}")
                .WithCache(_cache, $"user_{id}", TimeSpan.FromMinutes(10))
                .ConfigureResource(r =>
                {
                    r.OnNotFound(async _ =>
                    {
                        _logger.LogWarning("User {UserId} not found", id);
                    });
                })
                .ExecuteAsync();
                
            return resource.Value?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return null;
        }
    }
    
    // Paginated search with cancellation
    public async Task<PagedResult<User>> SearchUsersAsync(
        string query,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var resource = await Get<PagedResult<User>>($"{BaseUrl}/users/search")
            .WithQueryParam("q", query)
            .WithPaging(page, pageSize)
            .WithCancellation(cancellationToken)
            .ExecuteAsync();
            
        return resource.Value?.Data ?? new PagedResult<User>();
    }
    
    // Bulk import with progress
    public async Task<BulkImportResult> ImportUsersAsync(
        List<UserImportDto> users,
        IProgress<double> progress)
    {
        var result = await ExecuteBulkAsync<UserImportDto, User>(
            $"{BaseUrl}/users/import",
            users,
            batchSize: 100,
            onProgress: p => progress.Report(p.PercentComplete / 100.0));
            
        return new BulkImportResult
        {
            TotalCount = result.TotalProcessed,
            SuccessCount = result.SuccessCount,
            FailureCount = result.ErrorCount,
            ImportedUsers = result.Results
        };
    }
    
    // Complex operation with transaction
    public async Task<TransferResult> TransferDataAsync(TransferRequest request)
    {
        return await BuildRequest<TransferResult>($"{BaseUrl}/transfer")
            .WithMethod(HttpMethod.Post)
            .WithBody(request)
            .WithHeader("X-Transaction-ID", Guid.NewGuid().ToString())
            .WithHeader("X-Idempotency-Key", request.IdempotencyKey)
            .WithTimeout(TimeSpan.FromMinutes(5))
            .Build()
            .ConfigureResource(r =>
            {
                r.OnAccepted(async response =>
                {
                    _logger.LogInformation("Transfer initiated: {Location}", 
                        response.Headers.Location);
                });
                
                r.OnConflict<TransferConflict>(async (conflict, retry) =>
                {
                    if (conflict.CanResolve)
                    {
                        request.ConflictResolution = conflict.SuggestedResolution;
                        await retry();
                    }
                });
            })
            .ExecuteAsync()
            .ContinueWith(t => t.Result.Value?.Data ?? new TransferResult());
    }
}
```

### Integration with Blazor Component

```razor
@page "/users"
@using FluentSignals.Http
@inject ComprehensiveApiClient Api

<h3>User Management</h3>

@if (_usersResource?.IsLoading.Value == true)
{
    <LoadingSpinner />
}
else if (_usersResource?.Error.Value != null)
{
    <ErrorAlert Message="@_usersResource.Error.Value.Message" OnRetry="LoadUsers" />
}
else if (_users?.Items.Any() == true)
{
    <UserTable Users="_users.Items" OnEdit="EditUser" OnDelete="DeleteUser" />
    <Pagination Current="_currentPage" Total="_users.TotalPages" OnPageChange="ChangePage" />
}

@code {
    private HttpResource? _usersResource;
    private PagedResult<User>? _users;
    private int _currentPage = 1;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadUsers();
    }
    
    private async Task LoadUsers()
    {
        _usersResource = await Api.GetUsers(_currentPage, 20).ExecuteAsync();
        _users = _usersResource.Value?.Data;
        
        // Subscribe to updates
        _usersResource.Subscribe(_ => InvokeAsync(StateHasChanged));
    }
    
    private async Task ChangePage(int page)
    {
        _currentPage = page;
        await LoadUsers();
    }
    
    private async Task EditUser(User user)
    {
        // Edit logic
    }
    
    private async Task DeleteUser(int userId)
    {
        await Api.DeleteUser(userId).ExecuteAsync();
        await LoadUsers(); // Refresh
    }
}
```

## Summary

TypedHttpResource provides a powerful, type-safe way to build HTTP API clients with:
- Full type safety for requests and responses
- Reactive state management
- Comprehensive error handling
- Advanced features like caching, pagination, and bulk operations
- Testability through mock implementations
- Integration with dependency injection

By following the patterns and practices in this guide, you can build robust, maintainable API clients that handle complex scenarios while remaining easy to use and test.