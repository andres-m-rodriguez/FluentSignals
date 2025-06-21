# FluentSignals.SignalR

SignalR integration for FluentSignals providing real-time reactive resources with automatic reconnection and state management.

## Installation

```bash
dotnet add package FluentSignals.SignalR
```

## Features

- 🔄 **Real-time Updates** - Reactive resources that update via SignalR
- 🔌 **Auto-reconnection** - Automatic reconnection with configurable retry
- 📡 **Connection State** - Observable connection state management
- 🎯 **Filtered Subscriptions** - Subscribe to specific message types
- 💾 **Message Buffering** - Buffer messages during disconnection
- 🧪 **Testable** - Mock-friendly design for unit testing

## Quick Start

```csharp
using FluentSignals.SignalR;

// Create a SignalR resource
var stockPrices = new ResourceSignalR<StockPrice>(
    hubUrl: "/hubs/stock",
    eventName: "ReceiveStockPrice"
);

// Subscribe to updates
stockPrices.Subscribe(price => 
{
    Console.WriteLine($"{price.Symbol}: ${price.Price}");
});

// Monitor connection state
stockPrices.ConnectionState.Subscribe(state =>
{
    Console.WriteLine($"Connection: {state}");
});

// Start the connection
await stockPrices.StartAsync();

// Stop when done
await stockPrices.StopAsync();
```

## Advanced Usage

### Filtered Subscriptions

```csharp
// Only receive updates for specific stocks
var techStocks = new ResourceSignalR<StockPrice>(
    hubUrl: "/hubs/stock",
    eventName: "ReceiveStockPrice",
    filter: price => price.Sector == "Technology"
);

// Or use method chaining
var appleStock = new ResourceSignalR<StockPrice>("/hubs/stock", "ReceiveStockPrice")
    .WithFilter(price => price.Symbol == "AAPL");
```

### Connection Configuration

```csharp
var resource = new ResourceSignalR<WeatherUpdate>(
    hubUrl: "/hubs/weather",
    eventName: "WeatherUpdate",
    configureConnection: connection =>
    {
        connection.WithAutomaticReconnect(new[] 
        { 
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30)
        });
        
        connection.ServerTimeout = TimeSpan.FromSeconds(30);
        connection.KeepAliveInterval = TimeSpan.FromSeconds(15);
    }
);
```

### Authentication

```csharp
var secureResource = new ResourceSignalR<SecureData>(
    hubUrl: "/hubs/secure",
    eventName: "ReceiveData",
    configureConnection: connection =>
    {
        connection.AccessTokenProvider = async () =>
        {
            var token = await GetAccessTokenAsync();
            return token;
        };
    }
);
```

### Multiple Event Handlers

```csharp
public class ChatResource : ResourceSignalR<ChatMessage>
{
    public ChatResource(string hubUrl) : base(hubUrl, "ReceiveMessage")
    {
        // Handle multiple events
        Connection.On<UserInfo>("UserJoined", user =>
        {
            Console.WriteLine($"{user.Name} joined the chat");
        });
        
        Connection.On<string>("UserLeft", userId =>
        {
            Console.WriteLine($"User {userId} left the chat");
        });
        
        Connection.On<TypingIndicator>("UserTyping", indicator =>
        {
            Console.WriteLine($"{indicator.UserName} is typing...");
        });
    }
    
    // Send messages
    public async Task SendMessageAsync(string message)
    {
        await Connection.InvokeAsync("SendMessage", message);
    }
    
    public async Task NotifyTypingAsync()
    {
        await Connection.InvokeAsync("NotifyTyping");
    }
}
```

### Error Handling and Logging

```csharp
var resource = new ResourceSignalR<Notification>(
    hubUrl: "/hubs/notifications",
    eventName: "ReceiveNotification",
    logger: loggerFactory.CreateLogger<ResourceSignalR<Notification>>()
);

// Handle connection errors
resource.ConnectionError.Subscribe(error =>
{
    logger.LogError(error, "SignalR connection error");
});

// Handle reconnection events
resource.Connection.Reconnecting += error =>
{
    logger.LogWarning("SignalR reconnecting: {Error}", error?.Message);
    return Task.CompletedTask;
};

resource.Connection.Reconnected += connectionId =>
{
    logger.LogInformation("SignalR reconnected: {ConnectionId}", connectionId);
    return Task.CompletedTask;
};
```

