# Async Signals Guide

This guide provides comprehensive documentation on using FluentSignals' async signal types for managing asynchronous operations with reactive state management.

## Table of Contents
- [Overview](#overview)
- [AsyncSignal](#asyncsignal)
- [AsyncTypedSignal](#asynctypedsignal)
- [Loading and Error States](#loading-and-error-states)
- [Practical Examples](#practical-examples)
- [Best Practices](#best-practices)
- [Common Patterns](#common-patterns)

## Overview

FluentSignals provides two async signal types that help manage asynchronous operations with automatic state tracking:

1. **AsyncSignal** - For async operations without a return value
2. **AsyncTypedSignal<T>** - For async operations that produce a typed result

Both types automatically track loading states, errors, and provide reactive updates throughout the async operation lifecycle.

## AsyncSignal

`AsyncSignal` is designed for async operations that don't return a value but need state tracking.

### Basic Usage

```csharp
// Create an async signal
var saveSignal = new AsyncSignal();

// Subscribe to state changes
saveSignal.IsLoading.Subscribe(isLoading => 
    Console.WriteLine($"Saving: {isLoading}"));

saveSignal.Error.Subscribe(error => 
{
    if (error != null)
        Console.WriteLine($"Save failed: {error.Message}");
});

// Run an async operation
await saveSignal.RunAsync(async () =>
{
    await databaseService.SaveDataAsync();
    Console.WriteLine("Data saved successfully!");
});
```

### Error Handling

```csharp
var processSignal = new AsyncSignal();

// Errors are automatically captured
await processSignal.RunAsync(async () =>
{
    await Task.Delay(100);
    throw new InvalidOperationException("Processing failed");
});

// Check error state
if (processSignal.Error.Value != null)
{
    Console.WriteLine($"Error: {processSignal.Error.Value.Message}");
}

// Error is cleared on successful run
await processSignal.RunAsync(async () =>
{
    await Task.Delay(100);
    // Success - error is now null
});
```

### Multiple Operations

```csharp
var syncSignal = new AsyncSignal();

// Run sequential operations
await syncSignal.RunAsync(async () =>
{
    await DownloadDataAsync();
});

await syncSignal.RunAsync(async () =>
{
    await ProcessDataAsync();
});

await syncSignal.RunAsync(async () =>
{
    await UploadResultsAsync();
});
```

## AsyncTypedSignal

`AsyncTypedSignal<T>` extends `TypedSignal<T>` with async loading capabilities and state tracking.

### Basic Usage

```csharp
// Create an async typed signal with initial value
var userSignal = new AsyncTypedSignal<User?>(null);

// Subscribe to value changes
userSignal.Subscribe(user =>
{
    if (user != null)
        Console.WriteLine($"User loaded: {user.Name}");
});

// Load data asynchronously
await userSignal.LoadAsync(async () =>
{
    return await userService.GetUserAsync(userId);
});

// Access the loaded value
var currentUser = userSignal.Value;
```

### Complex Data Loading

```csharp
public class DashboardViewModel
{
    private readonly AsyncTypedSignal<DashboardData> _dashboardData;
    
    public DashboardViewModel()
    {
        _dashboardData = new AsyncTypedSignal<DashboardData>(new DashboardData());
        
        // Subscribe to all state changes
        _dashboardData.Subscribe(data => UpdateUI(data));
        _dashboardData.IsLoading.Subscribe(loading => ShowLoadingSpinner(loading));
        _dashboardData.Error.Subscribe(error => ShowError(error));
    }
    
    public async Task RefreshDashboard()
    {
        await _dashboardData.LoadAsync(async () =>
        {
            var tasks = new[]
            {
                GetSalesDataAsync(),
                GetUserStatsAsync(),
                GetRecentActivityAsync()
            };
            
            var results = await Task.WhenAll(tasks);
            
            return new DashboardData
            {
                Sales = results[0],
                UserStats = results[1],
                RecentActivity = results[2]
            };
        });
    }
}
```

## Loading and Error States

Both async signal types implement the `IAsyncSignal` interface, providing consistent state management:

```csharp
public interface IAsyncSignal : ISignal
{
    ISignal<bool> IsLoading { get; }
    ISignal<Exception?> Error { get; }
}
```

### Tracking Loading States

```csharp
var dataSignal = new AsyncTypedSignal<List<Product>>(new List<Product>());

// Create loading indicator
dataSignal.IsLoading.Subscribe(isLoading =>
{
    loadingSpinner.Visible = isLoading;
    contentPanel.Enabled = !isLoading;
});

// Load data - loading states update automatically
await dataSignal.LoadAsync(async () =>
{
    return await productService.GetProductsAsync();
});
```

### Error Recovery Patterns

```csharp
var apiSignal = new AsyncTypedSignal<ApiResponse?>(null);

async Task LoadWithRetry(int maxAttempts = 3)
{
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        await apiSignal.LoadAsync(async () =>
        {
            return await apiClient.GetDataAsync();
        });
        
        if (apiSignal.Error.Value == null)
            break; // Success
            
        if (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
}
```

## Practical Examples

### 1. Search with Debouncing

```csharp
public class SearchViewModel
{
    private readonly AsyncTypedSignal<SearchResults> _searchResults;
    private CancellationTokenSource? _searchCts;
    
    public SearchViewModel()
    {
        _searchResults = new AsyncTypedSignal<SearchResults>(new SearchResults());
    }
    
    public async Task SearchAsync(string query)
    {
        // Cancel previous search
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        
        // Debounce
        await Task.Delay(300, token);
        
        await _searchResults.LoadAsync(async () =>
        {
            token.ThrowIfCancellationRequested();
            return await searchService.SearchAsync(query, token);
        });
    }
}
```

### 2. Form Submission with Validation

```csharp
public class FormViewModel
{
    private readonly AsyncSignal _submitSignal;
    private readonly TypedSignal<string?> _validationError;
    
    public FormViewModel()
    {
        _submitSignal = new AsyncSignal();
        _validationError = new TypedSignal<string?>(null);
        
        // Disable form during submission
        _submitSignal.IsLoading.Subscribe(isSubmitting =>
        {
            submitButton.Enabled = !isSubmitting;
            formFields.ReadOnly = isSubmitting;
        });
        
        // Show errors
        _submitSignal.Error.Subscribe(error =>
        {
            if (error != null)
                _validationError.Value = error.Message;
        });
    }
    
    public async Task SubmitForm(FormData data)
    {
        _validationError.Value = null;
        
        await _submitSignal.RunAsync(async () =>
        {
            // Validate
            var validationResult = validator.Validate(data);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors.First());
            }
            
            // Submit
            await formService.SubmitAsync(data);
            
            // Navigate on success
            navigationService.NavigateTo("/success");
        });
    }
}
```

### 3. Paginated Data Loading

```csharp
public class PaginatedListViewModel<T>
{
    private readonly AsyncTypedSignal<PagedResult<T>> _pagedData;
    private int _currentPage = 1;
    private readonly int _pageSize = 20;
    
    public PaginatedListViewModel()
    {
        _pagedData = new AsyncTypedSignal<PagedResult<T>>(
            new PagedResult<T> { Items = new List<T>(), TotalCount = 0 }
        );
    }
    
    public async Task LoadPage(int page)
    {
        _currentPage = page;
        
        await _pagedData.LoadAsync(async () =>
        {
            return await dataService.GetPagedAsync<T>(_currentPage, _pageSize);
        });
    }
    
    public async Task NextPage()
    {
        if (_currentPage < TotalPages)
        {
            await LoadPage(_currentPage + 1);
        }
    }
    
    public int TotalPages => 
        (int)Math.Ceiling(_pagedData.Value.TotalCount / (double)_pageSize);
}
```

### 4. Real-time Data Updates

```csharp
public class LiveDataViewModel : IDisposable
{
    private readonly AsyncTypedSignal<LiveData> _liveData;
    private readonly Timer _refreshTimer;
    
    public LiveDataViewModel()
    {
        _liveData = new AsyncTypedSignal<LiveData>(new LiveData());
        
        // Initial load
        _ = RefreshData();
        
        // Auto-refresh every 5 seconds
        _refreshTimer = new Timer(async _ =>
        {
            await RefreshData();
        }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    private async Task RefreshData()
    {
        await _liveData.LoadAsync(async () =>
        {
            return await liveDataService.GetLatestAsync();
        });
    }
    
    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _liveData?.Dispose();
    }
}
```

## Best Practices

### 1. Always Dispose Signals

```csharp
public class MyComponent : IDisposable
{
    private readonly AsyncTypedSignal<Data> _dataSignal;
    private readonly List<IDisposable> _subscriptions = new();
    
    public MyComponent()
    {
        _dataSignal = new AsyncTypedSignal<Data>(new Data());
        
        // Track subscriptions
        _subscriptions.Add(_dataSignal.Subscribe(OnDataChanged));
        _subscriptions.Add(_dataSignal.IsLoading.Subscribe(OnLoadingChanged));
    }
    
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }
        _dataSignal?.Dispose();
    }
}
```

### 2. Handle Concurrent Operations

```csharp
public class ConcurrentOperationHandler
{
    private readonly AsyncTypedSignal<Result> _resultSignal;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public async Task ProcessAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await _resultSignal.LoadAsync(async () =>
            {
                return await LongRunningOperationAsync();
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 3. Combine with Other Signals

```csharp
public class CompositeViewModel
{
    private readonly AsyncTypedSignal<User?> _userSignal;
    private readonly AsyncTypedSignal<List<Permission>> _permissionsSignal;
    private readonly TypedSignal<bool> _canEdit;
    
    public CompositeViewModel()
    {
        _userSignal = new AsyncTypedSignal<User?>(null);
        _permissionsSignal = new AsyncTypedSignal<List<Permission>>(new List<Permission>());
        _canEdit = new TypedSignal<bool>(false);
        
        // Update canEdit when user or permissions change
        _userSignal.Subscribe(_ => UpdateCanEdit());
        _permissionsSignal.Subscribe(_ => UpdateCanEdit());
    }
    
    private void UpdateCanEdit()
    {
        _canEdit.Value = _userSignal.Value != null && 
                        _permissionsSignal.Value.Any(p => p.Name == "Edit");
    }
}
```

### 4. Error Handling Strategies

```csharp
public class ResilientDataLoader
{
    private readonly AsyncTypedSignal<Data> _dataSignal;
    private readonly ILogger _logger;
    
    public async Task LoadDataAsync()
    {
        await _dataSignal.LoadAsync(async () =>
        {
            try
            {
                return await primaryDataSource.GetDataAsync();
            }
            catch (Exception primaryEx)
            {
                _logger.LogWarning(primaryEx, "Primary source failed, trying backup");
                
                try
                {
                    return await backupDataSource.GetDataAsync();
                }
                catch (Exception backupEx)
                {
                    _logger.LogError(backupEx, "Backup source also failed");
                    
                    // Return cached data if available
                    if (_dataSignal.Value != null)
                    {
                        _logger.LogInformation("Returning cached data");
                        return _dataSignal.Value;
                    }
                    
                    throw new DataLoadException("All data sources failed", backupEx);
                }
            }
        });
    }
}
```

## Common Patterns

### Transform Loading Results

```csharp
var rawDataSignal = new AsyncTypedSignal<RawData?>(null);
var processedSignal = new TypedSignal<ProcessedData?>(null);

rawDataSignal.Subscribe(raw =>
{
    if (raw != null)
    {
        processedSignal.Value = ProcessData(raw);
    }
});

await rawDataSignal.LoadAsync(async () => await GetRawDataAsync());
```

### Loading Multiple Related Resources

```csharp
public class RelatedDataLoader
{
    private readonly AsyncTypedSignal<User> _userSignal;
    private readonly AsyncTypedSignal<UserProfile> _profileSignal;
    private readonly AsyncTypedSignal<List<Order>> _ordersSignal;
    
    public async Task LoadUserDataAsync(int userId)
    {
        // Load user first
        await _userSignal.LoadAsync(async () => 
            await userService.GetUserAsync(userId));
        
        if (_userSignal.Value != null)
        {
            // Load related data in parallel
            var profileTask = _profileSignal.LoadAsync(async () => 
                await profileService.GetProfileAsync(_userSignal.Value.Id));
                
            var ordersTask = _ordersSignal.LoadAsync(async () => 
                await orderService.GetUserOrdersAsync(_userSignal.Value.Id));
            
            await Task.WhenAll(profileTask, ordersTask);
        }
    }
}
```

### State Machine with Async Signals

```csharp
public enum ProcessState
{
    Idle,
    Validating,
    Processing,
    Completed,
    Failed
}

public class ProcessStateMachine
{
    private readonly TypedSignal<ProcessState> _state;
    private readonly AsyncSignal _processSignal;
    
    public ProcessStateMachine()
    {
        _state = new TypedSignal<ProcessState>(ProcessState.Idle);
        _processSignal = new AsyncSignal();
        
        // Update state based on async signal
        _processSignal.IsLoading.Subscribe(isLoading =>
        {
            if (isLoading && _state.Value == ProcessState.Idle)
                _state.Value = ProcessState.Validating;
        });
        
        _processSignal.Error.Subscribe(error =>
        {
            if (error != null)
                _state.Value = ProcessState.Failed;
        });
    }
    
    public async Task RunProcess(ProcessData data)
    {
        _state.Value = ProcessState.Idle;
        
        await _processSignal.RunAsync(async () =>
        {
            // Validation phase
            await ValidateAsync(data);
            _state.Value = ProcessState.Processing;
            
            // Processing phase
            await ProcessAsync(data);
            _state.Value = ProcessState.Completed;
        });
    }
}
```

## Summary

Async signals in FluentSignals provide:

- **Automatic State Management**: Loading, error, and value states are tracked automatically
- **Type Safety**: Full IntelliSense support with `AsyncTypedSignal<T>`
- **Reactive Updates**: All state changes trigger notifications to subscribers
- **Error Handling**: Built-in error capture and state management
- **Composability**: Easily combine with other signals for complex scenarios

By using async signals, you can build robust applications that handle asynchronous operations gracefully while maintaining reactive, responsive user interfaces.