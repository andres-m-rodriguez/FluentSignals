# FluentSignals.Blazor - AI Guide

This guide helps AI assistants understand and use the FluentSignals.Blazor library, which provides Blazor-specific components and extensions for reactive UI development.

## Overview

FluentSignals.Blazor provides:
- **Blazor Components** - Pre-built reactive components
- **Signal Extensions** - Blazor-specific signal helpers
- **HTTP Resource Components** - Data-fetching UI components
- **SignalR Components** - Real-time UI components
- **Form Binding** - Two-way binding with signals
- **Lifecycle Integration** - Automatic subscription management

## Core Components

### 1. SignalComponentBase

```csharp
// Base class for signal-aware components
public class CounterComponent : SignalComponentBase
{
    private Signal<int> _counter = new(0);
    
    protected override void OnInitialized()
    {
        // Subscribe automatically handles StateHasChanged
        Subscribe(_counter);
    }
    
    private void IncrementCounter() => _counter.Value++;
}

// In Razor
@inherits SignalComponentBase

<p>Count: @_counter.Value</p>
<button @onclick="IncrementCounter">Increment</button>
```

### 2. HttpResourceView Component

```razor
<!-- Basic usage -->
<HttpResourceView TData="User" Url="/api/users/1">
    <Loading>
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    </Loading>
    <Success Context="user">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title">@user.Name</h5>
                <p class="card-text">@user.Email</p>
            </div>
        </div>
    </Success>
    <Error Context="error">
        <div class="alert alert-danger">
            <strong>Error!</strong> @error.Message
        </div>
    </Error>
</HttpResourceView>

<!-- With custom configuration -->
<HttpResourceView TData="List<Product>" 
                  Url="/api/products"
                  Method="HttpMethod.Get"
                  Headers="@(new Dictionary<string, string> { ["X-API-Key"] = "secret" })"
                  OnDataLoaded="HandleProductsLoaded"
                  RefreshInterval="TimeSpan.FromSeconds(30)">
    <Success Context="products">
        @foreach (var product in products)
        {
            <ProductCard Product="product" />
        }
    </Success>
</HttpResourceView>

@code {
    private void HandleProductsLoaded(List<Product> products)
    {
        Console.WriteLine($"Loaded {products.Count} products");
    }
}
```

### 3. ResourceView Component

```razor
<!-- For typed HTTP resources -->
@inject UserApiClient UserApi

<ResourceView TData="User" 
              Resource="@(() => UserApi.GetUserAsync(UserId))"
              @ref="userView">
    <NotStarted>
        <p>Click to load user data</p>
        <button @onclick="() => userView.LoadAsync()">Load User</button>
    </NotStarted>
    <Loading>
        <div class="placeholder-glow">
            <span class="placeholder col-12"></span>
        </div>
    </Loading>
    <Success Context="user">
        <UserProfile User="user" />
    </Success>
    <Error Context="error">
        <ErrorAlert Error="error" />
    </Error>
</ResourceView>

@code {
    [Parameter] public int UserId { get; set; }
    private ResourceView<User>? userView;
}
```

### 4. ResourceSignalView Component

```razor
<!-- For ResourceSignal<T> -->
<ResourceSignalView TData="DashboardData" ResourceSignal="dashboardSignal">
    <Loading>
        <DashboardSkeleton />
    </Loading>
    <Success Context="data">
        <Dashboard Data="data" />
    </Success>
    <Error Context="error">
        <div class="alert alert-danger">
            Failed to load dashboard: @error.Message
            <button class="btn btn-sm btn-outline-danger" 
                    @onclick="() => dashboardSignal.RefreshAsync()">
                Retry
            </button>
        </div>
    </Error>
</ResourceSignalView>

@code {
    private ResourceSignal<DashboardData> dashboardSignal = new(
        fetcher: async (ct) => await LoadDashboardAsync(ct)
    );
    
    protected override async Task OnInitializedAsync()
    {
        await dashboardSignal.LoadAsync();
    }
    
    private async Task<DashboardData> LoadDashboardAsync(CancellationToken ct)
    {
        // Load dashboard data
        return await Http.GetFromJsonAsync<DashboardData>("/api/dashboard", ct);
    }
}
```

### 5. ResourceSignalRView Component

