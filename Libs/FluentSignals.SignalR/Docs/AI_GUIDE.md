# FluentSignals.SignalR - AI Guide

This guide helps AI assistants understand and use the FluentSignals.SignalR library, which provides reactive SignalR integration for real-time communication.

## Overview

FluentSignals.SignalR provides:
- **ResourceSignalR<T>** - Reactive SignalR hub connections
- **Automatic reconnection** - Built-in connection resilience
- **State management** - Connection state tracking
- **Type-safe messaging** - Strongly-typed hub methods

## Core Components

### 1. ResourceSignalR<T>

```csharp
// Create a SignalR resource
var stockPriceResource = new ResourceSignalR<StockPrice>(
    hubUrl: "https://api.example.com/hubs/stock",
    methodName: "ReceiveStockPrice",
    fetcher: async (ct) => 
    {
        // Optional: Load initial data
        var response = await httpClient.GetFromJsonAsync<StockPrice>("/api/stock/latest", ct);
        return response ?? new StockPrice();
    },
    configureConnection: builder =>
    {
        // Configure the hub connection
        builder
            .WithAutomaticReconnect()
            .WithUrl(url, options =>
            {
                options.AccessTokenProvider = async () => await GetAccessToken();
            });
    },
    logger: logger
);

// Start the connection
await stockPriceResource.StartAsync();

// Subscribe to updates
var subscription = stockPriceResource.Subscribe(state =>
{
    if (state.HasData)
    {
        Console.WriteLine($"Stock price: {state.Data.Symbol} - ${state.Data.Price}");
    }
});

// Stop when done
await stockPriceResource.StopAsync();
```

### 2. Connection State Management

```csharp
// Access connection state
var connectionState = stockPriceResource.ConnectionState;

// Subscribe to connection state changes
connectionState.Subscribe(state =>
{
    switch (state)
    {
        case HubConnectionState.Connecting:
            Console.WriteLine("Connecting to hub...");
            break;
        case HubConnectionState.Connected:
            Console.WriteLine("Connected to hub");
            break;
        case HubConnectionState.Reconnecting:
            Console.WriteLine("Connection lost, reconnecting...");
            break;
        case HubConnectionState.Disconnected:
            Console.WriteLine("Disconnected from hub");
            break;
    }
});

// Check current state
if (stockPriceResource.IsConnected)
{
    // Connection is active
}
```

### 3. Multiple Method Subscriptions

```csharp
public class ChatHubResource : ResourceSignalR<ChatMessage>
{
    private readonly HubConnection _hubConnection;
    
    public ChatHubResource(string hubUrl, ILogger<ChatHubResource>? logger = null)
        : base(
            hubUrl: hubUrl,
            methodName: "ReceiveMessage",
            fetcher: null,
            configureConnection: builder => builder.WithAutomaticReconnect(),
            logger: logger)
    {
        // Subscribe to additional methods
        _hubConnection = GetHubConnection(); // Protected property
        
        _hubConnection.On<UserInfo>("UserJoined", user =>
        {
            logger?.LogInformation($"User joined: {user.Name}");
        });
        
        _hubConnection.On<string>("UserLeft", userId =>
        {
            logger?.LogInformation($"User left: {userId}");
        });
        
        _hubConnection.On<string, bool>("UserTyping", (userId, isTyping) =>
        {
            logger?.LogInformation($"User {userId} typing: {isTyping}");
        });
    }
    
    // Send messages to hub
    public async Task SendMessageAsync(string message)
    {
        if (IsConnected)
        {
            await _hubConnection.InvokeAsync("SendMessage", message);
        }
    }
    
    public async Task NotifyTypingAsync(bool isTyping)
    {
        if (IsConnected)
        {
            await _hubConnection.InvokeAsync("NotifyTyping", isTyping);
        }
    }
}
```

### 4. Automatic Reconnection

```csharp
// Configure reconnection delays
var resource = new ResourceSignalR<NotificationData>(
    hubUrl: "https://api.example.com/hubs/notifications",
    methodName: "ReceiveNotification",
    configureConnection: builder =>
    {
        builder.WithAutomaticReconnect(new[]
        {
            TimeSpan.Zero,              // Retry immediately
            TimeSpan.FromSeconds(2),    // Then after 2 seconds
            TimeSpan.FromSeconds(10),   // Then after 10 seconds
            TimeSpan.FromSeconds(30)    // Then after 30 seconds
        });
    }
);

// Custom reconnection policy
public class CustomRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (retryContext.PreviousRetryCount >= 5)
            return null; // Stop retrying after 5 attempts
            
        return TimeSpan.FromSeconds(Math.Pow(2, retryContext.PreviousRetryCount));
    }
}

// Use custom policy
builder.WithAutomaticReconnect(new CustomRetryPolicy());
```

