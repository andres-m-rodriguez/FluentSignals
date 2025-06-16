# FluentSignals.Blazor

Blazor integration for FluentSignals - A powerful reactive state management library. This package provides Blazor-specific components, SignalBus for inter-component communication, and helpers to make working with signals in Blazor applications seamless and efficient.

## Features

- 📡 **SignalBus** - Publish/Subscribe pattern for component communication
- 📬 **Queue-based subscriptions** - Receive messages even if published before subscription
- 🎯 **Resource components** - Display any async resource with loading/error states
- 🔌 **SignalR integration** - Real-time data with ResourceSignalRView
- 🌐 **HTTP Resource components** - Ready-to-use components for HTTP resources
- 🎯 **SignalComponentBase** - Base component class with signal integration
- ⚡ **Automatic UI updates** - Components automatically re-render when signals change
- 🔄 **Lifecycle integration** - Proper subscription cleanup on component disposal

## Installation

```bash
dotnet add package FluentSignals.Blazor
```

## Quick Start

### Basic Setup

```csharp
// Program.cs
builder.Services.AddFluentSignalsBlazor(options =>
{
    options.WithBaseUrl("https://api.example.com")
           .WithTimeout(TimeSpan.FromSeconds(30));
});

// Or with SignalBus
builder.Services.AddFluentSignalsBlazorWithSignalBus();
```

### Using Signals in Components

```razor
@inherits SignalComponentBase

<h3>Counter: @count.Value</h3>
<button @onclick="Increment">Increment</button>

@code {
    private Signal<int> count = new(0);

    private void Increment()
    {
        count.Value++;
    }
}
```

### SignalBus - Publishing Messages

```razor
@inject ISignalPublisher SignalPublisher

<button @onclick="PublishMessage">Send Message</button>

@code {
    private async Task PublishMessage()
    {
        await SignalPublisher.PublishAsync(new UserCreatedEvent 
        { 
            UserId = 123, 
            Name = "John Doe" 
        });
    }
}
```

### SignalBus - Consuming Messages

```razor
@inject ISignalConsumer<UserCreatedEvent> SignalConsumer
@implements IDisposable

<h3>New Users</h3>
<ul>
    @foreach (var user in users)
    {
        <li>@user.Name</li>
    }
</ul>

@code {
    private List<UserCreatedEvent> users = new();
    private IDisposable? subscription;

    protected override void OnInitialized()
    {
        // Standard subscription - only receives new messages
        subscription = SignalConsumer.Subscribe(message =>
        {
            users.Add(message);
            InvokeAsync(StateHasChanged);
        });
    }

    public void Dispose()
    {
        subscription?.Dispose();
    }
}
```

### Queue-based Subscriptions

```razor
@inject ISignalConsumer<NotificationMessage> SignalConsumer

@code {
    protected override void OnInitialized()
    {
        // Queue subscription - receives ALL messages, including those published before subscription
        subscription = SignalConsumer.SubscribeByQueue(async message =>
        {
            notifications.Add(message);
            await InvokeAsync(StateHasChanged);
        }, processExistingMessages: true);
    }
}
```

### Resource Components

```razor
<!-- Generic Resource View -->
<ResourceSignalView TData="Product" Fetcher="@LoadProduct">
    <LoadingContent>
        <div class="spinner">Loading product...</div>
    </LoadingContent>
    <DataContent Context="product">
        <h3>@product.Name</h3>
        <p>Price: @product.Price.ToString("C")</p>
    </DataContent>
    <ErrorContent Context="error">
        <div class="error">@error.Message</div>
    </ErrorContent>
</ResourceSignalView>

<!-- SignalR Resource View -->
<ResourceSignalRView TData="StockPrice" 
                     HubUrl="/stockHub" 
                     MethodName="PriceUpdate"
                     ShowConnectionStatus="true">
    <DataContent Context="stock">
        <div class="stock-price">
            <h4>@stock.Symbol</h4>
            <span class="price">@stock.Price.ToString("C")</span>
            <span class="change @(stock.Change >= 0 ? "up" : "down")">
                @stock.Change.ToString("+0.00;-0.00")
            </span>
        </div>
    </DataContent>
</ResourceSignalRView>

<!-- HTTP Resource View -->
<HttpResourceView TData="User" Url="/api/users/1">
    <LoadingContent>
        <p>Loading user data...</p>
    </LoadingContent>
    <DataContent Context="user">
        <h3>@user.Name</h3>
        <p>Email: @user.Email</p>
    </DataContent>
    <ErrorContent Context="error">
        <p class="error">Failed to load user: @error.Message</p>
    </ErrorContent>
</HttpResourceView>
```

## Advanced Features

### Custom Signal Components

```csharp
public class MyCustomComponent : SignalComponentBase
{
    private ComputedSignal<string> fullName;
    
    protected override void OnInitialized()
    {
        var firstName = new Signal<string>("John");
        var lastName = new Signal<string>("Doe");
        
        fullName = CreateComputed(() => $"{firstName.Value} {lastName.Value}");
        
        // Component will automatically re-render when fullName changes
        WatchSignal(fullName);
    }
}
```

### Service Registration

```csharp
// Register a consumer for a specific message type
builder.Services.AddSignalConsumer<OrderPlacedEvent>();

// The consumer can then be injected into components
@inject ISignalConsumer<OrderPlacedEvent> OrderConsumer
```

## Best Practices

1. **Always dispose subscriptions** - Use `IDisposable` on your components
2. **Use queue subscriptions for cross-page messaging** - Messages persist across navigation
3. **Keep messages immutable** - Create new instances rather than modifying existing ones
4. **Use typed messages** - Create specific classes for different message types

## Documentation

For more examples and detailed documentation, visit our [GitHub repository](https://github.com/yourusername/FluentSignals).

## License

This project is licensed under the MIT License.