```razor
<!-- For SignalR resources -->
<ResourceSignalRView TData="LiveMetrics"
                     HubUrl="https://api.example.com/hubs/metrics"
                     MethodName="ReceiveMetrics"
                     Fetcher="LoadInitialMetrics"
                     ConfigureConnection="ConfigureHub">
    <Connecting>
        <div class="text-muted">
            <span class="spinner-grow spinner-grow-sm"></span>
            Connecting to live data...
        </div>
    </Connecting>
    <Connected Context="metrics">
        <MetricsDisplay Metrics="metrics" />
    </Connected>
    <Reconnecting>
        <div class="text-warning">
            Connection lost. Reconnecting...
        </div>
    </Reconnecting>
    <Disconnected>
        <div class="text-danger">
            Disconnected from server
        </div>
    </Disconnected>
    <Error Context="error">
        <div class="alert alert-danger">
            Connection error: @error.Message
        </div>
    </Error>
</ResourceSignalRView>

@code {
    private async Task<LiveMetrics> LoadInitialMetrics(CancellationToken ct)
    {
        return await Http.GetFromJsonAsync<LiveMetrics>("/api/metrics/latest", ct);
    }
    
    private void ConfigureHub(IHubConnectionBuilder builder)
    {
        builder
            .WithAutomaticReconnect()
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }
}
```

### 6. StockSignalRView Component

```razor
<!-- Specialized component for filtered SignalR data -->
<StockSignalRView TData="StockPrice"
                  HubUrl="@hubUrl"
                  MethodName="ReceivePriceUpdate"
                  Filter="@(price => watchedSymbols.Contains(price.Symbol))"
                  ShowConnectionStatus="true"
                  ConnectionStatusClass="connection-indicator">
    <Connected Context="price">
        <div class="stock-ticker @(price.Change >= 0 ? "up" : "down")">
            <span class="symbol">@price.Symbol</span>
            <span class="price">${price.Price:F2}</span>
            <span class="change">
                @(price.Change >= 0 ? "▲" : "▼") 
                @Math.Abs(price.Change)%
            </span>
        </div>
    </Connected>
</StockSignalRView>

@code {
    private string hubUrl = "https://api.example.com/hubs/stocks";
    private List<string> watchedSymbols = new() { "AAPL", "GOOGL", "MSFT" };
}
```

## Form Binding Patterns

### 1. Two-Way Signal Binding

```razor
@using FluentSignals.Blazor.Extensions

<EditForm Model="@formModel" OnValidSubmit="HandleSubmit">
    <div class="mb-3">
        <label>Username</label>
        <InputText @bind-Value="username.Value" class="form-control" />
        <small class="text-muted">
            Characters: @username.Value.Length
        </small>
    </div>
    
    <div class="mb-3">
        <label>Email</label>
        <InputText @bind-Value="email.Value" class="form-control" />
        @if (!isEmailValid.Value && email.Value.Length > 0)
        {
            <div class="invalid-feedback d-block">
                Invalid email format
            </div>
        }
    </div>
    
    <button type="submit" 
            class="btn btn-primary" 
            disabled="@(!isFormValid.Value)">
        Submit
    </button>
</EditForm>

<div class="mt-3">
    <h5>Form Summary</h5>
    <p>@formSummary.Value</p>
</div>

@code {
    private Signal<string> username = new("");
    private Signal<string> email = new("");
    
    private ComputedSignal<bool> isEmailValid;
    private ComputedSignal<bool> isFormValid;
    private ComputedSignal<string> formSummary;
    
    private FormModel formModel = new();
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        // Computed validations
        isEmailValid = new ComputedSignal<bool>(
            () => string.IsNullOrEmpty(email.Value) || 
                  email.Value.Contains("@") && email.Value.Contains("."),
            [email]
        );
        
        isFormValid = new ComputedSignal<bool>(
            () => !string.IsNullOrWhiteSpace(username.Value) && 
                  isEmailValid.Value && 
                  !string.IsNullOrWhiteSpace(email.Value),
            [username, email, isEmailValid]
        );
        
        formSummary = new ComputedSignal<string>(
            () => $"User: {username.Value}, Email: {email.Value}",
            [username, email]
        );
        
        // Subscribe to changes
        Subscribe(username);
        Subscribe(email);
        Subscribe(isEmailValid);
        Subscribe(isFormValid);
        Subscribe(formSummary);
        
        // Sync with model
        username.Subscribe(v => formModel.Username = v);
        email.Subscribe(v => formModel.Email = v);
    }
    
    private class FormModel
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
```