### 5. Hub Method Invocation

```csharp
public class LiveDataResource : ResourceSignalR<LiveData>
{
    public LiveDataResource(string hubUrl) 
        : base(hubUrl, "ReceiveLiveData")
    {
    }
    
    // Subscribe to specific data streams
    public async Task SubscribeToStreamAsync(string streamId)
    {
        if (!IsConnected) 
            await StartAsync();
            
        await InvokeAsync("SubscribeToStream", streamId);
    }
    
    public async Task UnsubscribeFromStreamAsync(string streamId)
    {
        if (IsConnected)
        {
            await InvokeAsync("UnsubscribeFromStream", streamId);
        }
    }
    
    // Get data on demand
    public async Task<T?> RequestDataAsync<T>(string dataId)
    {
        if (!IsConnected)
            return default;
            
        return await InvokeAsync<T>("GetData", dataId);
    }
    
    // Protected helper for hub invocation
    protected async Task InvokeAsync(string methodName, params object?[] args)
    {
        var hub = GetHubConnection();
        await hub.InvokeAsync(methodName, args);
    }
    
    protected async Task<T> InvokeAsync<T>(string methodName, params object?[] args)
    {
        var hub = GetHubConnection();
        return await hub.InvokeAsync<T>(methodName, args);
    }
}
```

## Integration Patterns

### 1. Real-time Dashboard

```csharp
public class DashboardService
{
    private readonly ResourceSignalR<DashboardMetrics> _metricsResource;
    private readonly ResourceSignalR<SystemAlert> _alertResource;
    private readonly ILogger<DashboardService> _logger;
    
    public ISignal<ResourceState<DashboardMetrics>> MetricsState => _metricsResource.State;
    public ISignal<ResourceState<SystemAlert>> AlertState => _alertResource.State;
    
    public DashboardService(string hubUrl, ILogger<DashboardService> logger)
    {
        _logger = logger;
        
        _metricsResource = new ResourceSignalR<DashboardMetrics>(
            hubUrl: hubUrl,
            methodName: "ReceiveMetrics",
            fetcher: async (ct) => await LoadInitialMetrics(ct),
            configureConnection: ConfigureHub,
            logger: logger
        );
        
        _alertResource = new ResourceSignalR<SystemAlert>(
            hubUrl: hubUrl,
            methodName: "ReceiveAlert",
            configureConnection: ConfigureHub,
            logger: logger
        );
    }
    
    private void ConfigureHub(IHubConnectionBuilder builder)
    {
        builder
            .WithAutomaticReconnect()
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Warning);
            });
    }
    
    public async Task StartAsync()
    {
        await Task.WhenAll(
            _metricsResource.StartAsync(),
            _alertResource.StartAsync()
        );
    }
    
    public async Task StopAsync()
    {
        await Task.WhenAll(
            _metricsResource.StopAsync(),
            _alertResource.StopAsync()
        );
    }
}
```

### 2. Chat Application