### Message Buffering

```csharp
public class BufferedResource<T> : ResourceSignalR<T>
{
    private readonly Queue<T> _messageBuffer = new();
    private readonly int _maxBufferSize;
    
    public BufferedResource(string hubUrl, string eventName, int maxBufferSize = 100)
        : base(hubUrl, eventName)
    {
        _maxBufferSize = maxBufferSize;
    }
    
    protected override void OnMessageReceived(T message)
    {
        if (ConnectionState.Value != HubConnectionState.Connected)
        {
            // Buffer messages during disconnection
            _messageBuffer.Enqueue(message);
            if (_messageBuffer.Count > _maxBufferSize)
            {
                _messageBuffer.Dequeue();
            }
        }
        else
        {
            // Process buffered messages
            while (_messageBuffer.Count > 0)
            {
                base.OnMessageReceived(_messageBuffer.Dequeue());
            }
            base.OnMessageReceived(message);
        }
    }
}
```

## Integration with Blazor

```razor
@using FluentSignals.SignalR
@implements IAsyncDisposable

<h3>Live Stock Prices</h3>

@if (_stockResource?.ConnectionState.Value == HubConnectionState.Connected)
{
    @if (_stockResource.Value != null)
    {
        <div class="stock-price">
            <span class="symbol">@_stockResource.Value.Symbol</span>
            <span class="price">$@_stockResource.Value.Price.ToString("F2")</span>
            <span class="change @(_stockResource.Value.Change >= 0 ? "positive" : "negative")">
                @(_stockResource.Value.Change >= 0 ? "+" : "")@_stockResource.Value.Change.ToString("F2")%
            </span>
        </div>
    }
}
else
{
    <div class="connection-status">
        Connecting... (@_stockResource?.ConnectionState.Value)
    </div>
}

@code {
    private ResourceSignalR<StockPrice>? _stockResource;
    
    protected override async Task OnInitializedAsync()
    {
        _stockResource = new ResourceSignalR<StockPrice>(
            "/hubs/stock",
            "ReceiveStockPrice",
            filter: price => price.Symbol == "AAPL"
        );
        
        _stockResource.Subscribe(_ => InvokeAsync(StateHasChanged));
        
        await _stockResource.StartAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_stockResource != null)
        {
            await _stockResource.StopAsync();
            _stockResource.Dispose();
        }
    }
}
```

## Testing

```csharp
public class SignalRResourceTests
{
    [Fact]
    public async Task ResourceSignalR_UpdatesValueOnMessage()
    {
        // Arrange
        var mockConnection = new Mock<HubConnection>();
        var resource = new TestableResourceSignalR<TestData>(mockConnection.Object);
        var testData = new TestData { Id = 1, Name = "Test" };
        
        // Act
        await resource.SimulateMessage(testData);
        
        // Assert
        Assert.Equal(testData, resource.Value);
        Assert.False(resource.IsLoading.Value);
    }
}

public class TestableResourceSignalR<T> : ResourceSignalR<T>
{
    public TestableResourceSignalR(HubConnection connection) 
        : base("/test", "TestEvent")
    {
        // Override with mock connection
        Connection = connection;
    }
    
    public Task SimulateMessage(T message)
    {
        OnMessageReceived(message);
        return Task.CompletedTask;
    }
}
```

## Best Practices

1. **Dispose Resources**: Always dispose SignalR resources to close connections
2. **Handle Reconnection**: Subscribe to connection state changes
3. **Use Filters**: Filter messages on the client to reduce processing
4. **Authentication**: Configure authentication before starting connection
5. **Error Handling**: Always handle connection errors gracefully
6. **Testing**: Use mock connections for unit testing

## License

Part of the FluentSignals project. See LICENSE for details.