### 2. Complex Form State

```csharp
public class FormStateManager<TModel> : IDisposable where TModel : class, new()
{
    private readonly Signal<TModel> _model = new(new TModel());
    private readonly Signal<bool> _isDirty = new(false);
    private readonly Signal<bool> _isSubmitting = new(false);
    private readonly Signal<List<string>> _errors = new(new());
    private readonly TModel _originalModel;
    
    public ISignal<TModel> Model => _model;
    public ISignal<bool> IsDirty => _isDirty;
    public ISignal<bool> IsSubmitting => _isSubmitting;
    public ISignal<List<string>> Errors => _errors;
    
    public ComputedSignal<bool> CanSubmit { get; }
    public ComputedSignal<bool> HasErrors { get; }
    
    public FormStateManager(TModel? initialModel = null)
    {
        _originalModel = initialModel ?? new TModel();
        _model.Value = DeepClone(_originalModel);
        
        CanSubmit = new ComputedSignal<bool>(
            () => _isDirty.Value && !_isSubmitting.Value && !HasErrors.Value,
            [_isDirty, _isSubmitting, _errors]
        );
        
        HasErrors = new ComputedSignal<bool>(
            () => _errors.Value.Any(),
            [_errors]
        );
        
        // Track changes
        _model.Subscribe(_ => _isDirty.Value = !AreEqual(_model.Value, _originalModel));
    }
    
    public void UpdateField<TValue>(Expression<Func<TModel, TValue>> field, TValue value)
    {
        var model = DeepClone(_model.Value);
        var property = GetProperty(field);
        property.SetValue(model, value);
        _model.Value = model;
    }
    
    public async Task<bool> SubmitAsync(Func<TModel, Task<bool>> submitAction)
    {
        _isSubmitting.Value = true;
        _errors.Value = new List<string>();
        
        try
        {
            var success = await submitAction(_model.Value);
            if (success)
            {
                _isDirty.Value = false;
            }
            return success;
        }
        catch (Exception ex)
        {
            _errors.Value = new List<string> { ex.Message };
            return false;
        }
        finally
        {
            _isSubmitting.Value = false;
        }
    }
    
    public void Reset()
    {
        _model.Value = DeepClone(_originalModel);
        _isDirty.Value = false;
        _errors.Value = new List<string>();
    }
    
    public void Dispose()
    {
        _model.Dispose();
        _isDirty.Dispose();
        _isSubmitting.Dispose();
        _errors.Dispose();
        CanSubmit.Dispose();
        HasErrors.Dispose();
    }
}
```

## Service Extensions

### 1. Registration Extensions

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Basic registration
builder.Services.AddFluentSignalsBlazor();

// With SignalBus
builder.Services.AddFluentSignalsBlazorWithSignalBus(options =>
{
    options.EnableLogging = true;
    options.DefaultPriority = SignalPriority.Normal;
});

// With HTTP configuration
builder.Services.AddFluentSignalsBlazor(blazorOptions =>
{
    blazorOptions.DefaultRefreshInterval = TimeSpan.FromMinutes(1);
    blazorOptions.EnableAutoRefresh = true;
}, 
httpOptions =>
{
    httpOptions.BaseUrl = "https://api.example.com";
    httpOptions.Timeout = TimeSpan.FromSeconds(30);
});

// Register HTTP resource factory
builder.Services.AddHttpResourceFactory();

// Register typed resources
builder.Services.AddTypedHttpResource<UserApiClient>();
builder.Services.AddTypedHttpResource<ProductApiClient>("https://products.api.com");
```

### 2. Signal Extensions for Blazor

```csharp
public static class SignalBlazorExtensions
{
    // Subscribe with automatic StateHasChanged
    public static IDisposable SubscribeStateChange<T>(
        this ISignal<T> signal, 
        ComponentBase component)
    {
        return signal.Subscribe(_ => component.InvokeAsync(component.StateHasChanged));
    }
    
    // Subscribe with value handler and StateHasChanged
    public static IDisposable SubscribeWithState<T>(
        this ISignal<T> signal,
        ComponentBase component,
        Action<T> handler)
    {
        return signal.Subscribe(value =>
        {
            handler(value);
            component.InvokeAsync(component.StateHasChanged);
        });
    }
    