```csharp
public class ChatService : IAsyncDisposable
{
    private readonly ResourceSignalR<ChatMessage> _messageResource;
    private readonly Signal<List<ChatUser>> _onlineUsers = new(new());
    private readonly Signal<Dictionary<string, bool>> _typingUsers = new(new());
    
    public ISignal<List<ChatUser>> OnlineUsers => _onlineUsers;
    public ISignal<Dictionary<string, bool>> TypingUsers => _typingUsers;
    
    public ChatService(string hubUrl, string currentUserId)
    {
        _messageResource = new ResourceSignalR<ChatMessage>(
            hubUrl: hubUrl,
            methodName: "ReceiveMessage",
            configureConnection: builder =>
            {
                builder
                    .WithAutomaticReconnect()
                    .WithUrl(hubUrl, options =>
                    {
                        options.Headers["UserId"] = currentUserId;
                    });
            }
        );
        
        // Set up additional hub methods
        var hub = _messageResource.GetHubConnection();
        
        hub.On<List<ChatUser>>("UpdateOnlineUsers", users =>
        {
            _onlineUsers.Value = users;
        });
        
        hub.On<string, bool>("UserTypingUpdate", (userId, isTyping) =>
        {
            var typing = new Dictionary<string, bool>(_typingUsers.Value);
            if (isTyping)
                typing[userId] = true;
            else
                typing.Remove(userId);
            _typingUsers.Value = typing;
        });
    }
    
    public async Task SendMessageAsync(string content, string? replyToId = null)
    {
        await _messageResource.InvokeAsync("SendMessage", new
        {
            Content = content,
            ReplyToId = replyToId,
            Timestamp = DateTime.UtcNow
        });
    }
    
    public async Task StartTypingAsync()
    {
        await _messageResource.InvokeAsync("SetTypingStatus", true);
    }
    
    public async Task StopTypingAsync()
    {
        await _messageResource.InvokeAsync("SetTypingStatus", false);
    }
    
    public async ValueTask DisposeAsync()
    {
        await _messageResource.DisposeAsync();
        _onlineUsers.Dispose();
        _typingUsers.Dispose();
    }
}
```

### 3. Live Notifications

```csharp
public class NotificationService
{
    private readonly ResourceSignalR<Notification> _notificationResource;
    private readonly List<Notification> _notifications = new();
    private readonly Signal<int> _unreadCount = new(0);
    
    public ISignal<int> UnreadCount => _unreadCount;
    public IReadOnlyList<Notification> Notifications => _notifications.AsReadOnly();
    
    public NotificationService(string hubUrl, IAuthService authService)
    {
        _notificationResource = new ResourceSignalR<Notification>(
            hubUrl: hubUrl,
            methodName: "ReceiveNotification",
            fetcher: LoadUnreadNotifications,
            configureConnection: builder =>
            {
                builder
                    .WithAutomaticReconnect()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = async () =>
                        {
                            return await authService.GetAccessTokenAsync();
                        };
                    });
            }
        );
        
        // Update internal state when notifications arrive
        _notificationResource.Subscribe(state =>
        {
            if (state.HasData && state.Data != null)
            {
                _notifications.Insert(0, state.Data);
                if (!state.Data.IsRead)
                {
                    _unreadCount.Value++;
                }
                
                // Keep only last 100 notifications
                if (_notifications.Count > 100)
                {
                    _notifications.RemoveRange(100, _notifications.Count - 100);
                }
            }
        });
    }
    
    private async Task<Notification> LoadUnreadNotifications(CancellationToken ct)
    {
        // Load initial unread count
        var unread = await httpClient.GetFromJsonAsync<List<Notification>>(
            "/api/notifications/unread", ct);
        
        if (unread != null)
        {
            _notifications.AddRange(unread);
            _unreadCount.Value = unread.Count(n => !n.IsRead);
        }
        
        return new Notification(); // Return empty for initial state
    }
    
    public async Task MarkAsReadAsync(string notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            _unreadCount.Value = Math.Max(0, _unreadCount.Value - 1);
            
            await _notificationResource.InvokeAsync("MarkAsRead", notificationId);
        }
    }
}
```

## Error Handling

### Connection Error Handling

```csharp
public class ResilientSignalRResource<T> : ResourceSignalR<T>
{
    private readonly Signal<string?> _connectionError = new(null);
    private int _reconnectAttempts = 0;
    
    public ISignal<string?> ConnectionError => _connectionError;
    
    public ResilientSignalRResource(string hubUrl, string methodName)
        : base(hubUrl, methodName)
    {
        // Handle reconnection events
        GetHubConnection().Reconnecting += OnReconnecting;
        GetHubConnection().Reconnected += OnReconnected;
        GetHubConnection().Closed += OnClosed;
    }
    
    private Task OnReconnecting(Exception? exception)
    {
        _reconnectAttempts++;
        _connectionError.Value = $"Connection lost. Reconnecting... (Attempt {_reconnectAttempts})";
        Logger?.LogWarning(exception, "SignalR connection lost, reconnecting");
        return Task.CompletedTask;
    }
    
    private Task OnReconnected(string? connectionId)
    {
        _reconnectAttempts = 0;
        _connectionError.Value = null;
        Logger?.LogInformation($"SignalR reconnected with ID: {connectionId}");
        return Task.CompletedTask;
    }
    
    private Task OnClosed(Exception? exception)
    {
        if (exception != null)
        {
            _connectionError.Value = "Connection closed due to error";
            Logger?.LogError(exception, "SignalR connection closed with error");
        }
        else
        {
            _connectionError.Value = null;
            Logger?.LogInformation("SignalR connection closed");
        }
        return Task.CompletedTask;
    }
    
    public override async ValueTask DisposeAsync()
    {
        var hub = GetHubConnection();
        hub.Reconnecting -= OnReconnecting;
        hub.Reconnected -= OnReconnected;
        hub.Closed -= OnClosed;
        
        await base.DisposeAsync();
        _connectionError.Dispose();
    }
}
```

