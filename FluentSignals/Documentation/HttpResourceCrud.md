# HTTP Resource CRUD Operations Guide

This guide provides comprehensive documentation on using FluentSignals' `HttpResource` class for Create, Read, Update, and Delete (CRUD) operations with RESTful APIs.

## Table of Contents
- [Overview](#overview)
- [Setup and Configuration](#setup-and-configuration)
- [CRUD Operations](#crud-operations)
- [Error Handling](#error-handling)
- [Reactive Features](#reactive-features)
- [Advanced Scenarios](#advanced-scenarios)
- [Best Practices](#best-practices)

## Overview

`HttpResource` is a reactive HTTP client that integrates seamlessly with FluentSignals' reactive state management system. It provides:

- Type-safe HTTP operations for all REST verbs (GET, POST, PUT, PATCH, DELETE)
- Automatic state management (loading, error, success states)
- Built-in retry policies with exponential backoff
- Reactive signals for real-time UI updates
- Integration with `IHttpClientFactory` for proper HttpClient lifecycle management

## Setup and Configuration

### Basic Setup (Console/API Applications)

```csharp
// Direct HttpClient usage
var httpClient = new HttpClient 
{ 
    BaseAddress = new Uri("https://api.example.com/") 
};

var resource = new HttpResource(httpClient);
```

### Blazor Setup (Recommended)

```csharp
// Program.cs
builder.Services.AddHttpClient();
builder.Services.AddFluentSignalsBlazor(options =>
{
    options.BaseUrl = "https://api.example.com/";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.DefaultHeaders["User-Agent"] = "MyApp/1.0";
    
    // Configure retry policy
    options.RetryOptions = new RetryOptions
    {
        MaxRetryAttempts = 3,
        InitialRetryDelay = 100,
        UseExponentialBackoff = true,
        RetryableStatusCodes = new List<HttpStatusCode>
        {
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        }
    };
});
```

### Using HttpResourceFactory in Components

```csharp
@inject IHttpResourceFactory ResourceFactory

@code {
    private HttpResource _apiResource;
    
    protected override void OnInitialized()
    {
        // Create with default configuration
        _apiResource = ResourceFactory.Create();
        
        // Or create with custom configuration
        _apiResource = ResourceFactory.CreateWithOptions(options =>
        {
            options.BaseUrl = "https://api.custom.com/";
            options.DefaultHeaders["Authorization"] = $"Bearer {authToken}";
        });
    }
}
```

## CRUD Operations

### Create (POST)

```csharp
public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool Completed { get; set; }
}

// Create a new resource
async Task CreateTodo()
{
    var newTodo = new TodoItem 
    { 
        Title = "Learn FluentSignals",
        Completed = false 
    };
    
    // POST request that returns the created resource
    var response = await _resource.PostAsync<TodoItem, TodoItem>(
        "todos", 
        newTodo
    );
    
    if (response.IsSuccess)
    {
        var createdTodo = response.Data;
        Console.WriteLine($"Created todo with ID: {createdTodo.Id}");
    }
}

// POST without expecting response data
async Task CreateTodoSimple()
{
    var newTodo = new TodoItem { Title = "Simple Task" };
    var response = await _resource.PostAsync("todos", newTodo);
    
    // Check status code
    if (response.StatusCode == HttpStatusCode.Created)
    {
        Console.WriteLine("Todo created successfully");
    }
}
```

### Read (GET)

```csharp
// Get all resources
async Task LoadAllTodos()
{
    var response = await _resource.GetAsync<List<TodoItem>>("todos");
    
    if (response.IsSuccess && response.Data != null)
    {
        foreach (var todo in response.Data)
        {
            Console.WriteLine($"{todo.Id}: {todo.Title}");
        }
    }
}

// Get single resource
async Task LoadTodo(int id)
{
    var response = await _resource.GetAsync<TodoItem>($"todos/{id}");
    
    if (response.IsSuccess)
    {
        var todo = response.Data;
        Console.WriteLine($"Loaded: {todo.Title}");
    }
}

// Get with query parameters
async Task SearchTodos(string query)
{
    var response = await _resource.GetAsync<List<TodoItem>>(
        $"todos?search={Uri.EscapeDataString(query)}&limit=10"
    );
    
    // Process search results
}
```

### Update (PUT)

```csharp
// Full update - replaces entire resource
async Task UpdateTodo(TodoItem todo)
{
    todo.Title = "Updated Title";
    todo.Completed = true;
    
    var response = await _resource.PutAsync<TodoItem, TodoItem>(
        $"todos/{todo.Id}", 
        todo
    );
    
    if (response.IsSuccess)
    {
        Console.WriteLine("Todo updated successfully");
    }
}

// PUT without response data
async Task UpdateTodoSimple(int id, TodoItem todo)
{
    var response = await _resource.PutAsync($"todos/{id}", todo);
    
    if (response.StatusCode == HttpStatusCode.NoContent)
    {
        Console.WriteLine("Update successful");
    }
}
```

### Partial Update (PATCH)

```csharp
// Update only specific fields
async Task ToggleTodoComplete(int id, bool completed)
{
    // Anonymous object for partial update
    var patch = new { completed };
    
    var response = await _resource.PatchAsync<object, TodoItem>(
        $"todos/{id}", 
        patch
    );
    
    if (response.IsSuccess)
    {
        Console.WriteLine($"Todo completion status updated to: {completed}");
    }
}

// Multiple field update
async Task UpdateTodoFields(int id)
{
    var updates = new 
    { 
        title = "Partially Updated",
        priority = "high",
        tags = new[] { "important", "urgent" }
    };
    
    await _resource.PatchAsync($"todos/{id}", updates);
}
```

### Delete (DELETE)

```csharp
// Delete resource
async Task DeleteTodo(int id)
{
    var response = await _resource.DeleteAsync($"todos/{id}");
    
    if (response.StatusCode == HttpStatusCode.NoContent ||
        response.StatusCode == HttpStatusCode.OK)
    {
        Console.WriteLine($"Todo {id} deleted successfully");
    }
}

// Delete with response data (some APIs return the deleted item)
async Task DeleteTodoWithResponse(int id)
{
    var response = await _resource.DeleteAsync<TodoItem>($"todos/{id}");
    
    if (response.IsSuccess && response.Data != null)
    {
        Console.WriteLine($"Deleted: {response.Data.Title}");
    }
}
```

## Error Handling

### Basic Error Handling

```csharp
async Task SafeCreateTodo(TodoItem todo)
{
    try
    {
        var response = await _resource.PostAsync<TodoItem, TodoItem>("todos", todo);
        
        if (!response.IsSuccess)
        {
            // Handle HTTP errors
            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    Console.WriteLine("Invalid todo data");
                    break;
                case HttpStatusCode.Unauthorized:
                    Console.WriteLine("Authentication required");
                    break;
                case HttpStatusCode.Forbidden:
                    Console.WriteLine("Access denied");
                    break;
                default:
                    Console.WriteLine($"Error: {response.StatusCode}");
                    break;
            }
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Network error: {ex.Message}");
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("Request timeout");
    }
}
```

### Using Error Signals

```csharp
protected override void OnInitialized()
{
    _resource = ResourceFactory.Create();
    
    // Subscribe to error signal
    _resource.Error.Subscribe(error =>
    {
        if (error != null)
        {
            _errorMessage = error.Message;
            _errorCode = error.Code;
            StateHasChanged();
        }
    });
}
```

## Reactive Features

### Subscribing to State Changes

```csharp
@implements IDisposable

@code {
    private HttpResource _resource;
    private IDisposable _subscription;
    private bool _isLoading;
    private string _statusMessage;
    
    protected override void OnInitialized()
    {
        _resource = ResourceFactory.Create();
        
        // Subscribe to loading state
        _resource.IsLoading.Subscribe(loading =>
        {
            _isLoading = loading;
            StateHasChanged();
        });
        
        // Subscribe to response updates
        _subscription = _resource.Subscribe(response =>
        {
            if (response?.IsSuccess == true)
            {
                _statusMessage = "Data loaded successfully";
            }
            StateHasChanged();
        });
        
        // Subscribe to status codes
        _resource.LastStatusCode.Subscribe(status =>
        {
            if (status.HasValue)
            {
                Console.WriteLine($"Last status: {status}");
            }
        });
    }
    
    public void Dispose()
    {
        _subscription?.Dispose();
        _resource?.Dispose();
    }
}
```

### Computed Values from HTTP Resources

```csharp
public class TodoService
{
    private readonly HttpResource _resource;
    private readonly ComputedSignal<int> _activeTodoCount;
    
    public TodoService(IHttpResourceFactory factory)
    {
        _resource = factory.Create();
        
        // Compute active todo count from resource data
        _activeTodoCount = new ComputedSignal<int>(() =>
        {
            if (_resource.Value is HttpResponse<List<TodoItem>> response && 
                response.IsSuccess && 
                response.Data != null)
            {
                return response.Data.Count(t => !t.Completed);
            }
            return 0;
        });
    }
    
    public ISignal<int> ActiveTodoCount => _activeTodoCount;
}
```

## Advanced Scenarios

### Batch Operations

```csharp
async Task BatchUpdateTodos(List<TodoItem> todos)
{
    var tasks = todos.Select(todo => 
        _resource.PutAsync<TodoItem, TodoItem>($"todos/{todo.Id}", todo)
    ).ToList();
    
    var responses = await Task.WhenAll(tasks);
    
    var successCount = responses.Count(r => r.IsSuccess);
    Console.WriteLine($"Updated {successCount} of {todos.Count} todos");
}
```

### Request Cancellation

```csharp
private CancellationTokenSource _cts;

async Task LoadDataWithCancellation()
{
    _cts?.Cancel();
    _cts = new CancellationTokenSource();
    
    try
    {
        var response = await _resource.GetAsync<List<TodoItem>>(
            "todos", 
            _cts.Token
        );
        // Process response
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Operation was cancelled");
    }
}
```

### Custom Headers per Request

```csharp
async Task LoadWithCustomHeaders()
{
    // Create a new resource with custom headers for this specific request
    var customResource = ResourceFactory.CreateWithOptions(options =>
    {
        options.DefaultHeaders["X-Custom-Header"] = "SpecialValue";
        options.DefaultHeaders["X-Request-ID"] = Guid.NewGuid().ToString();
    });
    
    await customResource.GetAsync<List<TodoItem>>("todos");
}
```

### Refresh Pattern

```csharp
public class RefreshableTodoList : ComponentBase
{
    private HttpResource _resource;
    private Timer _refreshTimer;
    
    protected override void OnInitialized()
    {
        _resource = ResourceFactory.Create();
        
        // Initial load
        _ = LoadTodos();
        
        // Auto-refresh every 30 seconds
        _refreshTimer = new Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                await _resource.RefreshAsync();
                StateHasChanged();
            });
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    
    async Task LoadTodos()
    {
        await _resource.GetAsync<List<TodoItem>>("todos");
    }
    
    async Task ManualRefresh()
    {
        await _resource.RefreshAsync(); // Re-runs the last request
    }
}
```

## Best Practices

### 1. Resource Lifecycle Management

```csharp
@implements IDisposable

@code {
    private HttpResource _resource;
    private readonly List<IDisposable> _subscriptions = new();
    
    protected override void OnInitialized()
    {
        _resource = ResourceFactory.Create();
        
        // Track all subscriptions
        _subscriptions.Add(_resource.Subscribe(OnResourceUpdate));
        _subscriptions.Add(_resource.IsLoading.Subscribe(OnLoadingChange));
    }
    
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }
        _resource?.Dispose();
    }
}
```

### 2. Error Recovery

```csharp
async Task LoadWithRetry()
{
    const int maxAttempts = 3;
    int attempt = 0;
    
    while (attempt < maxAttempts)
    {
        try
        {
            var response = await _resource.GetAsync<List<TodoItem>>("todos");
            if (response.IsSuccess)
            {
                break;
            }
        }
        catch (Exception ex) when (attempt < maxAttempts - 1)
        {
            attempt++;
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
}
```

### 3. Type-Safe API Client

```csharp
public interface ITodoApi
{
    Task<HttpResponse<List<TodoItem>>> GetTodosAsync();
    Task<HttpResponse<TodoItem>> GetTodoAsync(int id);
    Task<HttpResponse<TodoItem>> CreateTodoAsync(TodoItem todo);
    Task<HttpResponse<TodoItem>> UpdateTodoAsync(int id, TodoItem todo);
    Task<HttpResponse> DeleteTodoAsync(int id);
}

public class TodoApiClient : ITodoApi
{
    private readonly HttpResource _resource;
    
    public TodoApiClient(IHttpResourceFactory factory)
    {
        _resource = factory.CreateWithBaseUrl("https://api.example.com/api/");
    }
    
    public Task<HttpResponse<List<TodoItem>>> GetTodosAsync() =>
        _resource.GetAsync<List<TodoItem>>("todos");
    
    public Task<HttpResponse<TodoItem>> GetTodoAsync(int id) =>
        _resource.GetAsync<TodoItem>($"todos/{id}");
    
    public Task<HttpResponse<TodoItem>> CreateTodoAsync(TodoItem todo) =>
        _resource.PostAsync<TodoItem, TodoItem>("todos", todo);
    
    public Task<HttpResponse<TodoItem>> UpdateTodoAsync(int id, TodoItem todo) =>
        _resource.PutAsync<TodoItem, TodoItem>($"todos/{id}", todo);
    
    public Task<HttpResponse> DeleteTodoAsync(int id) =>
        _resource.DeleteAsync($"todos/{id}");
}
```

### 4. Optimistic Updates

```csharp
public class OptimisticTodoService
{
    private readonly HttpResource _resource;
    private readonly Signal<List<TodoItem>> _todos = new(new List<TodoItem>());
    
    public async Task ToggleTodoOptimistic(TodoItem todo)
    {
        // Optimistically update UI
        todo.Completed = !todo.Completed;
        _todos.Value = new List<TodoItem>(_todos.Value); // Trigger update
        
        try
        {
            // Send update to server
            var response = await _resource.PatchAsync<object, TodoItem>(
                $"todos/{todo.Id}", 
                new { completed = todo.Completed }
            );
            
            if (!response.IsSuccess)
            {
                // Revert on failure
                todo.Completed = !todo.Completed;
                _todos.Value = new List<TodoItem>(_todos.Value);
            }
        }
        catch
        {
            // Revert on error
            todo.Completed = !todo.Completed;
            _todos.Value = new List<TodoItem>(_todos.Value);
            throw;
        }
    }
}
```

## Summary

HttpResource provides a powerful, reactive way to handle CRUD operations in FluentSignals applications. Key benefits include:

- **Type Safety**: Full IntelliSense support and compile-time checking
- **Reactive Updates**: Automatic UI updates through signal subscriptions
- **Error Handling**: Built-in error states and retry policies
- **State Management**: Automatic loading/error/success state tracking
- **Integration**: Seamless integration with Blazor and dependency injection

By following the patterns and practices in this guide, you can build robust, reactive applications with efficient HTTP communication.