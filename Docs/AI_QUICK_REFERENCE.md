# FluentSignals AI Quick Reference

Quick lookup guide for AI assistants to provide instant help with FluentSignals.

## Package Installation

```bash
# Core packages
dotnet add package FluentSignals
dotnet add package FluentSignals.Http
dotnet add package FluentSignals.SignalR
dotnet add package FluentSignals.SignalBus
dotnet add package FluentSignals.Blazor

# Additional dependencies
dotnet add package Microsoft.Extensions.Http
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

## Minimal Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add FluentSignals
builder.Services.AddHttpClient();
builder.Services.AddFluentSignalsHttp();
builder.Services.AddFluentSignalsBlazorWithSignalBus();

// Add typed HTTP resources
builder.Services.AddTypedHttpResource<UserApiClient>("https://api.example.com");

var app = builder.Build();

// Configure pipeline
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();
```

## Quick Patterns

### Basic Signal in Component
```razor
@implements IDisposable

<p>Count: @count.Value</p>
<button @onclick="() => count.Value++">Increment</button>

@code {
    private Signal<int> count = new(0);
    private IDisposable? sub;
    
    protected override void OnInitialized()
    {
        sub = count.Subscribe(_ => InvokeAsync(StateHasChanged));
    }
    
    public void Dispose() => sub?.Dispose();
}
```

### HTTP Resource Quick Fetch
```razor
<HttpResourceView TData="User" Url="/api/user/1">
    <Loading>Loading...</Loading>
    <Success Context="user">
        <h3>@user.Name</h3>
    </Success>
    <Error Context="error">
        <p>Error: @error.Message</p>
    </Error>
</HttpResourceView>
```

### Typed HTTP Resource Quick Create
```csharp
public class UserApi : TypedHttpResource
{
    public async Task<User?> GetUserAsync(int id)
    {
        var response = await Get<User>($"/users/{id}").ExecuteAsync();
        return response.Data;
    }
}
```

### SignalBus Quick Pub/Sub
```csharp
// Publish
@inject ISignalPublisher Publisher
Publisher.Publish(new UserLoggedIn { UserId = "123" });

// Subscribe
@inject ISignalConsumer<UserLoggedIn> Consumer
Consumer.Subscribe(evt => Console.WriteLine($"User {evt.UserId} logged in"));
```

### SignalR Quick Connect
```csharp
var resource = new ResourceSignalR<StockPrice>(
    hubUrl: "https://api.example.com/hub",
    methodName: "ReceivePrice",
    fetcher: async ct => await GetInitialPrice()
);
await resource.StartAsync();
```

## Common Gotchas & Solutions

| Problem | Solution |
|---------|----------|
| Component not updating | Wrap in `InvokeAsync(StateHasChanged)` |
| Memory leak | Always dispose subscriptions |
| Null reference | Check `HasData` before accessing `Data` |
| HTTP timeout | Configure `HttpResourceOptions.Timeout` |
| SignalR disconnects | Use `.WithAutomaticReconnect()` |
| Cache not working | Ensure cache provider is registered |
| DI not resolving | Check service registration order |

## Essential Imports

```csharp
// Core
using FluentSignals;
using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;
using FluentSignals.Resources;

// HTTP
using FluentSignals.Http;
using FluentSignals.Http.Options;
using FluentSignals.Http.Resources;
using FluentSignals.Http.Types;

// SignalR
using FluentSignals.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

// SignalBus
using FluentSignals.SignalBus;

// Blazor
using FluentSignals.Blazor.Components;
using FluentSignals.Blazor.Extensions;
```

## Type Reference

### Core Types
- `Signal<T>` - Synchronous reactive value
- `ComputedSignal<T>` - Derived reactive value
- `AsyncSignal<T>` - Async reactive value
- `TypedSignal<T>` - Base typed signal
- `AsyncTypedSignal<T>` - Base async typed signal

### HTTP Types
- `HttpResource` - HTTP client wrapper
- `HttpResponse` - Non-generic response
- `HttpResponse<T>` - Generic typed response
- `TypedHttpResource` - Base class for typed clients
- `HttpResourceOptions` - Configuration options
- `IHttpResourceInterceptor` - Interceptor interface

### SignalR Types
- `ResourceSignalR<T>` - SignalR resource
- `ResourceState<T>` - Resource state container

### SignalBus Types
- `ISignalPublisher` - Event publisher
- `ISignalConsumer<T>` - Event consumer
- `ISignalBus` - Internal bus interface

## Quick Debugging

```csharp
// Check signal value
Console.WriteLine($"Signal value: {mySignal.Value}");

// Check async signal state
Console.WriteLine($"Loading: {asyncSignal.IsLoading.Value}");
Console.WriteLine($"Error: {asyncSignal.Error.Value?.Message}");
Console.WriteLine($"Has data: {asyncSignal.Value != null}");

// HTTP resource state
Console.WriteLine($"Status: {resource.LastStatusCode}");
Console.WriteLine($"Loading: {resource.IsLoading.Value}");

// SignalR connection state
Console.WriteLine($"State: {signalRResource.ConnectionState.Value}");
```

## Performance Tips

1. **Use CompositeDisposable** for multiple subscriptions
2. **Cache HTTP responses** with appropriate TTL
3. **Batch SignalBus events** when possible
4. **Use ValueTask** for SignalR disposal
5. **Implement virtual scrolling** for large lists
6. **Debounce rapid signal updates**
7. **Use ConfigureAwait(false)** in libraries
8. **Dispose resources in reverse order**

## Testing Snippets

```csharp
// Mock HTTP response
var mockRequest = new MockHttpResourceRequest<User>(
    new User { Id = 1, Name = "Test" },
    HttpStatusCode.OK
);

// Test signal updates
var signal = new Signal<int>(0);
var values = new List<int>();
using var sub = signal.Subscribe(v => values.Add(v));
signal.Value = 1;
Assert.Equal([0, 1], values);

// Test async signal
var asyncSignal = new AsyncSignal<string>(async () => "data");
await asyncSignal.LoadAsync();
Assert.Equal("data", asyncSignal.Value);
```

This quick reference provides instant access to the most common FluentSignals patterns and solutions.