## Testing

### Testing SignalR Resources

```csharp
public class SignalRResourceTests
{
    [Fact]
    public async Task Should_Update_State_When_Message_Received()
    {
        // Arrange
        var testData = new TestData { Value = "test" };
        var resource = new TestableSignalRResource<TestData>();
        
        var receivedData = new List<TestData>();
        using var subscription = resource.Subscribe(state =>
        {
            if (state.HasData && state.Data != null)
                receivedData.Add(state.Data);
        });
        
        // Act
        await resource.SimulateMessageReceived(testData);
        
        // Assert
        Assert.Single(receivedData);
        Assert.Equal("test", receivedData[0].Value);
    }
}

// Testable implementation
public class TestableSignalRResource<T> : ResourceSignalR<T>
{
    public TestableSignalRResource() 
        : base("mock://hub", "TestMethod", configureConnection: builder =>
        {
            // Configure for testing
        })
    {
    }
    
    public async Task SimulateMessageReceived(T data)
    {
        // Simulate receiving data from hub
        UpdateState(ResourceState<T>.Success(data));
    }
    
    public void SimulateConnectionStateChange(HubConnectionState state)
    {
        ConnectionState.Value = state;
    }
    
    protected override Task<HubConnection> CreateHubConnection()
    {
        // Return mock hub connection for testing
        return Task.FromResult(new MockHubConnection());
    }
}
```

## Best Practices

### 1. Always Handle Connection States

```csharp
// Bad - Assume connection is always active
await hubConnection.InvokeAsync("Method", data);

// Good - Check connection state
if (resource.IsConnected)
{
    await resource.InvokeAsync("Method", data);
}
else
{
    logger.LogWarning("Cannot invoke method - not connected");
}
```

### 2. Configure Automatic Reconnection

```csharp
// Always configure reconnection for production
configureConnection: builder =>
{
    builder
        .WithAutomaticReconnect(new[] 
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30)
        })
        .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information));
}
```

### 3. Dispose Properly

```csharp
public class MyComponent : IAsyncDisposable
{
    private readonly ResourceSignalR<Data> _resource;
    private readonly IDisposable _subscription;
    
    public MyComponent()
    {
        _resource = new ResourceSignalR<Data>("hub", "method");
        _subscription = _resource.Subscribe(HandleData);
    }
    
    public async ValueTask DisposeAsync()
    {
        _subscription?.Dispose();
        await _resource.DisposeAsync();
    }
}
```

### 4. Use Typed Hub Proxies

```csharp
// Define hub interface
public interface IChatHub
{
    Task SendMessage(string message);
    Task JoinRoom(string roomId);
    Task LeaveRoom(string roomId);
}

// Create typed resource
public class TypedChatResource : ResourceSignalR<ChatMessage>
{
    private readonly IHubConnection _typedHub;
    
    public TypedChatResource(string hubUrl)
        : base(hubUrl, "ReceiveMessage")
    {
        _typedHub = GetHubConnection();
    }
    
    public async Task SendMessageAsync(string message)
    {
        await _typedHub.InvokeAsync(nameof(IChatHub.SendMessage), message);
    }
}
```

## Key Takeaways for AI Assistants

1. **ResourceSignalR<T> combines SignalR with reactive signals** - Real-time + reactive
2. **Always configure automatic reconnection** - Essential for production
3. **Handle all connection states** - Connecting, Connected, Reconnecting, Disconnected
4. **Use fetcher for initial data** - Avoid empty state on startup
5. **Dispose properly with ValueTask** - SignalR uses async disposal
6. **Check IsConnected before invoking** - Avoid exceptions
7. **Subscribe to multiple hub methods** - Not limited to one method
8. **Test with mock connections** - Don't require real SignalR server