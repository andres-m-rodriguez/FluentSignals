# FluentSignals AI Guide

This guide is specifically designed for AI assistants to understand and effectively use the FluentSignals library. FluentSignals is a reactive state management library for .NET applications with specialized support for Blazor, HTTP resources, SignalR, and event bus patterns.

## Library Structure

FluentSignals is organized into multiple packages:

1. **FluentSignals** - Core reactive signals library
2. **FluentSignals.Http** - HTTP client integration with reactive patterns
3. **FluentSignals.SignalR** - SignalR real-time communication integration
4. **FluentSignals.SignalBus** - Event bus/messaging system
5. **FluentSignals.Blazor** - Blazor components and integration

## Core Concepts

### 1. Signals
A Signal is a reactive container for a value that notifies subscribers when it changes.

```csharp
// Create a signal
var counter = new Signal<int>(0);

// Subscribe to changes
counter.Subscribe(value => Console.WriteLine($"Counter: {value}"));

// Update the value
counter.Value = 5; // Triggers subscription
```

### 2. Computed Signals
Computed signals derive their value from other signals and update automatically.

```csharp
var firstName = new Signal<string>("John");
var lastName = new Signal<string>("Doe");

var fullName = new ComputedSignal<string>(
    () => $"{firstName.Value} {lastName.Value}",
    [firstName, lastName]
);
```

### 3. Async Signals
Async signals handle asynchronous operations with loading and error states.

```csharp
var dataSignal = new AsyncSignal<WeatherData>(async () => 
{
    var response = await httpClient.GetFromJsonAsync<WeatherData>("/api/weather");
    return response;
});

// Check states
if (dataSignal.IsLoading.Value) { /* Show loading */ }
if (dataSignal.Error.Value != null) { /* Show error */ }
if (dataSignal.Value != null) { /* Show data */ }
```

## HTTP Resources (FluentSignals.Http)

### Basic HTTP Resource Usage

```csharp
// In Program.cs
builder.Services.AddFluentSignalsHttp();
builder.Services.AddHttpClient();

// Create and use HTTP resource
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };
var resource = new HttpResource(httpClient);

// Make requests
var response = await resource.GetAsync<User>("/users/1");
if (response.IsSuccess)
{
    var user = response.Data;
}
```

### Typed HTTP Resources

Create strongly-typed API clients:

```csharp
// Define a typed resource
public class UserApiResource : TypedHttpResource
{
    public async Task<User?> GetUserAsync(int id)
    {
        var response = await Get<User>($"/users/{id}").ExecuteAsync();
        return response.Data;
    }
    
    public async Task<User?> CreateUserAsync(CreateUserDto dto)
    {
        var response = await Post<CreateUserDto, User>("/users", dto).ExecuteAsync();
        return response.Data;
    }
    
    public async Task<bool> DeleteUserAsync(int id)
    {
        var response = await Delete($"/users/{id}").ExecuteAsync();
        return response.IsSuccess;
    }
}

// Register in DI
builder.Services.AddTypedHttpResource<UserApiResource>("https://api.example.com");

// Use in a component/service
public class UserService
{
    private readonly UserApiResource _userApi;
    
    public UserService(UserApiResource userApi)
    {
        _userApi = userApi;
    }
    
    public async Task<User?> GetUserAsync(int id) => await _userApi.GetUserAsync(id);
}
```

### HTTP Resource with Error Handling

```csharp
var resource = new HttpResource(httpClient);

// Add status handlers
resource
    .OnSuccess<User>(user => Console.WriteLine($"User loaded: {user.Name}"))
    .OnNotFound(response => Console.WriteLine("User not found"))
    .OnServerError(response => Console.WriteLine("Server error occurred"));

// Make request - handlers will be called automatically
await resource.GetAsync<User>("/users/1");
```

### HTTP Resource with Interceptors

```csharp
// Configure with interceptors
var options = new HttpResourceOptions
{
    Interceptors = 
    {
        new BearerTokenInterceptor(() => Task.FromResult("your-token")),
        new LoggingInterceptor(message => logger.LogInformation(message)),
        new RetryInterceptor(maxRetries: 3)
    }
};

var resource = new HttpResource(httpClient, options);
```

## Blazor Integration (FluentSignals.Blazor)

### Using Signals in Blazor Components