    // Bind to component lifecycle
    public static void BindToComponent<T>(
        this ISignal<T> signal,
        ComponentBase component,
        Action<T> handler)
    {
        var subscription = signal.SubscribeWithState(component, handler);
        
        // Auto-dispose when component is disposed
        if (component is IDisposable disposable)
        {
            var originalDispose = disposable.Dispose;
            disposable.Dispose = () =>
            {
                subscription.Dispose();
                originalDispose();
            };
        }
    }
}

// Usage
@code {
    private Signal<string> message = new("Hello");
    
    protected override void OnInitialized()
    {
        // Automatic UI updates
        message.SubscribeStateChange(this);
        
        // With handler
        message.SubscribeWithState(this, msg => 
        {
            Console.WriteLine($"Message changed: {msg}");
        });
    }
}
```

## Component Patterns

### 1. Reactive List Component

```razor
@typeparam TItem
@inherits SignalComponentBase

<div class="reactive-list">
    @if (isLoading.Value)
    {
        <div class="loading">Loading...</div>
    }
    else if (filteredItems.Value.Any())
    {
        <div class="search-box mb-3">
            <input type="text" 
                   class="form-control" 
                   placeholder="Search..."
                   @bind="searchTerm.Value" 
                   @bind:event="oninput" />
        </div>
        
        <div class="list-group">
            @foreach (var item in pagedItems.Value)
            {
                <div class="list-group-item">
                    @ItemTemplate(item)
                </div>
            }
        </div>
        
        @if (totalPages.Value > 1)
        {
            <Pagination CurrentPage="currentPage" 
                       TotalPages="totalPages.Value" 
                       OnPageChanged="page => currentPage.Value = page" />
        }
    }
    else
    {
        <div class="empty">@EmptyMessage</div>
    }
</div>

@code {
    [Parameter, EditorRequired] public ISignal<List<TItem>> Items { get; set; } = null!;
    [Parameter, EditorRequired] public RenderFragment<TItem> ItemTemplate { get; set; } = null!;
    [Parameter] public Func<TItem, string, bool>? SearchPredicate { get; set; }
    [Parameter] public int PageSize { get; set; } = 10;
    [Parameter] public string EmptyMessage { get; set; } = "No items found";
    
    private Signal<string> searchTerm = new("");
    private Signal<int> currentPage = new(1);
    private Signal<bool> isLoading = new(false);
    
    private ComputedSignal<List<TItem>> filteredItems = null!;
    private ComputedSignal<List<TItem>> pagedItems = null!;
    private ComputedSignal<int> totalPages = null!;
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        // Filtered items based on search
        filteredItems = new ComputedSignal<List<TItem>>(() =>
        {
            if (string.IsNullOrWhiteSpace(searchTerm.Value) || SearchPredicate == null)
                return Items.Value;
                
            return Items.Value
                .Where(item => SearchPredicate(item, searchTerm.Value))
                .ToList();
        }, [Items, searchTerm]);
        
        // Paged items
        pagedItems = new ComputedSignal<List<TItem>>(() =>
        {
            var skip = (currentPage.Value - 1) * PageSize;
            return filteredItems.Value.Skip(skip).Take(PageSize).ToList();
        }, [filteredItems, currentPage]);
        
        // Total pages
        totalPages = new ComputedSignal<int>(() =>
        {
            return (int)Math.Ceiling(filteredItems.Value.Count / (double)PageSize);
        }, [filteredItems]);
        
        // Reset page when search changes
        searchTerm.Subscribe(_ => currentPage.Value = 1);
        
        // Subscribe to all signals
        Subscribe(Items);
        Subscribe(searchTerm);
        Subscribe(currentPage);
        Subscribe(isLoading);
        Subscribe(filteredItems);
        Subscribe(pagedItems);
        Subscribe(totalPages);
    }
}
```

### 2. Auto-Save Form Component

```razor
@inherits SignalComponentBase
@inject ISignalPublisher Publisher

