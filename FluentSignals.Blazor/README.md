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

## Components

### HttpResourceView

A component for displaying HTTP resources with built-in loading, error, and success states. The component exposes a `Resource` property that provides access to the underlying `HttpResource` for advanced scenarios like custom event handling and manual refresh.

### ResourceSignalView

A generic component for displaying any async resource with automatic state management.

### ResourceSignalRView

A specialized component for SignalR real-time data with connection status display.

### SignalComponentBase

A base component class that provides automatic signal integration and lifecycle management.

## Typed HTTP Resources with Factory

Create strongly-typed HTTP resource classes with automatic dependency injection:

```csharp
// Define your resource with the HttpResource attribute
[HttpResource("/api/users")]
public class UserResource : TypedHttpResource
{
    // Parameterless constructor required for factory
    public UserResource() { }
    
    public HttpResourceRequest<User> GetById(int id) => 
        Get<User>($"{BaseUrl}/{id}");
    
    public HttpResourceRequest<IEnumerable<User>> GetAll() => 
        Get<IEnumerable<User>>(BaseUrl);
    
    public HttpResourceRequest<User> Create(User user) => 
        Post<User>(BaseUrl, user);
    
    public HttpResourceRequest<User> Update(int id, User user) => 
        Put<User>($"{BaseUrl}/{id}", user);
    
    public HttpResourceRequest Delete(int id) => 
        Delete($"{BaseUrl}/{id}");
}
```

Register and use typed resources with factory:

```csharp
// Registration in Program.cs
services.AddFluentSignalsBlazor(options => 
{
    options.BaseUrl = "https://api.example.com";
});
services.AddTypedHttpResourceFactory<UserResource>();

// Usage in components
@inject ITypedHttpResourceFactory<UserResource> UserFactory

@code {
    private UserResource? users;
    private HttpResource? userResource;
    
    protected override async Task OnInitializedAsync()
    {
        // Create resource with DI-configured HttpClient
        users = UserFactory.Create();
        
        // Or create with custom options
        users = UserFactory.Create(options => 
        {
            options.Timeout = TimeSpan.FromSeconds(60);
        });
        
        // Execute requests
        userResource = await users.GetById(123).ExecuteAsync();
        userResource.OnSuccess(() => ShowNotification("User loaded!"));
    }
}
```

Alternatively, inject the resource directly:

```csharp
@inject UserResource Users

@code {
    protected override async Task OnInitializedAsync()
    {
        var resource = await Users.GetById(123).ExecuteAsync();
        resource.OnSuccess(() => ShowNotification("User loaded!"));
    }
}
```

### Advanced Typed Resources

Create fully typed custom methods for complex scenarios:

```csharp
[HttpResource("/api/v2")]
public class AdvancedApiResource : TypedHttpResource
{
    public AdvancedApiResource() { }
    
    // Typed search with complex criteria
    public HttpResourceRequest<SearchResult<Product>> SearchProducts(ProductSearchCriteria criteria)
    {
        return Post<ProductSearchCriteria, SearchResult<Product>>($"{BaseUrl}/products/search", criteria)
            .WithHeader("X-Search-Version", "2.0")
            .ConfigureResource(r => 
            {
                r.OnSuccess(result => Console.WriteLine($"Found {result.Data.TotalCount} products"));
                r.OnNotFound(() => Console.WriteLine("No products found"));
            });
    }
    
    // Batch operations with progress tracking
    public HttpResourceRequest<BatchResult> ProcessBatch(BatchRequest batch)
    {
        return Post<BatchRequest, BatchResult>($"{BaseUrl}/batch", batch)
            .WithHeader("X-Batch-Id", Guid.NewGuid().ToString())
            .ConfigureResource(r => 
            {
                r.IsLoading.Subscribe(loading => 
                {
                    if (loading) ShowProgress("Processing batch...");
                    else HideProgress();
                });
            });
    }
    
    // File upload with typed metadata
    public HttpResourceRequest<UploadResult> UploadFile(Stream file, FileMetadata metadata)
    {
        return BuildRequest<UploadResult>($"{BaseUrl}/files")
            .WithMethod(HttpMethod.Post)
            .WithBody(new { file, metadata })
            .WithHeader("Content-Type", "multipart/form-data")
            .WithQueryParam("category", metadata.Category)
            .Build()
            .ConfigureResource(r => r.OnServerError(() => ShowError("Upload failed")));
    }
}

// Usage in component
@inject ITypedHttpResourceFactory<AdvancedApiResource> ApiFactory

@code {
    private async Task SearchProducts()
    {
        var api = ApiFactory.Create();
        var criteria = new ProductSearchCriteria 
        { 
            Query = searchText, 
            MinPrice = 10, 
            MaxPrice = 100 
        };
        
        var resource = await api.SearchProducts(criteria).ExecuteAsync();
        // Resource will handle success/error states automatically
    }
}
```

## SignalBus

The SignalBus provides a publish/subscribe pattern for component communication with support for both standard and queue-based subscriptions.

## Documentation

For detailed documentation and examples, visit our [GitHub repository](https://github.com/andres-m-rodriguez/FluentSignals).

## License

This project is licensed under the MIT License.