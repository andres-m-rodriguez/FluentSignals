# FluentSignals

A powerful reactive state management library for .NET applications inspired by SolidJS signals. FluentSignals provides fine-grained reactivity with automatic dependency tracking, making it perfect for building responsive applications with minimal boilerplate.

## 📦 Latest Version: 1.1.2

### What's New in 1.1.2
- **Custom JSON Serialization**: Configure JsonSerializerOptions for HttpResource
- **Bug Fix**: Resolved duplicate handler calls for typed HTTP status handlers
- **Enhanced Documentation**: Added comprehensive guides for JSON deserialization

See the [full changelog](https://github.com/andres-m-rodriguez/FluentSignals/blob/main/CHANGELOG.md) for version history.

## Features

- 🚀 **Fine-grained reactivity** - Only update what needs to be updated
- 🔄 **Automatic dependency tracking** - No need to manually manage subscriptions
- 📦 **Typed and untyped signals** - Use `Signal<T>` for type safety or `Signal` for flexibility
- ⚡ **Async signals** - Built-in support for asynchronous operations
- 🌊 **Computed signals** - Automatically derive values from other signals
- 🎯 **Resource management** - Generic resource pattern with loading/error states
- 🌐 **HTTP resources** - Built-in HTTP client with caching and retry policies
- 🔌 **Extensible** - Easy to extend with custom signal types

## Installation

```bash
dotnet add package FluentSignals
```

## Quick Start

### Basic Signal Usage

```csharp
using FluentSignals;

// Create a signal
var count = new Signal<int>(0);

// Subscribe to changes
count.Subscribe(value => Console.WriteLine($"Count is now: {value}"));

// Update the signal
count.Value = 1; // Output: Count is now: 1
count.Value = 2; // Output: Count is now: 2
```

### Computed Signals

```csharp
var firstName = new Signal<string>("John");
var lastName = new Signal<string>("Doe");

// Create a computed signal
var fullName = new ComputedSignal<string>(() => $"{firstName.Value} {lastName.Value}");

fullName.Subscribe(name => Console.WriteLine($"Full name: {name}"));

firstName.Value = "Jane"; // Output: Full name: Jane Doe
```

### Async Signals

```csharp
var asyncSignal = new AsyncSignal<string>(async () => 
{
    await Task.Delay(1000);
    return "Data loaded!";
});

// Access the value
await asyncSignal.GetValueAsync(); // Returns "Data loaded!" after 1 second
```

### Resource Signals

```csharp
// Create a resource with a fetcher function
var userResource = new ResourceSignal<User>(
    async (ct) => await LoadUserFromDatabase(userId, ct)
);

// Subscribe to state changes
userResource.Subscribe(state =>
{
    if (state.IsLoading) Console.WriteLine("Loading...");
    if (state.HasData) Console.WriteLine($"User: {state.Data.Name}");
    if (state.HasError) Console.WriteLine($"Error: {state.Error.Message}");
});

// Load the resource
await userResource.LoadAsync();
```

### HTTP Resources

#### Direct Usage
```csharp
// Setup
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
var resource = new HttpResource(httpClient);

// Subscribe to reactive updates
resource.Subscribe(response =>
{
    if (response?.IsSuccess == true)
    {
        // Handle successful response
    }
});

// GET - Fetch data
var todos = await resource.GetAsync<List<TodoItem>>("todos");

// POST - Create new resource
var newTodo = new TodoItem { Title = "New Task" };
var created = await resource.PostAsync<TodoItem, TodoItem>("todos", newTodo);

// PUT - Full update
var updated = new TodoItem { Id = 1, Title = "Updated Task", Completed = true };
var result = await resource.PutAsync<TodoItem, TodoItem>("todos/1", updated);

// PATCH - Partial update
var patch = new { completed = true };
var patched = await resource.PatchAsync<object, TodoItem>("todos/1", patch);

// DELETE - Remove resource
var deleted = await resource.DeleteAsync("todos/1");

// Access reactive signals
resource.IsLoading.Subscribe(loading => Console.WriteLine($"Loading: {loading}"));
resource.Error.Subscribe(error => Console.WriteLine($"Error: {error?.Message}"));
resource.LastStatusCode.Subscribe(status => Console.WriteLine($"Status: {status}"));
```

#### Using HttpResourceFactory (Recommended for Blazor)
```csharp
// Program.cs - Setup
builder.Services.AddHttpClient();
builder.Services.AddFluentSignalsBlazor(options =>
{
    options.BaseUrl = "https://api.example.com/";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.RetryOptions = new RetryOptions
    {
        MaxRetryAttempts = 3,
        InitialRetryDelay = 100,
        UseExponentialBackoff = true
    };
});

// In your Blazor component or service
@inject IHttpResourceFactory ResourceFactory

@code {
    private HttpResource _todosResource;
    
    protected override void OnInitialized()
    {
        // Create with default options from DI
        _todosResource = ResourceFactory.Create();
        
        // Or create with custom base URL
        _todosResource = ResourceFactory.CreateWithBaseUrl("https://api.other.com/");
        
        // Or create with custom options
        _todosResource = ResourceFactory.CreateWithOptions(options =>
        {
            options.Timeout = TimeSpan.FromSeconds(60);
            options.DefaultHeaders["Authorization"] = "Bearer token";
        });
        
        // Subscribe to changes
        _todosResource.Subscribe(response =>
        {
            if (response?.IsSuccess == true)
            {
                StateHasChanged();
            }
        });
    }
    
    // All REST operations work the same way
    async Task LoadTodos() => await _todosResource.GetAsync<List<TodoItem>>("todos");
    
    async Task CreateTodo(TodoItem todo) => 
        await _todosResource.PostAsync<TodoItem, TodoItem>("todos", todo);
    
    async Task UpdateTodo(TodoItem todo) => 
        await _todosResource.PutAsync<TodoItem, TodoItem>($"todos/{todo.Id}", todo);
    
    async Task PatchTodo(int id, object patch) => 
        await _todosResource.PatchAsync<object, TodoItem>($"todos/{id}", patch);
    
    async Task DeleteTodo(int id) => 
        await _todosResource.DeleteAsync($"todos/{id}");
}
```

### Typed HTTP Resources

Create strongly-typed HTTP resource classes for better organization and reusability:

```csharp
using FluentSignals.Options.HttpResource;

// Option 1: Direct instantiation with HttpClient
public class UserResource : TypedHttpResource
{
    public UserResource(HttpClient httpClient) 
        : base(httpClient, "/api/users") { }
    
    public HttpResourceRequest<User> GetById(int id) => 
        Get<User>($"{BaseUrl}/{id}");
    
    public HttpResourceRequest<IEnumerable<User>> GetAll() => 
        Get<IEnumerable<User>>(BaseUrl);
    
    public HttpResourceRequest<User> Create(User user) => 
        Post<User>(BaseUrl, user);
    
    public HttpResourceRequest<User> Update(int id, User user) => 
        Put<User>($"{BaseUrl}/{id}", user);
    
    public HttpResourceRequest Delete(int id) => 
        Delete($"{BaseUrl}/{id}");
}

// Direct usage
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
var users = new UserResource(httpClient);

// Execute requests
var userResource = await users.GetById(123).ExecuteAsync();
userResource.Subscribe(response => 
{
    if (response?.IsSuccess == true)
    {
        Console.WriteLine($"User loaded: {response.Data.Name}");
    }
});

// Option 2: Using with dependency injection (recommended)
// Define with attribute for factory-based creation
[HttpResource("/api/users")]
public class UserResource : TypedHttpResource
{
    public UserResource() { } // Parameterless constructor for factory
    
    // Same methods as above...
}

// Register in DI
services.AddTypedHttpResource<UserResource>();

// Inject and use
public class MyService
{
    private readonly UserResource _users;
    
    public MyService(UserResource users)
    {
        _users = users;
    }
    
    public async Task<User> GetUserAsync(int id)
    {
        var resource = await _users.GetById(id).ExecuteAsync();
        return resource.Value?.Data;
    }
}

// Advanced: Custom typed methods with full control
public class AdvancedUserResource : TypedHttpResource
{
    public AdvancedUserResource(HttpClient httpClient) 
        : base(httpClient, "/api/v2") { }
    
    // Fully typed search method with custom query parameters
    public HttpResourceRequest<PagedResult<User>> Search(UserSearchCriteria criteria)
    {
        return Get<PagedResult<User>>($"{BaseUrl}/users/search")
            .WithQueryParam("name", criteria.Name)
            .WithQueryParam("email", criteria.Email)
            .WithQueryParam("page", criteria.Page.ToString())
            .WithQueryParam("pageSize", criteria.PageSize.ToString());
    }
    
    // Bulk operations with typed request/response
    public HttpResourceRequest<BulkUpdateResult> BulkUpdate(BulkUpdateRequest<User> request)
    {
        return Post<BulkUpdateRequest<User>, BulkUpdateResult>($"{BaseUrl}/users/bulk", request)
            .WithHeader("X-Bulk-Operation", "true")
            .ConfigureResource(r => r.OnSuccess(() => Console.WriteLine("Bulk update completed")));
    }
    
    // Custom HTTP method with full control
    public HttpResourceRequest<MergeResult> MergeUsers(int sourceId, int targetId, MergeOptions options)
    {
        return Request<MergeOptions, MergeResult>(
                new HttpMethod("MERGE"), 
                $"{BaseUrl}/users/{sourceId}/merge/{targetId}", 
                options)
            .WithHeader("X-Merge-Strategy", options.Strategy.ToString());
    }
    
    // Complex request using the builder pattern
    public HttpResourceRequest<ImportResult> Import(Stream csvData, ImportOptions options)
    {
        return BuildRequest<ImportResult>($"{BaseUrl}/users/import")
            .WithMethod(HttpMethod.Post)
            .WithBody(new { data = csvData, options })
            .WithHeader("Content-Type", "multipart/form-data")
            .WithHeader("X-Import-Mode", options.Mode.ToString())
            .WithQueryParam("validate", options.ValidateBeforeImport.ToString())
            .Build();
    }
}
```

### Advanced TypedHttpResource Features

#### Interceptors for Cross-Cutting Concerns

```csharp
// Create a resource with interceptors
public class SecureUserResource : TypedHttpResourceWithInterceptors
{
    public SecureUserResource(HttpClient httpClient, ITokenProvider tokenProvider, ILogger<SecureUserResource> logger)
        : base(httpClient, "/api/users")
    {
        // Add authentication
        AddInterceptor(new BearerTokenInterceptor(tokenProvider.GetTokenAsync));
        
        // Add logging
        AddInterceptor(new LoggingInterceptor(logger));
        
        // Add retry logic
        AddInterceptor(new RetryInterceptor(maxRetries: 3, delay: TimeSpan.FromSeconds(1)));
    }
}

// Custom interceptor
public class ApiKeyInterceptor : IHttpResourceInterceptor
{
    private readonly string _apiKey;
    
    public ApiKeyInterceptor(string apiKey) => _apiKey = apiKey;
    
    public Task<HttpRequestMessage> OnRequestAsync(HttpRequestMessage request)
    {
        request.Headers.Add("X-Api-Key", _apiKey);
        return Task.FromResult(request);
    }
    
    public Task<HttpResponseMessage> OnResponseAsync(HttpResponseMessage response) => 
        Task.FromResult(response);
    
    public Task OnExceptionAsync(HttpRequestMessage request, Exception exception) => 
        Task.CompletedTask;
}
```

#### Response Caching

```csharp
// Use built-in memory cache
var cache = new MemoryResponseCache();

// Cache responses
var user = await userResource.GetById(123)
    .WithCache(cache, "user_123", TimeSpan.FromMinutes(5))
    .ExecuteAsync();

// Or with automatic cache key generation
var users = await userResource.GetAll()
    .WithCache(cache, TimeSpan.FromMinutes(10))
    .ExecuteAsync();
```

#### Pagination Support

```csharp
// Simple pagination
var pagedUsers = await userResource.GetUsers()
    .WithPaging(page: 2, pageSize: 50, sortBy: "name", sortDescending: true)
    .ExecuteAsync();

// Advanced pagination with filters
var request = new PagedRequest<User>
{
    Page = 1,
    PageSize = 20,
    SortBy = "createdAt",
    SortDescending = true,
    Filters = new Dictionary<string, string>
    {
        ["department"] = "Engineering",
        ["active"] = "true"
    }
};

var result = await userResource.SearchUsers()
    .WithPaging(request)
    .ExecuteAsync();
```

#### Bulk Operations

```csharp
public class BulkUserResource : TypedHttpResourceWithBulk
{
    // Import users with progress tracking
    public async Task<BulkResult<User>> ImportUsers(List<CreateUserDto> users)
    {
        return await ExecuteBulkAsync<CreateUserDto, User>(
            "/api/users/import",
            users,
            batchSize: 100,
            onProgress: progress =>
            {
                Console.WriteLine($"Progress: {progress.PercentComplete}% ({progress.ProcessedItems}/{progress.TotalItems})");
            });
    }
    
    // Parallel bulk operations for better performance
    public async Task<BulkResult<User>> ImportUsersParallel(List<CreateUserDto> users)
    {
        return await ExecuteBulkParallelAsync<CreateUserDto, User>(
            "/api/users/import",
            users,
            batchSize: 100,
            maxParallelism: 4);
    }
}
```

#### Fluent Extensions

```csharp
// Combine multiple features
var result = await userResource
    .SearchUsers("john")
    .WithPaging(1, 20)                              // Add pagination
    .WithCache(cache, TimeSpan.FromMinutes(5))      // Cache results
    .WithBearerToken(token)                         // Add auth token
    .WithTimeout(TimeSpan.FromSeconds(30))          // Set timeout
    .WithRetry(3, TimeSpan.FromSeconds(1))          // Configure retry
    .WithCancellation(cancellationToken)            // Support cancellation
    .ExecuteAsync();
```

#### Testing with Mocks

```csharp
// Create mock responses for testing
var mockUser = new User { Id = 1, Name = "Test User" };
var mockRequest = new MockHttpResourceRequest<User>(mockUser, HttpStatusCode.OK);

// Test error scenarios
var errorRequest = new MockHttpResourceRequest<User>(
    new HttpRequestException("Network error"));

// Add delay to simulate network latency
var slowRequest = new MockHttpResourceRequest<User>(
    mockUser, HttpStatusCode.OK, delay: TimeSpan.FromSeconds(2));
```

## Advanced Features

### Signal Bus (Pub/Sub)

```csharp
// Publisher
services.AddScoped<ISignalPublisher>();

await signalPublisher.PublishAsync(new UserCreatedEvent { UserId = 123 });

// Consumer
services.AddScoped<ISignalConsumer<UserCreatedEvent>>();

signalConsumer.Subscribe(message => 
{
    Console.WriteLine($"User created: {message.UserId}");
});
```

### Queue-based Subscriptions

```csharp
// Subscribe with message queue - receives all messages, even those published before subscription
subscription = signalConsumer.SubscribeByQueue(message =>
{
    ProcessMessage(message);
}, processExistingMessages: true);
```

## Integration with Blazor

FluentSignals integrates seamlessly with Blazor applications. See the `FluentSignals.Blazor` package for Blazor-specific features and components.

## Documentation

For more detailed documentation, examples, and API reference, visit our [GitHub repository](https://github.com/andres-m-rodriguez/FluentSignals).

## License

This project is licensed under the MIT License - see the LICENSE file for details.