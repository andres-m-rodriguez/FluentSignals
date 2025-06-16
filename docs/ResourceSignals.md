# ResourceSignal Documentation

## Overview

ResourceSignal is a reactive state management pattern for handling asynchronous resources in FluentSignals. It provides a standardized way to manage loading states, data, and errors for any type of resource - whether it's data from an API, file system, database, or real-time sources like SignalR.

## Core Concepts

### ResourceState<T>

The foundation of ResourceSignal is the `ResourceState<T>` class, which represents the complete state of a resource at any point in time:

- **IsLoading**: Whether the resource is currently being fetched
- **HasData**: Whether the resource has successfully loaded data
- **HasError**: Whether an error occurred while loading the resource
- **Data**: The actual resource data (when available)
- **Error**: The exception that occurred (when applicable)
- **LastUpdated**: Timestamp of the last state change
- **Metadata**: Additional key-value pairs for custom state information

### ResourceSignal<T>

`ResourceSignal<T>` is a reactive container that:
- Manages the lifecycle of asynchronous resources
- Provides automatic state transitions (loading → success/error)
- Supports manual state updates
- Integrates with FluentSignals' reactivity system
- Handles cancellation of in-flight requests

## Features

### 1. Automatic State Management

ResourceSignal automatically manages state transitions:

```csharp
// Create a resource with a fetcher function
var userResource = new ResourceSignal<User>(
    async (ct) => await apiClient.GetUserAsync(userId, ct)
);

// Load the resource - automatically sets loading state
await userResource.LoadAsync();
// State transitions to either success (with data) or error
```

### 2. Manual State Control

You can also manually control the resource state:

```csharp
// Set loading state
resource.SetLoading();

// Set data
resource.SetData(userData);

// Set error
resource.SetError(new Exception("Failed to load"));

// Clear the resource
resource.Clear();
```

### 3. Reactive Updates

ResourceSignal integrates with FluentSignals' reactivity:

```csharp
// Subscribe to state changes
var subscription = resource.Subscribe(state =>
{
    if (state.IsLoading)
        Console.WriteLine("Loading...");
    else if (state.HasData)
        Console.WriteLine($"Data: {state.Data}");
    else if (state.HasError)
        Console.WriteLine($"Error: {state.Error.Message}");
});
```

### 4. Metadata Support

Store additional information about the resource:

```csharp
resource.SetMetadata("source", "cache");
resource.SetMetadata("retryCount", 3);
resource.SetMetadata("lastFetchDuration", TimeSpan.FromSeconds(1.2));
```

## ResourceSignalR<T>

`ResourceSignalR<T>` extends ResourceSignal with SignalR integration for real-time updates:

### Features

1. **Automatic SignalR Connection Management**
   - Connects to SignalR hubs
   - Handles reconnection logic
   - Tracks connection state in metadata

2. **Real-time Data Updates**
   - Listens to specified hub methods
   - Automatically updates resource data when messages arrive
   - Maintains all ResourceSignal functionality

3. **Bi-directional Communication**
   - Send messages to the hub
   - Invoke hub methods and get responses
   - Full SignalR client capabilities

### Connection States

ResourceSignalR tracks the SignalR connection state:
- **Connecting**: Establishing connection to the hub
- **Connected**: Active connection, receiving updates
- **Reconnecting**: Connection lost, attempting to reconnect
- **Disconnected**: No active connection

### Usage Patterns

```csharp
// Create a SignalR-enabled resource
var stockResource = new ResourceSignalR<StockPrice>(
    hubUrl: "https://api.example.com/stockHub",
    methodName: "ReceiveStockUpdate",
    fetcher: async (ct) => await GetInitialStockPrice(ct)
);

// Connect and start receiving updates
await stockResource.ConnectAsync();

// Send data to the hub
await stockResource.SendAsync("SubscribeToStock", "AAPL");

// Invoke hub method and get result
var marketStatus = await stockResource.InvokeAsync<MarketStatus>("GetMarketStatus");
```

## Blazor Components

### ResourceSignalView

A Blazor component for displaying ResourceSignal data with automatic UI updates:

```razor
<ResourceSignalView TData="User" Fetcher="@LoadUser">
    <LoadingContent>
        <div class="spinner">Loading user...</div>
    </LoadingContent>
    <DataContent Context="user">
        <h3>@user.Name</h3>
        <p>@user.Email</p>
    </DataContent>
    <ErrorContent Context="error">
        <div class="error">Failed to load user: @error.Message</div>
    </ErrorContent>
</ResourceSignalView>
```

### ResourceSignalRView

A specialized component for SignalR-enabled resources with connection status:

```razor
<ResourceSignalRView TData="ChatMessage" 
                     HubUrl="/chatHub" 
                     MethodName="ReceiveMessage"
                     ShowConnectionStatus="true">
    <LoadingContent>
        <p>Connecting to chat...</p>
    </LoadingContent>
    <DataContent Context="message">
        <div class="message">
            <strong>@message.User:</strong> @message.Text
        </div>
    </DataContent>
    <ErrorContent Context="error">
        <p class="error">Chat error: @error.Message</p>
    </ErrorContent>
</ResourceSignalRView>
```

## Benefits

1. **Consistency**: Standardized way to handle all async resources
2. **Type Safety**: Full IntelliSense and compile-time checking
3. **Reactivity**: Automatic UI updates when resource state changes
4. **Error Handling**: Built-in error state management
5. **Cancellation**: Proper handling of cancelled operations
6. **Real-time**: Native SignalR integration for live data
7. **Metadata**: Extensible with custom state information
8. **Testability**: Easy to mock and test resource states

## Common Use Cases

- **API Data Fetching**: Load data from REST APIs with loading/error states
- **Real-time Dashboards**: Display live data from SignalR hubs
- **File Operations**: Track progress of file uploads/downloads
- **Database Queries**: Manage async database operations
- **WebSocket Data**: Handle streaming data with proper state management
- **Caching**: Track cache status and refresh operations
- **Polling**: Implement periodic data updates with state tracking

## Best Practices

1. **Always Handle All States**: Check for loading, data, and error states
2. **Use Metadata Wisely**: Store debugging info, timestamps, retry counts
3. **Dispose Properly**: Clean up subscriptions and connections
4. **Handle Reconnection**: For SignalR resources, implement reconnection UI
5. **Provide User Feedback**: Show appropriate UI for each state
6. **Cancel Operations**: Pass cancellation tokens for long-running operations
7. **Test State Transitions**: Unit test loading → success/error flows