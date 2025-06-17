# FluentSignals

A powerful reactive state management library for .NET applications inspired by SolidJS signals. FluentSignals provides fine-grained reactivity with automatic dependency tracking, making it perfect for building responsive applications with minimal boilerplate.

## 📦 Latest Version: 1.1.2

### What's New in 1.1.2
- **Custom JSON Serialization**: Configure JsonSerializerOptions for HttpResource
- **Bug Fix**: Resolved duplicate handler calls for typed HTTP status handlers
- **Enhanced Documentation**: Added comprehensive guides for JSON deserialization

See the [full changelog](https://github.com/yourusername/FluentSignals/blob/main/CHANGELOG.md) for version history.

## Features

- 🚀 **Fine-grained reactivity** - Only update what needs to be updated
- 🔄 **Automatic dependency tracking** - No need to manually manage subscriptions
- 📦 **Typed and untyped signals** - Use `Signal<T>` for type safety or `Signal` for flexibility
- ⚡ **Async signals** - Built-in support for asynchronous operations
- 🌊 **Computed signals** - Automatically derive values from other signals
- 🎯 **Resource management** - Generic resource pattern with loading/error states
- 🌐 **HTTP resources** - Built-in HTTP client with caching and retry policies
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

### Resource Signals

```csharp
// Create a resource with a fetcher function
var userResource = new ResourceSignal<User>(
    async (ct) => await LoadUserFromDatabase(userId, ct)
);

// Subscribe to state changes
userResource.Subscribe(state =>
{
    if (state.IsLoading) Console.WriteLine("Loading...");
    if (state.HasData) Console.WriteLine($"User: {state.Data.Name}");
    if (state.HasError) Console.WriteLine($"Error: {state.Error.Message}");
});

// Load the resource
await userResource.LoadAsync();
```

### HTTP Resources

#### Direct Usage
```csharp
// Setup
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
var resource = new HttpResource(httpClient);

// Subscribe to reactive updates
resource.Subscribe(response =>
{
    if (response?.IsSuccess == true)
    {
        // Handle successful response
    }
});

// GET - Fetch data
var todos = await resource.GetAsync<List<TodoItem>>("todos");

// POST - Create new resource
var newTodo = new TodoItem { Title = "New Task" };
var created = await resource.PostAsync<TodoItem, TodoItem>("todos", newTodo);

// PUT - Full update
var updated = new TodoItem { Id = 1, Title = "Updated Task", Completed = true };
var result = await resource.PutAsync<TodoItem, TodoItem>("todos/1", updated);

// PATCH - Partial update
var patch = new { completed = true };
var patched = await resource.PatchAsync<object, TodoItem>("todos/1", patch);

// DELETE - Remove resource
var deleted = await resource.DeleteAsync("todos/1");

// Access reactive signals
resource.IsLoading.Subscribe(loading => Console.WriteLine($"Loading: {loading}"));
resource.Error.Subscribe(error => Console.WriteLine($"Error: {error?.Message}"));
resource.LastStatusCode.Subscribe(status => Console.WriteLine($"Status: {status}"));
```

#### Using HttpResourceFactory (Recommended for Blazor)
```csharp
// Program.cs - Setup
builder.Services.AddHttpClient();
builder.Services.AddFluentSignalsBlazor(options =>
{
    options.BaseUrl = "https://api.example.com/";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.RetryOptions = new RetryOptions
    {
        MaxRetryAttempts = 3,
        InitialRetryDelay = 100,
        UseExponentialBackoff = true
    };
});

// In your Blazor component or service
@inject IHttpResourceFactory ResourceFactory

@code {
    private HttpResource _todosResource;
    
    protected override void OnInitialized()
    {
        // Create with default options from DI
        _todosResource = ResourceFactory.Create();
        
        // Or create with custom base URL
        _todosResource = ResourceFactory.CreateWithBaseUrl("https://api.other.com/");
        
        // Or create with custom options
        _todosResource = ResourceFactory.CreateWithOptions(options =>
        {
            options.Timeout = TimeSpan.FromSeconds(60);
            options.DefaultHeaders["Authorization"] = "Bearer token";
        });
        
        // Subscribe to changes
        _todosResource.Subscribe(response =>
        {
            if (response?.IsSuccess == true)
            {
                StateHasChanged();
            }
        });
    }
    
    // All REST operations work the same way
    async Task LoadTodos() => await _todosResource.GetAsync<List<TodoItem>>("todos");
    
    async Task CreateTodo(TodoItem todo) => 
        await _todosResource.PostAsync<TodoItem, TodoItem>("todos", todo);
    
    async Task UpdateTodo(TodoItem todo) => 
        await _todosResource.PutAsync<TodoItem, TodoItem>($"todos/{todo.Id}", todo);
    
    async Task PatchTodo(int id, object patch) => 
        await _todosResource.PatchAsync<object, TodoItem>($"todos/{id}", patch);
    
    async Task DeleteTodo(int id) => 
        await _todosResource.DeleteAsync($"todos/{id}");
}
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