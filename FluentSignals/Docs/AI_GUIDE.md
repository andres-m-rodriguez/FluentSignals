# FluentSignals Core - AI Guide

This guide helps AI assistants understand and use the FluentSignals core library, which provides reactive state management primitives for .NET applications.

## Overview

FluentSignals is the core library that provides:
- **Signal<T>** - Reactive containers for synchronous values
- **ComputedSignal<T>** - Derived signals that update automatically
- **AsyncSignal<T>** - Signals for asynchronous operations
- **ResourceSignal<T>** - Signals with loading/error/data states

## Core Concepts

### 1. Basic Signals

```csharp
// Create a signal
var counter = new Signal<int>(0);

// Subscribe to changes
var subscription = counter.Subscribe(value => 
{
    Console.WriteLine($"Counter changed to: {value}");
});

// Update the value
counter.Value = 5; // Notifies all subscribers

// Always dispose subscriptions
subscription.Dispose();
```

### 2. Typed Signals

```csharp
// TypedSignal is the base class for custom signals
public class UserSignal : TypedSignal<User?>
{
    public UserSignal() : base(null) { }
    
    public void Login(User user) => Value = user;
    public void Logout() => Value = null;
}

// Usage
var userSignal = new UserSignal();
userSignal.Subscribe(user => 
{
    Console.WriteLine(user != null ? $"User: {user.Name}" : "No user");
});
```

### 3. Computed Signals

```csharp
var firstName = new Signal<string>("John");
var lastName = new Signal<string>("Doe");

// Computed signal updates when dependencies change
var fullName = new ComputedSignal<string>(
    () => $"{firstName.Value} {lastName.Value}",
    dependencies: [firstName, lastName]
);

// fullName automatically updates when firstName or lastName changes
firstName.Value = "Jane"; // fullName.Value is now "Jane Doe"
```

### 4. Async Signals

```csharp
// Create async signal with a fetcher function
var weatherSignal = new AsyncSignal<WeatherData>(async () =>
{
    var response = await httpClient.GetFromJsonAsync<WeatherData>("/api/weather");
    return response ?? throw new Exception("Failed to fetch weather");
});

// Check states
if (weatherSignal.IsLoading.Value)
{
    // Show loading indicator
}
else if (weatherSignal.Error.Value != null)
{
    // Show error: weatherSignal.Error.Value.Message
}
else if (weatherSignal.Value != null)
{
    // Show data: weatherSignal.Value
}

// Trigger loading
await weatherSignal.LoadAsync();
```

### 5. Async Typed Signals

```csharp
public class ApiDataSignal<T> : AsyncTypedSignal<T>
{
    private readonly Func<Task<T>> _fetcher;
    
    public ApiDataSignal(Func<Task<T>> fetcher) : base(default)
    {
        _fetcher = fetcher;
    }
    
    public async Task RefreshAsync()
    {
        try
        {
            IsLoading.Value = true;
            Error.Value = null;
            Value = await _fetcher();
        }
        catch (Exception ex)
        {
            Error.Value = ex;
        }
        finally
        {
            IsLoading.Value = false;
        }
    }
}
```

### 6. Resource Signals

```csharp
// ResourceSignal combines loading, data, and error states
var userResource = new ResourceSignal<User>(
    fetcher: async (ct) =>
    {
        var response = await httpClient.GetFromJsonAsync<User>("/api/user", ct);
        return response ?? throw new Exception("User not found");
    }
);

// Access state
var state = userResource.State; // ResourceState<User>
if (state.IsLoading) { /* loading */ }
if (state.HasData) { var user = state.Data; }
if (state.HasError) { var error = state.Error; }

// Load the resource
await userResource.LoadAsync();

// Subscribe to state changes
userResource.Subscribe(state =>
{
    Console.WriteLine($"Loading: {state.IsLoading}, HasData: {state.HasData}");
});
```

## Contracts and Interfaces

### ISignal<T>
```csharp
public interface ISignal<T> : ISignalContract<T>
{
    T Value { get; }
}
```

### IAsyncSignal
```csharp
public interface IAsyncSignal
{
    ISignal<bool> IsLoading { get; }
    ISignal<Exception?> Error { get; }
}
```

### IResource<T>
```csharp
public interface IResource<T> : IAsyncSignal, ISignalContract<T>
{
    Task RefreshAsync();
    Task LoadAsync();
}
```

## Subscription Management

### Using Dispose Pattern
```csharp
public class MyComponent : IDisposable
{
    private readonly IDisposable _subscription;
    private readonly Signal<string> _message = new("Hello");
    
    public MyComponent()
    {
        _subscription = _message.Subscribe(msg => Console.WriteLine(msg));
    }
    
    public void Dispose()
    {
        _subscription?.Dispose();
        _message?.Dispose();
    }
}
```

### Using CompositeDisposable
```csharp
public class MultiSignalComponent : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    public MultiSignalComponent()
    {
        var signal1 = new Signal<int>(0);
        var signal2 = new Signal<string>("");
        
        _disposables.Add(signal1.Subscribe(HandleInt));
        _disposables.Add(signal2.Subscribe(HandleString));
        _disposables.Add(signal1); // Dispose the signal itself
        _disposables.Add(signal2);
    }
    
    private void HandleInt(int value) { }
    private void HandleString(string value) { }
    
    public void Dispose() => _disposables.Dispose();
}
```

## Extension Methods

### Subscribe Extensions
```csharp
// Subscribe with automatic disposal
using (var subscription = signal.SubscribeDisposable(value => 
{
    Console.WriteLine(value);
}))
{
    // Subscription is active
} // Automatically disposed

// Subscribe once
signal.SubscribeOnce(value => 
{
    Console.WriteLine("First value: " + value);
});

// Subscribe with condition
signal.SubscribeWhen(
    value => ProcessValue(value),
    condition: value => value > 0
);
```

