# FluentSignals.SignalBus

A lightweight, high-performance publish-subscribe messaging system for .NET applications. Designed to work seamlessly with both Blazor WebAssembly and Blazor Server, as well as any other .NET application.

## Features

### Core Features ✅
- **Type-safe messaging** - Strongly typed publish/subscribe pattern
- **Async/Sync handlers** - Support for both `Action<T>` and `Func<T, Task>`
- **Thread-safe** - Built with `ConcurrentDictionary` and `SemaphoreSlim`
- **Error handling** - Built-in error events and resilience
- **Memory efficient** - Automatic cleanup with `IDisposable` and `IAsyncDisposable`
- **Statistics** - Track messages, subscriptions, and errors
- **Blazor compatible** - Works in both WASM and Server modes

## Installation

```bash
dotnet add package FluentSignals.SignalBus
```

## Quick Start

### 1. Register the SignalBus

```csharp
// In Program.cs or Startup.cs
builder.Services.AddSignalBus();

// Or with middleware configuration
builder.Services.AddSignalBus(options =>
{
    options.EnableStatistics = true;
    options.EnableCorrelationId = true;
    
    options.UseMiddleware(pipeline => pipeline
        .UseCorrelationId()
        .UseLogging(LogLevel.Information)
        .UsePerformanceTracking(
            slowMessageThreshold: TimeSpan.FromMilliseconds(100),
            onSlowMessage: (context, elapsed) => 
            {
                logger.LogWarning("Slow message {Type} took {Elapsed}ms", 
                    context.MessageType.Name, elapsed.TotalMilliseconds);
            })
        .UseExceptionHandling(
            swallowExceptions: false,
            onException: (ex, context) =>
            {
                logger.LogError(ex, "Error in SignalBus for {Type}", context.MessageType.Name);
            })
        .UseValidation(validation => validation
            .RegisterValidator<UserLoggedIn>(msg => !string.IsNullOrEmpty(msg.UserId))
            .RegisterValidator<DataUpdated>(msg => msg.NewValue != null))
        .UseCustom("timing", async (context, next) =>
        {
            var sw = Stopwatch.StartNew();
            await next(context);
            logger.LogDebug("Message processed in {Elapsed}ms", sw.ElapsedMilliseconds);
        })
    );
});
```

### 2. Define Message Types

```csharp
public record UserLoggedIn(string UserId, DateTime Timestamp);
public record DataUpdated(string EntityId, object NewValue);
```

### 3. Publish Messages

```csharp
public class LoginService
{
    private readonly ISignalBus _signalBus;

    public LoginService(ISignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public async Task<bool> LoginAsync(string userId, string password)
    {
        // Perform login logic...
        
        // Publish event
        await _signalBus.PublishAsync(new UserLoggedIn(userId, DateTime.UtcNow));
        
        return true;
    }
}
```

### 4. Subscribe to Messages

```csharp
public class NotificationService : IDisposable
{
    private readonly ISignalBus _signalBus;
    private readonly List<SignalBusSubscription> _subscriptions = new();

    public NotificationService(ISignalBus signalBus)
    {
        _signalBus = signalBus;
        Initialize();
    }

    private async void Initialize()
    {
        // Sync handler
        var sub1 = await _signalBus.Subscribe<UserLoggedIn>(user =>
        {
            Console.WriteLine($"User {user.UserId} logged in at {user.Timestamp}");
        });
        _subscriptions.Add(sub1);

        // Async handler
        var sub2 = await _signalBus.SubscribeAsync<DataUpdated>(async data =>
        {
            await ProcessDataUpdateAsync(data);
        });
        _subscriptions.Add(sub2);
    }

    private async Task ProcessDataUpdateAsync(DataUpdated data)
    {
        // Process the update...
        await Task.Delay(100);
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

## Advanced Usage

### Single Subscription (Prevent Duplicates)

```csharp
// Only one subscription per target type is allowed
await _signalBus.SubscribeSingle<UserLoggedIn>(HandleLogin);
await _signalBus.SubscribeSingleAsync<DataUpdated>(HandleDataUpdateAsync);
```

### Error Handling

```csharp
// Subscribe to errors
_signalBus.ErrorOccurred += (sender, context) =>
{
    _logger.LogError(context.Exception, 
        "Error processing {MessageType}: {Message}", 
        context.MessageType.Name, 
        context.Message);
};