<EditForm Model="@model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    
    <div class="d-flex justify-content-between mb-3">
        <h3>Edit Profile</h3>
        <div>
            @if (autoSaveStatus.Value == AutoSaveStatus.Saving)
            {
                <span class="text-muted">
                    <span class="spinner-border spinner-border-sm"></span>
                    Saving...
                </span>
            }
            else if (autoSaveStatus.Value == AutoSaveStatus.Saved)
            {
                <span class="text-success">
                    <i class="bi bi-check-circle"></i>
                    Saved
                </span>
            }
        </div>
    </div>
    
    <div class="mb-3">
        <label>Name</label>
        <InputText class="form-control" 
                   @bind-Value="model.Name" 
                   @oninput="OnFieldChanged" />
        <ValidationMessage For="() => model.Name" />
    </div>
    
    <div class="mb-3">
        <label>Bio</label>
        <InputTextArea class="form-control" 
                       @bind-Value="model.Bio" 
                       @oninput="OnFieldChanged" 
                       rows="4" />
        <ValidationMessage For="() => model.Bio" />
    </div>
    
    <div class="form-check mb-3">
        <InputCheckbox class="form-check-input" 
                       @bind-Value="model.PublicProfile" 
                       @onchange="OnFieldChanged" />
        <label class="form-check-label">Public Profile</label>
    </div>
    
    <button type="submit" class="btn btn-primary">Save Changes</button>
</EditForm>

@code {
    private ProfileModel model = new();
    private Signal<AutoSaveStatus> autoSaveStatus = new(AutoSaveStatus.None);
    private Timer? autoSaveTimer;
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Subscribe(autoSaveStatus);
        LoadProfile();
    }
    
    private void OnFieldChanged()
    {
        // Cancel existing timer
        autoSaveTimer?.Dispose();
        
        // Mark as unsaved
        autoSaveStatus.Value = AutoSaveStatus.Unsaved;
        
        // Start new timer for auto-save
        autoSaveTimer = new Timer(async _ =>
        {
            await AutoSave();
        }, null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
    }
    
    private async Task AutoSave()
    {
        autoSaveStatus.Value = AutoSaveStatus.Saving;
        
        try
        {
            // Save to backend
            await Task.Delay(500); // Simulate API call
            
            autoSaveStatus.Value = AutoSaveStatus.Saved;
            
            // Publish saved event
            Publisher.Publish(new ProfileUpdatedEvent 
            { 
                Profile = model,
                IsAutoSave = true 
            });
            
            // Reset status after 2 seconds
            _ = Task.Delay(2000).ContinueWith(_ => 
            {
                if (autoSaveStatus.Value == AutoSaveStatus.Saved)
                    autoSaveStatus.Value = AutoSaveStatus.None;
            });
        }
        catch
        {
            autoSaveStatus.Value = AutoSaveStatus.Error;
        }
    }
    
    private async Task HandleSubmit()
    {
        // Manual save
        await AutoSave();
    }
    
    private void LoadProfile()
    {
        // Load profile data
        model = new ProfileModel
        {
            Name = "John Doe",
            Bio = "Software Developer",
            PublicProfile = true
        };
    }
    
    public override void Dispose()
    {
        autoSaveTimer?.Dispose();
        base.Dispose();
    }
    
    private class ProfileModel
    {
        [Required]
        public string Name { get; set; } = "";
        
        [MaxLength(500)]
        public string Bio { get; set; } = "";
        
        public bool PublicProfile { get; set; }
    }
    
    private enum AutoSaveStatus
    {
        None,
        Unsaved,
        Saving,
        Saved,
        Error
    }
    
    public class ProfileUpdatedEvent
    {
        public ProfileModel Profile { get; set; } = null!;
        public bool IsAutoSave { get; set; }
    }
}
```

## Testing Blazor Components

### 1. Testing Signal Components

```csharp
[Fact]
public void SignalComponent_Should_Update_On_Signal_Change()
{
    // Arrange
    using var ctx = new TestContext();
    var signal = new Signal<string>("Initial");
    
    // Act
    var component = ctx.RenderComponent<TestSignalComponent>(parameters => 
        parameters.Add(p => p.TextSignal, signal));
    
    // Assert initial render
    Assert.Contains("Initial", component.Markup);
    
    // Change signal value
    signal.Value = "Updated";
    
    // Assert component updated
    Assert.Contains("Updated", component.Markup);
}

@* Test component *@
@inherits SignalComponentBase

<p>@TextSignal.Value</p>