```razor
@implements IDisposable
@inject IServiceProvider ServiceProvider

<div>
    <h3>Counter: @counter.Value</h3>
    <button @onclick="Increment">Increment</button>
</div>

@code {
    private Signal<int> counter = new(0);
    private IDisposable? subscription;
    
    protected override void OnInitialized()
    {
        subscription = counter.Subscribe(_ => InvokeAsync(StateHasChanged));
    }
    
    private void Increment() => counter.Value++;
    
    public void Dispose() => subscription?.Dispose();
}
```

### HTTP Resource View Component

```razor
<HttpResourceView TData="WeatherData" 
                  Url="/api/weather"
                  OnResourceCreated="OnResourceCreated">
    <Loading>
        <div class="spinner">Loading weather data...</div>
    </Loading>
    <Success Context="weather">
        <h3>Temperature: @weather.Temperature°C</h3>
        <p>Conditions: @weather.Conditions</p>
    </Success>
    <Error Context="error">
        <div class="alert alert-danger">
            Error loading weather: @error.Message
        </div>
    </Error>
</HttpResourceView>

@code {
    private HttpResource? weatherResource;
    
    private void OnResourceCreated(HttpResource resource)
    {
        weatherResource = resource;
        // Can add custom handlers or access the resource
        resource.OnSuccess<WeatherData>(data => 
        {
            Console.WriteLine($"Weather loaded: {data.Temperature}°C");
        });
    }
    
    private async Task RefreshWeather()
    {
        if (weatherResource != null)
        {
            await weatherResource.RefreshAsync();
        }
    }
}
```

## SignalR Integration (FluentSignals.SignalR)

### Using ResourceSignalR

```csharp
// Create a SignalR resource
var stockPriceResource = new ResourceSignalR<StockPrice>(
    hubUrl: "https://api.example.com/hubs/stock",
    methodName: "ReceiveStockPrice",
    fetcher: async (ct) => await GetInitialStockPrice(),
    configureConnection: builder => 
    {
        builder.WithAutomaticReconnect();
    }
);

// Start the connection
await stockPriceResource.StartAsync();

// Subscribe to updates
stockPriceResource.Subscribe(state =>
{
    if (state.HasData)
    {
        Console.WriteLine($"Stock price: ${state.Data.Price}");
    }
});
```

### SignalR in Blazor

```razor
<ResourceSignalRView TData="StockPrice"
                     HubUrl="@hubUrl"
                     MethodName="ReceiveStockPrice">
    <Loading>
        <div>Connecting to stock updates...</div>
    </Loading>
    <Connected Context="price">
        <div>
            <h3>@price.Symbol: $@price.Price</h3>
            <small>Updated: @price.Timestamp</small>
        </div>
    </Connected>
    <Disconnected>
        <div class="alert alert-warning">Connection lost. Reconnecting...</div>
    </Disconnected>
</ResourceSignalRView>
```

## SignalBus (FluentSignals.SignalBus)

### Publishing Events

```csharp
// Define an event
public class OrderPlacedEvent
{
    public int OrderId { get; set; }
    public decimal Total { get; set; }
}

// Publish
@inject ISignalPublisher Publisher

private void PlaceOrder()
{
    var order = new OrderPlacedEvent { OrderId = 123, Total = 99.99m };
    Publisher.Publish(order);
}
```

### Subscribing to Events

```csharp
@inject ISignalConsumer<OrderPlacedEvent> OrderConsumer
@implements IDisposable

@code {
    private IDisposable? subscription;
    
    protected override void OnInitialized()
    {
        subscription = OrderConsumer.Subscribe(order =>
        {
            Console.WriteLine($"Order {order.OrderId} placed for ${order.Total}");
            InvokeAsync(StateHasChanged);
        });
    }
    
    public void Dispose() => subscription?.Dispose();
}
```

## Common Patterns and Best Practices

### 1. Resource Lifecycle Management

```csharp
public class DataComponent : ComponentBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    protected override void OnInitialized()
    {
        // Add all subscriptions to composite
        _disposables.Add(signal1.Subscribe(HandleSignal1));
        _disposables.Add(signal2.Subscribe(HandleSignal2));
    }
    
    public void Dispose() => _disposables.Dispose();
}
```

### 2. Error Handling Pattern