// Messages continue to other subscribers even if one fails
```

### Statistics and Monitoring

```csharp
var stats = _signalBus.GetStatistics();

Console.WriteLine($"Total messages: {stats.TotalMessagesPublished}");
Console.WriteLine($"Active subscriptions: {stats.ActiveSubscriptions}");
Console.WriteLine($"Total errors: {stats.TotalErrors}");

foreach (var (type, count) in stats.MessagesByType)
{
    Console.WriteLine($"{type}: {count} messages");
}
```

### Async Disposal

```csharp
await using var subscription = await _signalBus.SubscribeAsync<MyMessage>(HandleAsync);
// Subscription is automatically removed when disposed
```

## Middleware Pipeline

The SignalBus supports a middleware pipeline that runs on every `PublishAsync` call. Middleware can:

- Log messages
- Add correlation IDs
- Validate messages
- Track performance
- Handle exceptions
- Cancel message delivery

### Built-in Middleware

```csharp
builder.Services.AddSignalBus(options =>
{
    options.UseMiddleware(pipeline => pipeline
        // Add correlation ID to messages
        .UseCorrelationId()
        
        // Log all messages
        .UseLogging(LogLevel.Debug)
        
        // Track slow messages
        .UsePerformanceTracking(TimeSpan.FromMilliseconds(50))
        
        // Handle exceptions
        .UseExceptionHandling(swallowExceptions: true)
        
        // Validate messages
        .UseValidation(v => v.RegisterValidator<MyMessage>(IsValid))
    );
});
```

### Custom Middleware

```csharp
public class CustomMiddleware : ISignalBusMiddleware
{
    public async Task InvokeAsync(SignalBusContext context, SignalBusDelegate next)
    {
        // Before message processing
        Console.WriteLine($"Processing {context.MessageType.Name}");
        
        // Call next middleware
        await next(context);
        
        // After message processing
        Console.WriteLine($"Processed {context.MessageType.Name}");
    }
}

// Register it
options.UseMiddleware(pipeline => pipeline.Use<CustomMiddleware>());
```

### Accessing Middleware Context

```csharp
options.UseMiddleware(pipeline => pipeline
    .Use(async (context, next) =>
    {
        // Access context properties
        var messageType = context.MessageType;
        var correlationId = context.CorrelationId;
        var subscriberCount = context.SubscriberCount;
        
        // Store data for other middleware
        context.Items["ProcessingStart"] = DateTime.UtcNow;
        
        await next(context);
        
        // Cancel further processing
        if (someCondition)
            context.IsCancelled = true;
    })
);
```

## Blazor Integration

### In Blazor Components

```csharp
@implements IDisposable
@inject ISignalBus SignalBus

<div>
    <h3>Notifications</h3>
    @foreach (var notification in _notifications)
    {
        <div>@notification</div>
    }
</div>

@code {
    private List<string> _notifications = new();
    private SignalBusSubscription? _subscription;

    protected override async Task OnInitializedAsync()
    {
        _subscription = await SignalBus.SubscribeAsync<UserLoggedIn>(async user =>
        {
            _notifications.Add($"User {user.UserId} logged in");
            await InvokeAsync(StateHasChanged);
        });
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

## Performance Considerations

1. **Thread Safety**: All operations are thread-safe but use locks sparingly
2. **Memory**: Subscriptions hold strong references by default - always dispose them
3. **Async Publishing**: Messages are processed in parallel for better performance
4. **WASM Compatibility**: No timers or background threads that could cause issues

## Roadmap

- [ ] Weak reference support
- [ ] Message filtering and routing
- [ ] Priority queues
- [ ] Batching support
- [ ] Debugging dashboard
- [ ] Distributed messaging (separate package)

## License

MIT License