@code {
    [Parameter] public ISignal<string> TextSignal { get; set; } = null!;
    
    protected override void OnInitialized()
    {
        Subscribe(TextSignal);
    }
}
```

### 2. Testing HTTP Resource Components

```csharp
[Fact]
public async Task HttpResourceView_Should_Show_Loading_Then_Data()
{
    // Arrange
    using var ctx = new TestContext();
    var mockHttp = ctx.Services.AddMockHttpClient();
    
    mockHttp
        .When("/api/data")
        .Respond("application/json", JsonSerializer.Serialize(new { Name = "Test" }));
    
    // Act
    var component = ctx.RenderComponent<HttpResourceView<TestData>>(parameters =>
        parameters
            .Add(p => p.Url, "/api/data")
            .Add(p => p.Loading, "<p>Loading...</p>")
            .Add(p => p.Success, (TestData data) => $"<p>{data.Name}</p>"));
    
    // Assert loading state
    Assert.Contains("Loading...", component.Markup);
    
    // Wait for data
    await component.WaitForState(() => component.Markup.Contains("Test"), TimeSpan.FromSeconds(1));
    
    // Assert success state
    Assert.Contains("Test", component.Markup);
}
```

## Best Practices

### 1. Always Use SignalComponentBase

```csharp
// Bad - Manual subscription management
@implements IDisposable
private IDisposable? subscription;

protected override void OnInitialized()
{
    subscription = signal.Subscribe(_ => InvokeAsync(StateHasChanged));
}

public void Dispose() => subscription?.Dispose();

// Good - Automatic management
@inherits SignalComponentBase

protected override void OnInitialized()
{
    Subscribe(signal); // Handles StateHasChanged and disposal
}
```

### 2. Use Computed Signals for Derived UI State

```csharp
// Bad - Manual calculation on each render
<p>Total: @(items.Value.Sum(i => i.Price))</p>

// Good - Computed signal
@code {
    private ComputedSignal<decimal> total;
    
    protected override void OnInitialized()
    {
        total = new ComputedSignal<decimal>(
            () => items.Value.Sum(i => i.Price),
            [items]
        );
        Subscribe(total);
    }
}
<p>Total: @total.Value</p>
```

### 3. Handle All Resource States

```razor
<!-- Bad - Only showing success -->
<HttpResourceView TData="User" Url="/api/user">
    <Success Context="user">
        <p>@user.Name</p>
    </Success>
</HttpResourceView>

<!-- Good - All states handled -->
<HttpResourceView TData="User" Url="/api/user">
    <Loading>
        <UserSkeleton />
    </Loading>
    <Success Context="user">
        <UserCard User="user" />
    </Success>
    <Error Context="error">
        <ErrorAlert Message="@error.Message" />
    </Error>
</HttpResourceView>
```

### 4. Use RefreshInterval Wisely

```razor
<!-- Bad - Too frequent -->
<HttpResourceView TData="StockPrice" 
                  Url="/api/stock" 
                  RefreshInterval="TimeSpan.FromSeconds(1)">

<!-- Good - Reasonable interval -->
<HttpResourceView TData="StockPrice" 
                  Url="/api/stock" 
                  RefreshInterval="TimeSpan.FromSeconds(10)">
```

### 5. Dispose Resources in Components

```csharp
// Always implement IAsyncDisposable for SignalR components
@implements IAsyncDisposable

private ResourceSignalR<Data>? resource;

protected override async Task OnInitializedAsync()
{
    resource = new ResourceSignalR<Data>(hubUrl, "ReceiveData");
    await resource.StartAsync();
}

public async ValueTask DisposeAsync()
{
    if (resource != null)
        await resource.DisposeAsync();
}
```

## Key Takeaways for AI Assistants

1. **SignalComponentBase simplifies signal usage** - Automatic subscriptions and disposal
2. **Pre-built components handle common patterns** - HttpResourceView, ResourceSignalRView, etc.
3. **Form binding works seamlessly** - Two-way binding with signals
4. **Always handle all resource states** - Loading, Success, Error, etc.
5. **Use computed signals for UI logic** - Automatic updates when dependencies change
6. **Leverage SignalBus for component communication** - Decoupled event handling
7. **Test with bUnit** - Full component testing support
8. **Dispose resources properly** - Especially important for SignalR