# SignalBus Usage Guide

## Quick Start

### 1. Register SignalBus in Program.cs

```csharp
// Add FluentSignals with SignalBus
builder.Services.AddFluentSignalsBlazorWithSignalBus(options =>
{
    options.WithBaseUrl(builder.HostEnvironment.BaseAddress)
           .WithTimeout(TimeSpan.FromSeconds(30));
});

// Register consumers (optional for component-level consumers)
builder.Services.AddSignalConsumer<PersonForAddDto>();
```

### 2. Create a Message Type

```csharp
public class PersonForAddDto
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

### 3. Publish Messages

```razor
@inject ISignalPublisher SignalPublisher

<button @onclick="AddPerson">Add Person</button>

@code {
    private async Task AddPerson()
    {
        await SignalPublisher.PublishAsync(new PersonForAddDto 
        { 
            Name = "John Doe", 
            Age = 30 
        });
    }
}
```

### 4. Consume Messages

```razor
@inject ISignalConsumer<PersonForAddDto> SignalConsumer
@implements IDisposable

<ul>
    @foreach (var person in people)
    {
        <li>@person.Name - Age: @person.Age</li>
    }
</ul>

@code {
    private List<PersonForAddDto> people = new();
    private IDisposable? subscription;

    protected override void OnInitialized()
    {
        subscription = SignalConsumer.SubscribeDisposable(async message =>
        {
            people.Add(message);
            await InvokeAsync(StateHasChanged);
        });
    }

    public void Dispose()
    {
        subscription?.Dispose();
    }
}
```

## Key Features

- **Type-safe messaging** - Strongly typed publish/subscribe
- **Async support** - Both sync and async publishing
- **Built on FluentSignals** - Uses TypedSignal<T> internally
- **Component-friendly** - Easy to use in Blazor components
- **Auto cleanup** - IDisposable subscriptions

## Advanced Usage

### Multiple Consumers

Multiple components can subscribe to the same message type:

```csharp
// Component 1
subscription = SignalConsumer.SubscribeDisposable(HandleMessage);

// Component 2
subscription = SignalConsumer.SubscribeDisposable(HandleMessage);

// Both will receive the message
```

### Direct Signal Access

For advanced scenarios, access the underlying signal:

```csharp
var signal = SignalConsumer.Signal;
signal.Subscribe(value => { /* custom logic */ });
```

### Factory Pattern

Create consumers dynamically:

```csharp
@inject ISignalConsumerFactory consumerFactory

var consumer = consumerFactory.Create<MyMessage>();
```