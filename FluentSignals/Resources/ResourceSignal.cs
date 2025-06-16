using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;

namespace FluentSignals.Resources;

/// <summary>
/// Represents the state of a resource that can be loading, loaded with data, or in an error state
/// </summary>
/// <typeparam name="T">The type of data the resource holds</typeparam>
public class ResourceState<T>
{
    public bool IsLoading { get; set; }
    public bool HasData => Data != null;
    public bool HasError => Error != null;
    public T? Data { get; set; }
    public Exception? Error { get; set; }
    public DateTime? LastUpdated { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static ResourceState<T> Loading() => new() { IsLoading = true };
    public static ResourceState<T> Success(T data) => new() 
    { 
        Data = data, 
        IsLoading = false, 
        LastUpdated = DateTime.UtcNow 
    };
    public static ResourceState<T> Failure(Exception error) => new() 
    { 
        Error = error, 
        IsLoading = false, 
        LastUpdated = DateTime.UtcNow 
    };
}

/// <summary>
/// A signal that represents a resource with loading, data, and error states
/// </summary>
/// <typeparam name="T">The type of data the resource holds</typeparam>
public class ResourceSignal<T> : IDisposable
{
    private readonly TypedSignal<ResourceState<T>> _stateSignal;
    private readonly Func<CancellationToken, Task<T>>? _fetcher;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Creates a new ResourceSignal with an optional fetcher function
    /// </summary>
    /// <param name="fetcher">Optional function to fetch the resource data</param>
    /// <param name="initialData">Optional initial data</param>
    public ResourceSignal(Func<CancellationToken, Task<T>>? fetcher = null, T? initialData = default)
    {
        _fetcher = fetcher;
        var initialState = initialData != null 
            ? ResourceState<T>.Success(initialData) 
            : new ResourceState<T>();
        _stateSignal = new TypedSignal<ResourceState<T>>(initialState);
    }

    /// <summary>
    /// Gets the current state of the resource
    /// </summary>
    public ResourceState<T> State => _stateSignal.Value;

    /// <summary>
    /// Gets whether the resource is currently loading
    /// </summary>
    public bool IsLoading => State.IsLoading;

    /// <summary>
    /// Gets whether the resource has data
    /// </summary>
    public bool HasData => State.HasData;

    /// <summary>
    /// Gets whether the resource has an error
    /// </summary>
    public bool HasError => State.HasError;

    /// <summary>
    /// Gets the resource data if available
    /// </summary>
    public T? Data => State.Data;

    /// <summary>
    /// Gets the error if any
    /// </summary>
    public Exception? Error => State.Error;

    /// <summary>
    /// Loads or reloads the resource
    /// </summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_fetcher == null)
        {
            throw new InvalidOperationException("No fetcher function provided for this resource");
        }

        // Cancel any existing load operation
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Set loading state
            _stateSignal.Value = ResourceState<T>.Loading();

            // Fetch the data
            var data = await _fetcher(_cancellationTokenSource.Token);

            // Set success state if not cancelled
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _stateSignal.Value = ResourceState<T>.Success(data);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            // Set error state if not cancelled
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _stateSignal.Value = ResourceState<T>.Failure(ex);
            }
        }
    }

    /// <summary>
    /// Manually sets the resource to loading state
    /// </summary>
    public void SetLoading()
    {
        _stateSignal.Value = ResourceState<T>.Loading();
    }

    /// <summary>
    /// Manually sets the resource data
    /// </summary>
    public void SetData(T data)
    {
        _stateSignal.Value = ResourceState<T>.Success(data);
    }

    /// <summary>
    /// Manually sets an error on the resource
    /// </summary>
    public void SetError(Exception error)
    {
        _stateSignal.Value = ResourceState<T>.Failure(error);
    }

    /// <summary>
    /// Updates the resource metadata
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        var newState = new ResourceState<T>
        {
            IsLoading = State.IsLoading,
            Data = State.Data,
            Error = State.Error,
            LastUpdated = State.LastUpdated,
            Metadata = new Dictionary<string, object>(State.Metadata) { [key] = value }
        };
        _stateSignal.Value = newState;
    }

    /// <summary>
    /// Subscribes to state changes
    /// </summary>
    public IDisposable Subscribe(Action<ResourceState<T>> handler)
    {
        var subscription = _stateSignal.Subscribe(handler);
        return new SubscriptionDisposable(subscription, _stateSignal);
    }

    /// <summary>
    /// Refreshes the resource (alias for LoadAsync)
    /// </summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return LoadAsync(cancellationToken);
    }

    /// <summary>
    /// Clears the resource data and error
    /// </summary>
    public void Clear()
    {
        _stateSignal.Value = new ResourceState<T>();
    }

    /// <summary>
    /// Disposes the resource and cancels any pending operations
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _stateSignal.Dispose();
    }
}

/// <summary>
/// Wrapper to make ISignalSubscriptionContract work as IDisposable
/// </summary>
internal class SubscriptionDisposable : IDisposable
{
    private readonly ISignalSubscriptionContract _subscription;
    private readonly ISignal _signal;

    public SubscriptionDisposable(ISignalSubscriptionContract subscription, ISignal signal)
    {
        _subscription = subscription;
        _signal = signal;
    }

    public void Dispose()
    {
        _signal.Unsubscribe(_subscription.SubscriptionId);
    }
}