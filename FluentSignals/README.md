# FluentSignals

A powerful reactive state management library for .NET applications inspired by SolidJS signals. FluentSignals provides fine-grained reactivity with automatic dependency tracking, making it perfect for building responsive applications with minimal boilerplate.

## Features

- 🚀 **Fine-grained reactivity** - Only update what needs to be updated
- 🔄 **Automatic dependency tracking** - No need to manually manage subscriptions
- 📦 **Typed and untyped signals** - Use `Signal<T>` for type safety or `Signal` for flexibility
- ⚡ **Async signals** - Built-in support for asynchronous operations
- 🌊 **Computed signals** - Automatically derive values from other signals
- 🎯 **Resource management** - HTTP resources with caching and retry policies
- 🔌 **Extensible** - Easy to extend with custom signal types

## Installation

```bash
dotnet add package FluentSignals
```

## Quick Start

### Basic Signal Usage

```csharp
using FluentSignals;

// Create a signal
var count = new Signal<int>(0);

// Subscribe to changes
count.Subscribe(value => Console.WriteLine($"Count is now: {value}"));

// Update the signal
count.Value = 1; // Output: Count is now: 1
count.Value = 2; // Output: Count is now: 2
```

### Computed Signals

```csharp
var firstName = new Signal<string>("John");
var lastName = new Signal<string>("Doe");

// Create a computed signal
var fullName = new ComputedSignal<string>(() => $"{firstName.Value} {lastName.Value}");

fullName.Subscribe(name => Console.WriteLine($"Full name: {name}"));

firstName.Value = "Jane"; // Output: Full name: Jane Doe
```

### Async Signals

```csharp
var asyncSignal = new AsyncSignal<string>(async () => 
{
    await Task.Delay(1000);
    return "Data loaded!";
});

// Access the value
await asyncSignal.GetValueAsync(); // Returns "Data loaded!" after 1 second
```

### HTTP Resources

```csharp
services.AddFluentSignalsHttpResource(options =>
{
    options.WithBaseUrl("https://api.example.com")
           .WithTimeout(TimeSpan.FromSeconds(30));
});

// Create an HTTP resource
var userResource = new HttpResource<User>("/users/1", httpClient);

// Subscribe to state changes
userResource.Subscribe(state =>
{
    if (state.IsLoading) Console.WriteLine("Loading...");
    if (state.HasData) Console.WriteLine($"User: {state.Data.Name}");
    if (state.HasError) Console.WriteLine($"Error: {state.Error}");
});

// Fetch the data
await userResource.LoadAsync();
```

## Advanced Features

### Signal Bus (Pub/Sub)

```csharp
// Publisher
services.AddScoped<ISignalPublisher>();

await signalPublisher.PublishAsync(new UserCreatedEvent { UserId = 123 });

// Consumer
services.AddScoped<ISignalConsumer<UserCreatedEvent>>();

signalConsumer.Subscribe(message => 
{
    Console.WriteLine($"User created: {message.UserId}");
});
```

### Queue-based Subscriptions

```csharp
// Subscribe with message queue - receives all messages, even those published before subscription
subscription = signalConsumer.SubscribeByQueue(message =>
{
    ProcessMessage(message);
}, processExistingMessages: true);
```

## Integration with Blazor

FluentSignals integrates seamlessly with Blazor applications. See the `FluentSignals.Blazor` package for Blazor-specific features and components.

## Documentation

For more detailed documentation, examples, and API reference, visit our [GitHub repository](https://github.com/yourusername/FluentSignals).

## License

This project is licensed under the MIT License - see the LICENSE file for details.