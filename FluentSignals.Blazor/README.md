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

A component for displaying HTTP resources with built-in loading, error, and success states. Supports dynamic request building with signal subscriptions for automatic data reloading when signals change.

#### Basic Usage

```razor
<!-- Simple URL-based resource -->
<HttpResourceView T="WeatherData" Url="/api/weather">
    <Success>
        <WeatherDisplay Data="@context" />
    </Success>
</HttpResourceView>

<!-- With custom loading and error states -->
<HttpResourceView T="User[]" Url="/api/users" @ref="userView">
    <Loading>
        <div class="skeleton-loader">Loading users...</div>
    </Loading>
    <ErrorContent>
        <div class="error-panel">
            <p>Failed to load users: @context.Message</p>
        </div>
    </ErrorContent>
    <Success>
        @foreach (var user in context)
        {
            <UserCard User="@user" />
        }
    </Success>
</HttpResourceView>
```

#### Dynamic Requests with Signal Subscriptions

```razor
@code {
    private TypedSignal<string> searchTerm = new("");
    private TypedSignal<int> currentPage = new(1);
    private TypedSignal<string> sortBy = new("name");
}

<!-- Automatically reload when signals change -->
<HttpResourceView T="PagedResult<Product>" 
    DynamicRequestBuilder="@BuildProductRequest"
    SubscribeToSignals="@(new ISignal[] { searchTerm, currentPage, sortBy })">
    <Success>
        <ProductGrid Products="@context.Items" />
        <Pagination TotalPages="@context.TotalPages" 
                    CurrentPage="@currentPage.Value"
                    OnPageChange="@(page => currentPage.Value = page)" />
    </Success>
</HttpResourceView>

@code {
    private HttpRequestMessage BuildProductRequest()
    {
        var url = $"/api/products?search={searchTerm.Value}&page={currentPage.Value}&sort={sortBy.Value}";
        return new HttpRequestMessage(HttpMethod.Get, url);
    }
}
```

#### Parameters

- `Url` - The URL to fetch data from (simple GET requests)
- `RequestBuilder` - Function that builds the HTTP request
- `DynamicRequestBuilder` - Function that builds requests using current signal values
- `SubscribeToSignals` - Array of signals to subscribe to for automatic reloading
- `LoadOnInit` - Whether to load data on component initialization (default: true)
- `ShowRetryButton` - Show retry button on errors (default: true)
- `Loading` - Custom loading content
- `Success` - Content to display when data is loaded
- `Empty` - Content to display when no data is available
- `ErrorContent` - Custom error content
- `OnDataLoaded` - Callback when data is successfully loaded
- `OnError` - Callback when an error occurs
- `OnResourceCreated` - Callback when the resource is created

#### Methods

- `RefreshAsync()` - Manually refresh the data
- `GetResource()` - Get access to the underlying HttpResource

#### Real-World Examples

##### Search with Pagination

```razor
@code {
    private TypedSignal<string> searchTerm = new("");
    private TypedSignal<int> currentPage = new(1);
    private TypedSignal<int> pageSize = new(20);
}

<!-- Search bar is completely outside the component -->
<input type="text" @bind="searchTerm.Value" @bind:event="oninput" 
       placeholder="Search..." class="form-control mb-3" />

<HttpResourceView T="PagedResult<Product>" 
    DynamicRequestBuilder="@(() => new HttpRequestMessage(HttpMethod.Get, 
        $"/api/products?q={searchTerm.Value}&page={currentPage.Value}&size={pageSize.Value}"))"
    SubscribeToSignals="@(new[] { searchTerm, currentPage, pageSize })">
    <Success>
        <!-- Custom rendering of results -->
        @foreach (var product in context.Items)
        {
            <ProductCard Item="@product" />
        }
        
        <!-- Custom pagination controls -->
        <Pagination CurrentPage="@currentPage.Value"
                    TotalPages="@context.TotalPages"
                    OnPageChange="@(page => currentPage.Value = page)" />
    </Success>
</HttpResourceView>
```

##### Cursor-Based Pagination (Infinite Scroll)

```razor
@code {
    private TypedSignal<string?> nextCursor = new(null);
    private List<Post> allPosts = new();
}

<HttpResourceView T="CursorResult<Post>" 
    DynamicRequestBuilder="@(() => new HttpRequestMessage(HttpMethod.Get, 
        $"/api/posts?cursor={nextCursor.Value ?? ""}"))"
    SubscribeToSignals="@(new[] { nextCursor })"
    OnDataLoaded="@(result => { allPosts.AddRange(result.Items); })">
    <Success>
        <div class="posts-container">
            @foreach (var post in allPosts)
            {
                <PostItem Data="@post" />
            }
            
            @if (context.HasMore)
            {
                <button @onclick="() => nextCursor.Value = context.NextCursor">
                    Load More
                </button>
            }
        </div>
    </Success>
</HttpResourceView>
```

##### Advanced Filtering with POST Requests

```razor
@code {
    private TypedSignal<string> category = new("");
    private TypedSignal<decimal?> minPrice = new(null);
    private TypedSignal<decimal?> maxPrice = new(null);
    private TypedSignal<bool> inStock = new(false);
    
    private HttpRequestMessage BuildFilterRequest()
    {
        var filters = new {
            Category = category.Value,
            PriceRange = new { Min = minPrice.Value, Max = maxPrice.Value },
            InStockOnly = inStock.Value
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/products/search");
        request.Content = JsonContent.Create(filters);
        return request;
    }
}

<!-- Filter controls are separate from the component -->
<div class="filters">
    <select @bind="category.Value">
        <option value="">All Categories</option>
        <option value="electronics">Electronics</option>
        <option value="clothing">Clothing</option>
    </select>
    
    <input type="number" @bind="minPrice.Value" placeholder="Min Price" />
    <input type="number" @bind="maxPrice.Value" placeholder="Max Price" />
    
    <label>
        <input type="checkbox" @bind="inStock.Value" />
        In Stock Only
    </label>
</div>

<HttpResourceView T="SearchResult<Product>" 
    DynamicRequestBuilder="@BuildFilterRequest"
    SubscribeToSignals="@(new[] { category, minPrice, maxPrice, inStock })">
    <Success>
        <ProductResults Results="@context" />
    </Success>
</HttpResourceView>
```

The key advantage is that the HttpResourceView component doesn't care about how you build your UI or structure your requests. It simply:
1. Watches the signals you tell it to watch
2. Rebuilds the request using your custom function when any signal changes
3. Automatically fetches the data
4. Provides the results to your custom content

This separation allows complete flexibility in UI design while maintaining reactive data fetching.

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