```csharp
public class ApiService
{
    private readonly HttpResource _resource;
    
    public ApiService(HttpResource resource)
    {
        _resource = resource;
        
        // Global error handling
        _resource
            .OnUnauthorized(async _ => await HandleUnauthorized())
            .OnServerError(async _ => await HandleServerError());
    }
    
    public async Task<T?> SafeGetAsync<T>(string url)
    {
        try
        {
            var response = await _resource.GetAsync<T>(url);
            return response.IsSuccess ? response.Data : default;
        }
        catch (Exception ex)
        {
            // Log error
            return default;
        }
    }
}
```

### 3. Caching Pattern

```csharp
// Configure caching
services.AddMemoryCache();
services.AddFluentSignalsHttp(options =>
{
    options.CacheProvider = new MemoryCacheProvider(memoryCache);
});

// Use cached requests
var cachedRequest = resource
    .Get<Product>("/products/1")
    .WithCaching(TimeSpan.FromMinutes(5));
    
var product = await cachedRequest.ExecuteAsync();
```

### 4. Bulk Operations

```csharp
public class ProductApiResource : TypedHttpResourceWithBulk
{
    public async Task<BulkResult<Product>> CreateProductsAsync(List<Product> products)
    {
        return await BulkPost<Product, Product>("/products/bulk", products)
            .WithBatchSize(50)
            .OnBatchError((batch, error) => 
            {
                Console.WriteLine($"Batch failed: {error.Message}");
            })
            .ExecuteAsync();
    }
}
```

## Testing

### Mocking HTTP Resources

```csharp
// Create mock request
var mockRequest = new MockHttpResourceRequest<User>(
    new User { Id = 1, Name = "Test User" },
    HttpStatusCode.OK
);

// Use in tests
var result = await mockRequest.ExecuteAsync();
Assert.NotNull(result.Data);
Assert.Equal("Test User", result.Data.Name);
```

### Testing Signals

```csharp
[Fact]
public void Signal_Should_Notify_Subscribers()
{
    var signal = new Signal<int>(0);
    var values = new List<int>();
    
    using var subscription = signal.Subscribe(v => values.Add(v));
    
    signal.Value = 1;
    signal.Value = 2;
    
    Assert.Equal([0, 1, 2], values);
}
```

## Configuration Examples

### Complete Setup in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add FluentSignals
builder.Services.AddFluentSignalsHttp(options =>
{
    options.Timeout = TimeSpan.FromSeconds(30);
    options.DefaultHeaders = new Dictionary<string, string>
    {
        ["User-Agent"] = "MyApp/1.0"
    };
});

// Add typed resources
builder.Services.AddTypedHttpResource<UserApiResource>("https://api.example.com");
builder.Services.AddTypedHttpResource<ProductApiResource>("https://api.example.com");

// Add SignalBus
builder.Services.AddFluentSignalsBlazorWithSignalBus();

// Register consumers
builder.Services.AddSignalConsumer<OrderPlacedEvent>();
builder.Services.AddSignalConsumer<UserNotification>();

// Add caching
builder.Services.AddMemoryCache();
builder.Services.Configure<HttpResourceOptions>(options =>
{
    options.CacheProvider = new MemoryCacheProvider(
        builder.Services.BuildServiceProvider().GetRequiredService<IMemoryCache>()
    );
});

var app = builder.Build();
```

## Key Points for AI Assistants

1. **Always dispose subscriptions** - Use IDisposable pattern or CompositeDisposable
2. **Prefer typed resources** - Use TypedHttpResource for type-safe API clients
3. **Handle loading states** - Always check IsLoading before accessing data
4. **Use appropriate signals** - Signal for sync, AsyncSignal for async operations
5. **Leverage DI** - Register services and use dependency injection
6. **Error handling** - Use OnError handlers or try-catch blocks
7. **Blazor integration** - Call StateHasChanged() in subscriptions
8. **Caching** - Use caching for expensive or frequent requests
9. **Testing** - Use mock implementations for unit tests
10. **Resource cleanup** - Dispose resources and connections properly

## Common Issues and Solutions

1. **StateHasChanged not called**: Wrap in InvokeAsync(StateHasChanged)
2. **Memory leaks**: Always dispose subscriptions
3. **Null reference**: Check HasData before accessing Data
4. **Connection issues**: Use automatic reconnect for SignalR
5. **Race conditions**: Use CancellationToken for async operations

This guide provides a comprehensive overview of FluentSignals for AI assistants to effectively help users implement reactive patterns in their .NET applications.