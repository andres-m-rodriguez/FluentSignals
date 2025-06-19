# FluentSignals

[![NuGet](https://img.shields.io/nuget/v/FluentSignals.svg)](https://www.nuget.org/packages/FluentSignals/)
[![NuGet](https://img.shields.io/nuget/v/FluentSignals.Blazor.svg)](https://www.nuget.org/packages/FluentSignals.Blazor/)
[![Build Status](https://github.com/yourusername/FluentSignals/workflows/Build%20and%20Test/badge.svg)](https://github.com/yourusername/FluentSignals/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A powerful reactive state management library for .NET applications inspired by SolidJS signals. FluentSignals provides fine-grained reactivity with automatic dependency tracking, making it perfect for building responsive applications with minimal boilerplate.

## 🚀 Features

- **Fine-grained reactivity** - Only update what needs to be updated
- **Automatic dependency tracking** - No manual subscription management
- **Type-safe signals** - Full IntelliSense and compile-time checking
- **Async signals** - Built-in support for asynchronous operations
- **Computed signals** - Automatically derive values from other signals
- **SignalBus** - Publish/Subscribe pattern for component communication
- **Queue-based subscriptions** - Receive messages even if published before subscription
- **HTTP Resources** - Reactive HTTP data fetching with caching
- **Typed HTTP Resources** - Strongly-typed API clients with reactive features
- **Blazor integration** - Seamless integration with Blazor components

## 📦 Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| FluentSignals | Core reactive state management library | [![NuGet](https://img.shields.io/nuget/v/FluentSignals.svg)](https://www.nuget.org/packages/FluentSignals/) |
| FluentSignals.Blazor | Blazor components and SignalBus | [![NuGet](https://img.shields.io/nuget/v/FluentSignals.Blazor.svg)](https://www.nuget.org/packages/FluentSignals.Blazor/) |

## 🏃 Quick Start

### Installation

```bash
# Core library
dotnet add package FluentSignals

# Blazor integration
dotnet add package FluentSignals.Blazor
```

### Basic Signal Usage

```csharp
// Create a signal
var count = new Signal<int>(0);

// Subscribe to changes
count.Subscribe(value => Console.WriteLine($"Count: {value}"));

// Update the value
count.Value = 1; // Output: Count: 1
```

### Computed Signals

```csharp
var firstName = new Signal<string>("John");
var lastName = new Signal<string>("Doe");

// Automatically updates when dependencies change
var fullName = new ComputedSignal<string>(() => 
    $"{firstName.Value} {lastName.Value}"
);

fullName.Subscribe(name => Console.WriteLine($"Name: {name}"));

firstName.Value = "Jane"; // Output: Name: Jane Doe
```

### HTTP Resources (REST Operations)

```csharp
// Program.cs - Setup
builder.Services.AddHttpClient();
builder.Services.AddFluentSignalsBlazor();
```

```csharp
// Example: Complete REST API integration
public class TodoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public TodoService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public HttpResource CreateResource()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.example.com/");
        
        return new HttpResource(httpClient);
    }
}

// Usage in a component
@code {
    private HttpResource _resource;
    private List<TodoItem> _todos;
    
    protected override void OnInitialized()
    {
        _resource = TodoService.CreateResource();
        
        // Subscribe to changes
        _resource.Subscribe(response =>
        {
            if (response?.IsSuccess == true)
            {
                _todos = response.GetData<List<TodoItem>>();
            }
        });
    }
    
    // GET - Fetch all todos
    async Task LoadTodos()
    {
        await _resource.GetAsync<List<TodoItem>>("todos");
    }
    
    // POST - Create new todo
    async Task CreateTodo()
    {
        var newTodo = new TodoItem { Title = "New Task", Completed = false };
        await _resource.PostAsync<TodoItem, TodoItem>("todos", newTodo);
    }
    
    // PUT - Update entire todo
    async Task UpdateTodo(int id)
    {
        var updatedTodo = new TodoItem { Id = id, Title = "Updated Task", Completed = true };
        await _resource.PutAsync<TodoItem, TodoItem>($"todos/{id}", updatedTodo);
    }
    
    // PATCH - Partial update
    async Task ToggleTodo(int id, bool completed)
    {
        var patch = new { completed };
        await _resource.PatchAsync<object, TodoItem>($"todos/{id}", patch);
    }
    
    // DELETE - Remove todo
    async Task DeleteTodo(int id)
    {
        await _resource.DeleteAsync($"todos/{id}");
    }
}
```

### SignalBus in Blazor

```csharp
// Program.cs
builder.Services.AddFluentSignalsBlazorWithSignalBus();
```

```razor
@* Publisher Component *@
@inject ISignalPublisher SignalPublisher

<button @onclick="PublishMessage">Send Message</button>

@code {
    async Task PublishMessage()
    {
        await SignalPublisher.PublishAsync(new UserCreated { Name = "John" });
    }
}
```

```razor
@* Consumer Component *@
@inject ISignalConsumer<UserCreated> SignalConsumer

@code {
    protected override void OnInitialized()
    {
        // Receives ALL messages, even those published before subscription
        SignalConsumer.SubscribeByQueue(message =>
        {
            Console.WriteLine($"User created: {message.Name}");
        }, processExistingMessages: true);
    }
}
```

## 📖 Documentation

### Signal Types

- **Signal<T>** - Basic reactive value container
- **ComputedSignal<T>** - Derives value from other signals
- **AsyncSignal<T>** - Handles asynchronous operations
- **AsyncTypedSignal<T>** - Async signal with state tracking
- **HttpResource<T>** - Reactive HTTP data fetching

### Blazor Components

- **SignalComponentBase** - Base component with signal integration
- **HttpResourceView** - Component for displaying HTTP resources
- **ResourceView** - Generic resource display component

### SignalBus Features

- **Publish/Subscribe** - Decouple component communication
- **Queue-based Subscriptions** - Never miss a message
- **Type Safety** - Strongly typed message contracts
- **Async Support** - Async message handlers

## 🛠️ Development

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Creating NuGet Packages

```bash
dotnet pack --configuration Release
```

## 🚀 CI/CD

This project uses GitHub Actions for continuous integration and deployment:

- **Build and Test** - Runs on all pull requests
- **Publish to NuGet** - Automatically publishes packages when pushing to master

### Setting up CI/CD

1. Add your NuGet API key as a GitHub secret named `NUGET_API_KEY`
2. Update the package version in `Directory.Build.props`
3. Push to master to trigger automatic publishing

## 📚 Documentation

- [Complete Documentation](FluentSignals/Documentation/)
- [Version History](CHANGELOG.md)
- [HTTP Resource Guide](FluentSignals/Documentation/HttpResourceCrud.md)
- [Async Signals Guide](FluentSignals/Documentation/AsyncSignals.md)
- [JSON Deserialization Guide](FluentSignals/Documentation/HttpResourceDeserialization.md)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by [SolidJS](https://www.solidjs.com/) signals
- Built with ❤️ for the .NET community

## 📞 Support

- 📧 Email: your.email@example.com
- 🐛 Issues: [GitHub Issues](https://github.com/yourusername/FluentSignals/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/yourusername/FluentSignals/discussions)