## Best Practices

### 1. Always Dispose Subscriptions
```csharp
// Bad
signal.Subscribe(HandleValue); // Memory leak!

// Good
var subscription = signal.Subscribe(HandleValue);
// Later...
subscription.Dispose();
```

### 2. Use Computed Signals for Derived State
```csharp
// Bad - manually updating
var total = new Signal<decimal>(0);
items.Subscribe(itemList => 
{
    total.Value = itemList.Sum(i => i.Price);
});

// Good - automatic updates
var total = new ComputedSignal<decimal>(
    () => items.Value.Sum(i => i.Price),
    [items]
);
```

### 3. Handle All Async Signal States
```csharp
// Bad
var data = asyncSignal.Value; // Might be null!

// Good
if (asyncSignal.IsLoading.Value)
{
    ShowLoading();
}
else if (asyncSignal.Error.Value != null)
{
    ShowError(asyncSignal.Error.Value);
}
else if (asyncSignal.Value != null)
{
    ShowData(asyncSignal.Value);
}
```

### 4. Use Resource Signals for Complex State
```csharp
// Instead of managing multiple signals
var isLoading = new Signal<bool>(false);
var data = new Signal<T?>(null);
var error = new Signal<Exception?>(null);

// Use ResourceSignal
var resource = new ResourceSignal<T>(fetcher);
// Access via resource.State
```

## Common Patterns

### Debounced Updates
```csharp
public class DebouncedSignal<T> : IDisposable
{
    private readonly Signal<T> _source;
    private readonly Signal<T> _debounced;
    private Timer? _timer;
    private readonly int _delayMs;
    
    public ISignal<T> Value => _debounced;
    
    public DebouncedSignal(T initial, int delayMs = 300)
    {
        _source = new Signal<T>(initial);
        _debounced = new Signal<T>(initial);
        _delayMs = delayMs;
        
        _source.Subscribe(value =>
        {
            _timer?.Dispose();
            _timer = new Timer(_ => 
            {
                _debounced.Value = value;
            }, null, _delayMs, Timeout.Infinite);
        });
    }
    
    public void Update(T value) => _source.Value = value;
    
    public void Dispose()
    {
        _timer?.Dispose();
        _source.Dispose();
        _debounced.Dispose();
    }
}
```

### Signal Combining
```csharp
public static class SignalExtensions
{
    public static ComputedSignal<(T1, T2)> Combine<T1, T2>(
        this ISignal<T1> signal1,
        ISignal<T2> signal2)
    {
        return new ComputedSignal<(T1, T2)>(
            () => (signal1.Value, signal2.Value),
            [signal1, signal2]
        );
    }
    
    public static ComputedSignal<TResult> Map<TSource, TResult>(
        this ISignal<TSource> source,
        Func<TSource, TResult> mapper)
    {
        return new ComputedSignal<TResult>(
            () => mapper(source.Value),
            [source]
        );
    }
}

// Usage
var combined = signal1.Combine(signal2);
var mapped = signal.Map(x => x.ToString());
```

## Testing

### Testing Signals
```csharp
[Fact]
public void Signal_Should_Notify_Subscribers()
{
    // Arrange
    var signal = new Signal<int>(0);
    var values = new List<int>();
    
    // Act
    using var subscription = signal.Subscribe(values.Add);
    signal.Value = 1;
    signal.Value = 2;
    signal.Value = 3;
    
    // Assert
    Assert.Equal([0, 1, 2, 3], values);
}

[Fact]
public void ComputedSignal_Should_Update_When_Dependencies_Change()
{
    // Arrange
    var a = new Signal<int>(2);
    var b = new Signal<int>(3);
    var sum = new ComputedSignal<int>(() => a.Value + b.Value, [a, b]);
    
    // Assert initial value
    Assert.Equal(5, sum.Value);
    
    // Act
    a.Value = 5;
    
    // Assert updated value
    Assert.Equal(8, sum.Value);
}
```

### Testing Async Signals
```csharp
[Fact]
public async Task AsyncSignal_Should_Handle_Loading_States()
{
    // Arrange
    var signal = new AsyncSignal<string>(async () =>
    {
        await Task.Delay(10);
        return "data";
    });
    
    // Assert initial state
    Assert.False(signal.IsLoading.Value);
    Assert.Null(signal.Value);
    
    // Act
    var loadTask = signal.LoadAsync();
    
    // Assert loading state
    Assert.True(signal.IsLoading.Value);
    
    await loadTask;
    
    // Assert loaded state
    Assert.False(signal.IsLoading.Value);
    Assert.Equal("data", signal.Value);
}
```

## Performance Considerations

1. **Avoid creating signals in loops** - Create once and update values
2. **Dispose unused subscriptions** - Prevent memory leaks
3. **Use ComputedSignal for derived state** - More efficient than manual updates
4. **Batch updates when possible** - Reduce notification overhead
5. **Consider using ValueTask** for high-frequency async operations

## Key Takeaways for AI Assistants

1. **Signals are the core primitive** - Everything builds on Signal<T>
2. **Always handle disposal** - Memory leaks are the #1 issue
3. **Use the right signal type** - Signal for sync, AsyncSignal for async
4. **Computed signals are powerful** - Automatic dependency tracking
5. **Resource signals simplify state** - Combine loading/error/data
6. **Test the reactive behavior** - Not just the final values
7. **Provide complete examples** - Include disposal patterns