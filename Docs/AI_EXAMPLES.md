# FluentSignals AI Examples

This document provides concrete, copy-paste ready examples for common scenarios when using FluentSignals. These examples are designed to help AI assistants provide working code quickly.

## Table of Contents
1. [Basic Signal Patterns](#basic-signal-patterns)
2. [HTTP Resource Patterns](#http-resource-patterns)
3. [Typed HTTP Resources](#typed-http-resources)
4. [Blazor Component Patterns](#blazor-component-patterns)
5. [SignalR Patterns](#signalr-patterns)
6. [SignalBus Patterns](#signalbus-patterns)
7. [Advanced Patterns](#advanced-patterns)

## Basic Signal Patterns

### Counter with Multiple Subscribers
```csharp
public class CounterService
{
    private readonly Signal<int> _counter = new(0);
    
    public ISignal<int> Counter => _counter;
    
    public void Increment() => _counter.Value++;
    public void Decrement() => _counter.Value--;
    public void Reset() => _counter.Value = 0;
}

// In Blazor component
@inject CounterService CounterService
@implements IDisposable

<div>
    <h3>Count: @CounterService.Counter.Value</h3>
    <button @onclick="CounterService.Increment">+</button>
    <button @onclick="CounterService.Decrement">-</button>
    <button @onclick="CounterService.Reset">Reset</button>
</div>

@code {
    private IDisposable? _subscription;
    
    protected override void OnInitialized()
    {
        _subscription = CounterService.Counter.Subscribe(_ => InvokeAsync(StateHasChanged));
    }
    
    public void Dispose() => _subscription?.Dispose();
}
```

### Form State Management
```csharp
public class FormState
{
    public Signal<string> Username { get; } = new("");
    public Signal<string> Email { get; } = new("");
    public Signal<bool> AcceptTerms { get; } = new(false);
    
    public ComputedSignal<bool> IsValid { get; }
    public ComputedSignal<string> Summary { get; }
    
    public FormState()
    {
        IsValid = new ComputedSignal<bool>(
            () => !string.IsNullOrEmpty(Username.Value) && 
                  Email.Value.Contains("@") && 
                  AcceptTerms.Value,
            [Username, Email, AcceptTerms]
        );
        
        Summary = new ComputedSignal<string>(
            () => $"User: {Username.Value}, Email: {Email.Value}",
            [Username, Email]
        );
    }
}

// Usage in component
<EditForm Model="@this">
    <InputText @bind-Value="formState.Username.Value" />
    <InputText @bind-Value="formState.Email.Value" />
    <InputCheckbox @bind-Value="formState.AcceptTerms.Value" />
    
    <button disabled="@(!formState.IsValid.Value)">Submit</button>
    <p>@formState.Summary.Value</p>
</EditForm>
```

### Shopping Cart Pattern
```csharp
public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class ShoppingCartService
{
    private readonly Signal<List<CartItem>> _items = new(new());
    
    public ISignal<List<CartItem>> Items => _items;
    
    public ComputedSignal<decimal> TotalPrice { get; }
    public ComputedSignal<int> ItemCount { get; }
    
    public ShoppingCartService()
    {
        TotalPrice = new ComputedSignal<decimal>(
            () => _items.Value.Sum(i => i.Price * i.Quantity),
            [_items]
        );
        
        ItemCount = new ComputedSignal<int>(
            () => _items.Value.Sum(i => i.Quantity),
            [_items]
        );
    }
    
    public void AddItem(CartItem item)
    {
        var items = new List<CartItem>(_items.Value);
        var existing = items.FirstOrDefault(i => i.ProductId == item.ProductId);
        
        if (existing != null)
            existing.Quantity += item.Quantity;
        else
            items.Add(item);
            
        _items.Value = items;
    }
    
    public void RemoveItem(int productId)
    {
        _items.Value = _items.Value.Where(i => i.ProductId != productId).ToList();
    }
}
```

## HTTP Resource Patterns

### Basic CRUD Operations
```csharp
public class TodoService
{
    private readonly HttpResource _resource;
    
    public TodoService(IHttpResourceFactory factory)
    {
        _resource = factory.CreateWithBaseUrl("https://api.example.com");
    }
    
    public async Task<List<Todo>?> GetAllAsync()
    {
        var response = await _resource.GetAsync<List<Todo>>("/todos");
        return response.IsSuccess ? response.Data : null;
    }
    
    public async Task<Todo?> GetByIdAsync(int id)
    {
        var response = await _resource.GetAsync<Todo>($"/todos/{id}");
        return response.IsSuccess ? response.Data : null;
    }
    
    public async Task<Todo?> CreateAsync(CreateTodoDto dto)
    {
        var response = await _resource.PostAsync<CreateTodoDto, Todo>("/todos", dto);
        return response.IsSuccess ? response.Data : null;
    }
    
    public async Task<bool> UpdateAsync(int id, UpdateTodoDto dto)
    {
        var response = await _resource.PutAsync($"/todos/{id}", dto);
        return response.IsSuccess;
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _resource.DeleteAsync($"/todos/{id}");
        return response.IsSuccess;
    }
}
```

### HTTP Resource with Comprehensive Error Handling
```csharp
public class ApiClient
{
    private readonly HttpResource _resource;
    private readonly ILogger<ApiClient> _logger;
    
    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _logger = logger;
        
        var options = new HttpResourceOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            RetryOptions = new RetryOptions
            {
                MaxRetries = 3,
                DelayMilliseconds = 1000
            }
        };
        
        _resource = new HttpResource(httpClient, options);
        
        // Configure global handlers
        _resource
            .OnUnauthorized(async response =>
            {
                _logger.LogWarning("Unauthorized access attempt");
                // Redirect to login or refresh token
            })
            .OnNotFound(response =>
            {
                _logger.LogWarning($"Resource not found: {response.RequestMessage?.RequestUri}");
                return Task.CompletedTask;
            })
            .OnBadRequest<ValidationError>(error =>
            {
                _logger.LogWarning($"Validation failed: {string.Join(", ", error.Errors)}");
                return Task.CompletedTask;
            })
            .OnServerError(response =>
            {
                _logger.LogError($"Server error: {response.StatusCode}");
                return Task.CompletedTask;
            });
    }
    
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _resource.GetAsync<T>(endpoint);
            return response.IsSuccess ? response.Data : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching {endpoint}");
            return default;
        }
    }
}

public class ValidationError
{
    public List<string> Errors { get; set; } = new();
}
```

## Typed HTTP Resources

### Complete API Client Example
```csharp
[HttpResource("https://api.example.com/v1")]
public class UserApiClient : TypedHttpResource
{
    // GET /users
    public async Task<PagedResult<User>?> GetUsersAsync(int page = 1, int pageSize = 10)
    {
        var response = await Get<PagedResult<User>>("/users", new { page, pageSize })
            .ExecuteAsync();
        return response.Data;
    }
    
    // GET /users/{id}
    public async Task<User?> GetUserAsync(int id)
    {
        var response = await Get<User>($"/users/{id}")
            .OnNotFound(async _ => 
            {
                // Handle not found
            })
            .ExecuteAsync();
        return response.Data;
    }
    
    // POST /users
    public async Task<User?> CreateUserAsync(CreateUserDto dto)
    {
        var response = await Post<CreateUserDto, User>("/users", dto)
            .OnBadRequest<ValidationError>(async error =>
            {
                // Handle validation errors
            })
            .ExecuteAsync();
        return response.Data;
    }
    
    // PUT /users/{id}
    public async Task<User?> UpdateUserAsync(int id, UpdateUserDto dto)
    {
        var response = await Put<UpdateUserDto, User>($"/users/{id}", dto)
            .ExecuteAsync();
        return response.Data;
    }
    
    // DELETE /users/{id}
    public async Task<bool> DeleteUserAsync(int id)
    {
        var response = await Delete($"/users/{id}")
            .ExecuteAsync();
        return response.IsSuccess;
    }
    
    // GET /users/{id}/posts
    public async Task<List<Post>?> GetUserPostsAsync(int userId)
    {
        var response = await Get<List<Post>>($"/users/{userId}/posts")
            .ExecuteAsync();
        return response.Data;
    }
    
    // Custom headers example
    public async Task<User?> GetUserWithCustomHeadersAsync(int id, string apiKey)
    {
        var response = await Get<User>($"/users/{id}")
            .WithHeader("X-API-Key", apiKey)
            .WithHeader("X-Client-Version", "1.0")
            .ExecuteAsync();
        return response.Data;
    }
}

// DTOs
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class CreateUserDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class UpdateUserDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// Registration in Program.cs
builder.Services.AddHttpClient();
builder.Services.AddTypedHttpResource<UserApiClient>();
```

### Typed Resource with Interceptors
```csharp
public class AuthenticatedApiClient : TypedHttpResourceWithInterceptors
{
    private readonly ITokenService _tokenService;
    
    public AuthenticatedApiClient(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }
    
    public override void Initialize(HttpClient httpClient, string baseUrl, HttpResourceOptions options)
    {
        base.Initialize(httpClient, baseUrl, options);
        
        // Add authentication interceptor
        AddInterceptor(new BearerTokenInterceptor(async () => 
        {
            return await _tokenService.GetAccessTokenAsync();
        }));
        
        // Add logging interceptor
        AddInterceptor(new LoggingInterceptor(message => 
        {
            Console.WriteLine($"[API] {message}");
        }));
        
        // Add retry interceptor
        AddInterceptor(new RetryInterceptor(
            maxRetries: 3,
            delayMilliseconds: 1000,
            shouldRetry: (response) => 
            {
                return response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.TooManyRequests;
            }
        ));
    }
    
    public async Task<T?> SecureGetAsync<T>(string endpoint)
    {
        var response = await Get<T>(endpoint).ExecuteAsync();
        return response.Data;
    }
}
```

## Blazor Component Patterns

### Complete Data Table Component
```razor
@typeparam TItem
@implements IDisposable

<div class="data-table">
    @if (isLoading)
    {
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    }
    else if (error != null)
    {
        <div class="alert alert-danger">
            @error.Message
            <button class="btn btn-sm btn-outline-danger" @onclick="Refresh">Retry</button>
        </div>
    }
    else if (items.Any())
    {
        <table class="table">
            <thead>
                <tr>
                    @HeaderTemplate
                </tr>
            </thead>
            <tbody>
                @foreach (var item in items)
                {
                    <tr>
                        @RowTemplate(item)
                    </tr>
                }
            </tbody>
        </table>
        
        @if (totalPages > 1)
        {
            <nav>
                <ul class="pagination">
                    <li class="page-item @(currentPage == 1 ? "disabled" : "")">
                        <button class="page-link" @onclick="() => LoadPage(currentPage - 1)">
                            Previous
                        </button>
                    </li>
                    @for (int i = 1; i <= totalPages; i++)
                    {
                        var page = i;
                        <li class="page-item @(currentPage == page ? "active" : "")">
                            <button class="page-link" @onclick="() => LoadPage(page)">
                                @page
                            </button>
                        </li>
                    }
                    <li class="page-item @(currentPage == totalPages ? "disabled" : "")">
                        <button class="page-link" @onclick="() => LoadPage(currentPage + 1)">
                            Next
                        </button>
                    </li>
                </ul>
            </nav>
        }
    }
    else
    {
        <div class="alert alert-info">
            No data available.
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired] public string ApiEndpoint { get; set; } = "";
    [Parameter] public RenderFragment HeaderTemplate { get; set; } = @<text></text>;
    [Parameter] public RenderFragment<TItem> RowTemplate { get; set; } = _ => @<text></text>;
    [Parameter] public int PageSize { get; set; } = 10;
    [Parameter] public EventCallback<List<TItem>> OnDataLoaded { get; set; }
    
    private HttpResource? resource;
    private List<TItem> items = new();
    private bool isLoading;
    private Exception? error;
    private int currentPage = 1;
    private int totalPages = 1;
    private IDisposable? subscription;
    
    [Inject] private IHttpResourceFactory HttpResourceFactory { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        resource = HttpResourceFactory.Create();
        
        subscription = resource.IsLoading.Subscribe(loading =>
        {
            isLoading = loading;
            InvokeAsync(StateHasChanged);
        });
        
        await LoadPage(1);
    }
    
    private async Task LoadPage(int page)
    {
        if (page < 1 || page > totalPages) return;
        
        currentPage = page;
        error = null;
        
        try
        {
            var response = await resource!.GetAsync<PagedResponse<TItem>>(
                $"{ApiEndpoint}?page={page}&pageSize={PageSize}"
            );
            
            if (response.IsSuccess && response.Data != null)
            {
                items = response.Data.Items;
                totalPages = (int)Math.Ceiling(response.Data.TotalCount / (double)PageSize);
                
                if (OnDataLoaded.HasDelegate)
                    await OnDataLoaded.InvokeAsync(items);
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }
    }
    
    private async Task Refresh() => await LoadPage(currentPage);
    
    public void Dispose() => subscription?.Dispose();
    
    private class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
```

### Form with Validation and Submission
```razor
@inject ISignalPublisher SignalPublisher
@inject UserApiClient UserApi
@implements IDisposable

<EditForm Model="@model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    
    <div class="mb-3">
        <label class="form-label">Name</label>
        <InputText class="form-control" @bind-Value="model.Name" />
        <ValidationMessage For="() => model.Name" />
    </div>
    
    <div class="mb-3">
        <label class="form-label">Email</label>
        <InputText class="form-control" @bind-Value="model.Email" />
        <ValidationMessage For="() => model.Email" />
    </div>
    
    <button type="submit" class="btn btn-primary" disabled="@isSubmitting">
        @if (isSubmitting)
        {
            <span class="spinner-border spinner-border-sm me-2"></span>
        }
        Submit
    </button>
    
    @if (submitError != null)
    {
        <div class="alert alert-danger mt-3">
            @submitError
        </div>
    }
</EditForm>

@code {
    private UserFormModel model = new();
    private bool isSubmitting;
    private string? submitError;
    
    private async Task HandleSubmit()
    {
        isSubmitting = true;
        submitError = null;
        
        try
        {
            var user = await UserApi.CreateUserAsync(new CreateUserDto
            {
                Name = model.Name,
                Email = model.Email
            });
            
            if (user != null)
            {
                // Publish success event
                SignalPublisher.Publish(new UserCreatedEvent { User = user });
                
                // Reset form
                model = new();
            }
            else
            {
                submitError = "Failed to create user.";
            }
        }
        catch (Exception ex)
        {
            submitError = ex.Message;
        }
        finally
        {
            isSubmitting = false;
        }
    }
    
    public class UserFormModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = "";
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = "";
    }
    
    public class UserCreatedEvent
    {
        public User User { get; set; } = null!;
    }
}
```

## SignalR Patterns

### Real-time Notifications Component
```razor
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable

<div class="notifications">
    <div class="notification-header">
        <h5>Notifications</h5>
        <span class="badge bg-primary">@notifications.Count</span>
    </div>
    
    @if (connectionState == HubConnectionState.Connecting)
    {
        <div class="text-muted">Connecting...</div>
    }
    else if (connectionState == HubConnectionState.Connected)
    {
        @foreach (var notification in notifications)
        {
            <div class="notification-item alert alert-@notification.Type.ToLower()">
                <strong>@notification.Title</strong>
                <p>@notification.Message</p>
                <small>@notification.Timestamp.ToString("g")</small>
                <button class="btn-close" @onclick="() => RemoveNotification(notification.Id)"></button>
            </div>
        }
    }
    else
    {
        <div class="text-danger">Disconnected</div>
    }
</div>

@code {
    private ResourceSignalR<Notification>? notificationResource;
    private List<Notification> notifications = new();
    private HubConnectionState connectionState = HubConnectionState.Disconnected;
    private IDisposable? subscription;
    
    protected override async Task OnInitializedAsync()
    {
        notificationResource = new ResourceSignalR<Notification>(
            hubUrl: "https://api.example.com/hubs/notifications",
            methodName: "ReceiveNotification",
            configureConnection: builder =>
            {
                builder
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10) })
                    .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information));
            }
        );
        
        // Subscribe to connection state changes
        notificationResource.ConnectionState.Subscribe(state =>
        {
            connectionState = state;
            InvokeAsync(StateHasChanged);
        });
        
        // Subscribe to notifications
        subscription = notificationResource.Subscribe(state =>
        {
            if (state.HasData && state.Data != null)
            {
                notifications.Insert(0, state.Data);
                
                // Keep only last 10 notifications
                if (notifications.Count > 10)
                    notifications = notifications.Take(10).ToList();
                    
                InvokeAsync(StateHasChanged);
            }
        });
        
        await notificationResource.StartAsync();
    }
    
    private void RemoveNotification(string id)
    {
        notifications.RemoveAll(n => n.Id == id);
    }
    
    public async ValueTask DisposeAsync()
    {
        subscription?.Dispose();
        if (notificationResource != null)
            await notificationResource.DisposeAsync();
    }
    
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "Info"; // Info, Success, Warning, Danger
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

### Live Data Dashboard
```razor
@page "/dashboard"
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable

<h3>Live Dashboard</h3>

<div class="row">
    <div class="col-md-4">
        <StockSignalRView TData="StockPrice"
                          HubUrl="@hubUrl"
                          MethodName="ReceiveStockUpdate"
                          Filter="@(stock => watchList.Contains(stock.Symbol))"
                          ShowConnectionStatus="true">
            <Connected Context="stock">
                <div class="card mb-2">
                    <div class="card-body">
                        <h5 class="card-title">@stock.Symbol</h5>
                        <p class="card-text">
                            <span class="@(stock.Change >= 0 ? "text-success" : "text-danger")">
                                $@stock.Price (@(stock.Change >= 0 ? "+" : "")@stock.Change%)
                            </span>
                        </p>
                        <small class="text-muted">@stock.Timestamp.ToString("HH:mm:ss")</small>
                    </div>
                </div>
            </Connected>
        </StockSignalRView>
    </div>
    
    <div class="col-md-8">
        <ResourceSignalRView TData="SystemMetrics"
                             HubUrl="@hubUrl"
                             MethodName="ReceiveMetrics"
                             Fetcher="LoadInitialMetrics">
            <Connected Context="metrics">
                <div class="metrics-grid">
                    <div class="metric">
                        <h6>CPU Usage</h6>
                        <div class="progress">
                            <div class="progress-bar" style="width: @metrics.CpuUsage%">
                                @metrics.CpuUsage%
                            </div>
                        </div>
                    </div>
                    <div class="metric">
                        <h6>Memory Usage</h6>
                        <div class="progress">
                            <div class="progress-bar" style="width: @metrics.MemoryUsage%">
                                @metrics.MemoryUsage%
                            </div>
                        </div>
                    </div>
                    <div class="metric">
                        <h6>Active Users</h6>
                        <h3>@metrics.ActiveUsers</h3>
                    </div>
                </div>
            </Connected>
        </ResourceSignalRView>
    </div>
</div>

@code {
    private string hubUrl = "https://api.example.com/hubs/live";
    private List<string> watchList = new() { "AAPL", "GOOGL", "MSFT" };
    
    private async Task<SystemMetrics> LoadInitialMetrics(CancellationToken ct)
    {
        // Load initial metrics from API
        var response = await Http.GetFromJsonAsync<SystemMetrics>("/api/metrics", ct);
        return response ?? new SystemMetrics();
    }
    
    [Inject] private HttpClient Http { get; set; } = null!;
    
    public class StockPrice
    {
        public string Symbol { get; set; } = "";
        public decimal Price { get; set; }
        public decimal Change { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class SystemMetrics
    {
        public int CpuUsage { get; set; }
        public int MemoryUsage { get; set; }
        public int ActiveUsers { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## SignalBus Patterns

### Global Event Bus Usage
```csharp
// Events
public class ShoppingCartUpdatedEvent
{
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
}

public class UserLoggedInEvent
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
}

public class NotificationEvent
{
    public string Message { get; set; } = "";
    public NotificationType Type { get; set; }
}

// Shopping Cart Component
@inject ISignalPublisher Publisher
@inject ShoppingCartService CartService

<button @onclick="AddToCart">Add to Cart</button>

@code {
    [Parameter] public Product Product { get; set; } = null!;
    
    private async Task AddToCart()
    {
        await CartService.AddProductAsync(Product);
        
        // Publish cart updated event
        Publisher.Publish(new ShoppingCartUpdatedEvent
        {
            ItemCount = CartService.ItemCount,
            Total = CartService.Total
        });
        
        // Publish notification
        Publisher.Publish(new NotificationEvent
        {
            Message = $"{Product.Name} added to cart",
            Type = NotificationType.Success
        });
    }
}

// Cart Badge Component
@inject ISignalConsumer<ShoppingCartUpdatedEvent> CartConsumer
@implements IDisposable

<span class="badge bg-primary">@itemCount</span>

@code {
    private int itemCount;
    private IDisposable? subscription;
    
    protected override void OnInitialized()
    {
        subscription = CartConsumer.Subscribe(evt =>
        {
            itemCount = evt.ItemCount;
            InvokeAsync(StateHasChanged);
        });
    }
    
    public void Dispose() => subscription?.Dispose();
}

// Notification Component
@inject ISignalConsumer<NotificationEvent> NotificationConsumer
@implements IDisposable

<div class="notifications-container">
    @foreach (var notification in notifications)
    {
        <div class="alert alert-@notification.Type.ToString().ToLower() alert-dismissible">
            @notification.Message
            <button type="button" class="btn-close" @onclick="() => RemoveNotification(notification)"></button>
        </div>
    }
</div>

@code {
    private List<NotificationEvent> notifications = new();
    private IDisposable? subscription;
    
    protected override void OnInitialized()
    {
        subscription = NotificationConsumer.Subscribe(notification =>
        {
            notifications.Add(notification);
            InvokeAsync(StateHasChanged);
            
            // Auto-remove after 5 seconds
            Task.Delay(5000).ContinueWith(_ =>
            {
                RemoveNotification(notification);
            });
        });
    }
    
    private void RemoveNotification(NotificationEvent notification)
    {
        notifications.Remove(notification);
        InvokeAsync(StateHasChanged);
    }
    
    public void Dispose() => subscription?.Dispose();
}
```

## Advanced Patterns

### Composite State Management
```csharp
public class AppState
{
    // User state
    public Signal<User?> CurrentUser { get; } = new(null);
    public ComputedSignal<bool> IsAuthenticated { get; }
    public ComputedSignal<string> UserDisplayName { get; }
    
    // UI state
    public Signal<bool> IsSidebarOpen { get; } = new(true);
    public Signal<string> CurrentTheme { get; } = new("light");
    
    // Application data
    public AsyncSignal<List<Project>> Projects { get; }
    public Signal<Project?> SelectedProject { get; } = new(null);
    
    public AppState(IProjectService projectService)
    {
        IsAuthenticated = new ComputedSignal<bool>(
            () => CurrentUser.Value != null,
            [CurrentUser]
        );
        
        UserDisplayName = new ComputedSignal<string>(
            () => CurrentUser.Value?.Name ?? "Guest",
            [CurrentUser]
        );
        
        Projects = new AsyncSignal<List<Project>>(async () =>
        {
            if (CurrentUser.Value != null)
                return await projectService.GetUserProjectsAsync(CurrentUser.Value.Id);
            return new List<Project>();
        });
        
        // Auto-refresh projects when user changes
        CurrentUser.Subscribe(_ => Projects.LoadAsync());
    }
}

// Usage in layout
@inject AppState AppState
@implements IDisposable

<div class="layout @($"theme-{AppState.CurrentTheme.Value}")">
    <aside class="sidebar @(AppState.IsSidebarOpen.Value ? "open" : "collapsed")">
        <!-- Sidebar content -->
    </aside>
    
    <main>
        @if (AppState.IsAuthenticated.Value)
        {
            <div class="user-info">
                Welcome, @AppState.UserDisplayName.Value
            </div>
        }
        
        @Body
    </main>
</div>

@code {
    private CompositeDisposable disposables = new();
    
    protected override void OnInitialized()
    {
        disposables.Add(AppState.CurrentTheme.Subscribe(_ => InvokeAsync(StateHasChanged)));
        disposables.Add(AppState.IsSidebarOpen.Subscribe(_ => InvokeAsync(StateHasChanged)));
        disposables.Add(AppState.UserDisplayName.Subscribe(_ => InvokeAsync(StateHasChanged)));
    }
    
    public void Dispose() => disposables.Dispose();
}
```

### Offline Support with Caching
```csharp
public class OfflineFirstApiClient : TypedHttpResource
{
    private readonly IDistributedCache _cache;
    private readonly IConnectivityService _connectivity;
    
    public OfflineFirstApiClient(IDistributedCache cache, IConnectivityService connectivity)
    {
        _cache = cache;
        _connectivity = connectivity;
    }
    
    public async Task<T?> GetWithOfflineSupportAsync<T>(string endpoint, TimeSpan cacheDuration)
    {
        var cacheKey = $"api:{endpoint}";
        
        // Try to get from cache first
        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            var cachedData = JsonSerializer.Deserialize<CachedItem<T>>(cached);
            if (cachedData != null && cachedData.ExpiresAt > DateTime.UtcNow)
            {
                return cachedData.Data;
            }
        }
        
        // If online, fetch fresh data
        if (_connectivity.IsOnline)
        {
            try
            {
                var response = await Get<T>(endpoint).ExecuteAsync();
                if (response.IsSuccess && response.Data != null)
                {
                    // Cache the response
                    var cacheItem = new CachedItem<T>
                    {
                        Data = response.Data,
                        ExpiresAt = DateTime.UtcNow.Add(cacheDuration)
                    };
                    
                    await _cache.SetStringAsync(
                        cacheKey,
                        JsonSerializer.Serialize(cacheItem),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = cacheDuration
                        }
                    );
                    
                    return response.Data;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - fall back to cache
            }
        }
        
        // Return cached data even if expired when offline
        if (!string.IsNullOrEmpty(cached))
        {
            var cachedData = JsonSerializer.Deserialize<CachedItem<T>>(cached);
            return cachedData?.Data;
        }
        
        return default;
    }
    
    private class CachedItem<T>
    {
        public T? Data { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
```

These examples provide comprehensive patterns for using FluentSignals in real-world applications. Each example is complete and can be adapted to specific use cases.