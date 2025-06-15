# FluentSignals SignalBus

The SignalBus is a messaging system built on top of FluentSignals that provides a publish-subscribe pattern for communication between components in Blazor applications.

## Features

- **Type-safe messaging**: Strongly-typed message publishing and consumption
- **Async support**: Both synchronous and asynchronous message handlers
- **Scoped subscriptions**: Automatic cleanup with component lifecycle
- **Built on FluentSignals**: Leverages the reactive signal system internally

## Installation

Add the SignalBus services to your Blazor application:

```csharp
// In Program.cs
builder.Services.AddFluentSignalsBlazorWithSignalBus();

// Register consumers for specific message types
builder.Services.AddSignalConsumer<MyMessage>();
```

## Usage

### Define Message Types

```csharp
public class UserNotification
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Publishing Messages

```csharp
@inject ISignalPublisher Publisher

@code {
    private void SendNotification()
    {
        var notification = new UserNotification
        {
            Message = "Hello from SignalBus!",
            Timestamp = DateTime.Now
        };
        
        Publisher.Publish(notification);
    }
}
```

### Consuming Messages

```csharp
@inject ISignalConsumer<UserNotification> NotificationConsumer
@implements IDisposable

@code {
    private ISignalSubscriptionContract? _subscription;
    
    protected override void OnInitialized()
    {
        _subscription = NotificationConsumer.Subscribe(notification =>
        {
            // Handle notification
            Console.WriteLine($"Received: {notification.Message}");
            StateHasChanged();
        });
    }
    
    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

### Async Message Handling

```csharp
_subscription = NotificationConsumer.Subscribe(async notification =>
{
    await ProcessNotificationAsync(notification);
    StateHasChanged();
});
```

## Advanced Usage

### Using the Factory

```csharp
@inject SignalConsumerFactory ConsumerFactory

@code {
    private ISignalConsumer<MyMessage> _consumer;
    
    protected override void OnInitialized()
    {
        _consumer = ConsumerFactory.CreateConsumer<MyMessage>();
        // Subscribe to messages...
    }
}
```

### Direct Signal Access

For advanced scenarios, you can access the underlying signal:

```csharp
var signal = consumer.Signal;
// Use signal directly for complex reactive patterns
```

## Architecture

The SignalBus consists of:

- **ISignalPublisher**: Interface for publishing messages
- **ISignalConsumer<T>**: Interface for consuming typed messages
- **SignalBus**: Internal implementation managing signal routing
- **SignalPublisher**: Implementation of ISignalPublisher
- **SignalConsumer<T>**: Implementation wrapping typed signals

All message routing is handled through FluentSignals' TypedSignal<T> instances, providing reactive updates when messages are published.