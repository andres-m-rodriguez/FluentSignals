# Getting Started with FluentSignals.SignalR

## Overview

FluentSignals.SignalR provides a reactive wrapper around SignalR Client that follows the same patterns as FluentSignals.Http:

- Middleware pipeline for message processing
- Reactive signals for state management
- Dependency injection support
- Type-safe message handling

## Basic Example

```csharp
// 1. Setup in Program.cs
services.AddSignalRResourceFactory(options =>
{
    options.ReuseConnections = true;
    options.EnableAutomaticReconnect = true;
});

// 2. Create a service
public class NotificationService
{
    private readonly SignalRResourceFactory _factory;
    
    public NotificationService(SignalRResourceFactory factory)
    {
        _factory = factory;
    }
    
    public SignalRResource<Notification> GetNotifications()
    {
        return _factory.Create<Notification>(
            "https://api.example.com/notifications",
            "ReceiveNotification"
        );
    }
}

// 3. Use in your component
var notifications = service.GetNotifications();

// Subscribe to connection state
notifications.IsConnected.Subscribe(connected =>
{
    Console.WriteLine($"Connected: {connected}");
});

// Subscribe to notifications
notifications.SignalValue.Subscribe(notification =>
{
    Console.WriteLine($"New notification: {notification.Message}");
});

// Connect
await notifications.ConnectAsync();
```

## Key Differences from Old ResourceSignalR

### Old Way (ResourceSignalR)
```csharp
var resource = new ResourceSignalR<T>(hubUrl, methodName, fetcher);
await resource.ConnectAsync();
// Limited middleware support
// Mixed responsibilities
```

### New Way (SignalRResource)
```csharp
var resource = factory.Create<T>(hubUrl, methodName)
    .Use<LoggingMiddleware>()
    .Use<CustomMiddleware>()
    .Use(next => (msg, ctx, ct) => { /* inline */ });
    
await resource.ConnectAsync();
// Full middleware pipeline
// Clear separation of concerns
// Better testability
```