using System;
using System.Threading;
using System.Threading.Tasks;
using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;
using FluentSignals.Implementations.Subscriptions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace FluentSignals.SignalR.Core;

/// <summary>
/// Represents a SignalR resource that follows the same pattern as HttpResource
/// </summary>
public class SignalRResource<T> : IAsyncSignal<T>, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly string _methodName;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SignalRResource<T>>? _logger;
    private readonly Queue<Func<SignalRMessageHandler, SignalRMessageHandler>> _middleware = [];
    private IDisposable? _signalRSubscription;

    public ISignal<T> SignalValue { get; } = new TypedSignal<T>(default!);
    public ISignal<bool> IsConnected { get; } = new TypedSignal<bool>(false);
    public ISignal<Exception?> Error { get; } = new TypedSignal<Exception?>(null);
    public ISignal<HubConnectionState> ConnectionState { get; } = new TypedSignal<HubConnectionState>(HubConnectionState.Disconnected);
    public List<ISignalSubscriptionContract> Subscribers { get; } = [];

    public ISignal<bool> IsLoading => throw new NotImplementedException();

    public SignalRResource(
        HubConnection hubConnection,
        string methodName,
        IServiceProvider serviceProvider,
        ILogger<SignalRResource<T>>? logger = null)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _methodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;

        // Set up connection event handlers
        _hubConnection.Reconnecting += OnReconnecting;
        _hubConnection.Reconnected += OnReconnected;
        _hubConnection.Closed += OnClosed;
    }

    /// <summary>
    /// Adds middleware to the SignalR message processing pipeline
    /// </summary>
    public SignalRResource<T> Use(Func<SignalRMessageHandler, SignalRMessageHandler> middleware)
    {
        _middleware.Enqueue(middleware);
        return this;
    }

    /// <summary>
    /// Adds typed middleware to the SignalR message processing pipeline
    /// </summary>
    public SignalRResource<T> Use<TMiddleware>()
        where TMiddleware : ISignalRMiddleware
    {
        var middleware = _serviceProvider.GetService(typeof(TMiddleware)) as ISignalRMiddleware
            ?? throw new InvalidOperationException($"Middleware {typeof(TMiddleware).Name} not registered");
            
        return Use(next =>
            (message, context, cancellationToken) => middleware.InvokeAsync(message, context, next, cancellationToken)
        );
    }

    /// <summary>
    /// Connects to the SignalR hub
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Error.Value = null;
            ConnectionState.Value = HubConnectionState.Connecting;

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync(cancellationToken);
            }

            // Subscribe to the method
            var pipeline = BuildPipeline();
            
            _signalRSubscription = _hubConnection.On<T>(_methodName, async data =>
            {
                var context = new SignalRContext
                {
                    MethodName = _methodName,
                    ConnectionId = _hubConnection.ConnectionId,
                    State = _hubConnection.State
                };

                try
                {
                    await pipeline(data, context, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing SignalR message");
                    Error.Value = ex;
                }
            });

            ConnectionState.Value = HubConnectionState.Connected;
            IsConnected.Value = true;
            Notify();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to SignalR hub");
            Error.Value = ex;
            ConnectionState.Value = HubConnectionState.Disconnected;
            IsConnected.Value = false;
            throw;
        }
    }

    /// <summary>
    /// Disconnects from the SignalR hub
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _signalRSubscription?.Dispose();
            _signalRSubscription = null;

            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                await _hubConnection.StopAsync(cancellationToken);
            }

            ConnectionState.Value = HubConnectionState.Disconnected;
            IsConnected.Value = false;
            Notify();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error disconnecting from SignalR hub");
            Error.Value = ex;
            throw;
        }
    }

    /// <summary>
    /// Sends a message to the hub
    /// </summary>
    public async Task SendAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("Hub is not connected");
        }

        await _hubConnection.SendAsync(methodName, args, cancellationToken);
    }

    /// <summary>
    /// Invokes a method on the hub and returns the result
    /// </summary>
    public async Task<TResult> InvokeAsync<TResult>(string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("Hub is not connected");
        }

        return await _hubConnection.InvokeAsync<TResult>(methodName, args, cancellationToken);
    }

    public async Task LoadData(CancellationToken cancellationToken = default)
    {
        // For SignalR, LoadData means connecting if not connected
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await ConnectAsync(cancellationToken);
        }
    }

    private SignalRMessageHandler BuildPipeline()
    {
        SignalRMessageHandler final = async (data, context, ct) =>
        {
            SignalValue.Value = (T)Convert.ChangeType(data, typeof(T));
            Notify();
        };

        foreach (var component in _middleware.Reverse())
        {
            final = component(final);
        }

        return final;
    }

    #region Connection Event Handlers

    private Task OnReconnecting(Exception? exception)
    {
        _logger?.LogWarning(exception, "SignalR connection lost, attempting to reconnect");
        ConnectionState.Value = HubConnectionState.Reconnecting;
        IsConnected.Value = false;
        Notify();
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        _logger?.LogInformation("SignalR connection restored with ID: {ConnectionId}", connectionId);
        ConnectionState.Value = HubConnectionState.Connected;
        IsConnected.Value = true;
        Notify();
        return Task.CompletedTask;
    }

    private Task OnClosed(Exception? exception)
    {
        if (exception != null)
        {
            _logger?.LogError(exception, "SignalR connection closed with error");
            Error.Value = exception;
        }
        else
        {
            _logger?.LogInformation("SignalR connection closed");
        }

        ConnectionState.Value = HubConnectionState.Disconnected;
        IsConnected.Value = false;
        Notify();
        return Task.CompletedTask;
    }

    #endregion

    #region ISignal Implementation

    public ISignalSubscriptionContract Subscribe(ISignal subscriber)
    {
        var subscription = new SignalSubscription(Guid.NewGuid(), subscriber.Notify);
        Subscribers.Add(subscription);
        return subscription;
    }

    public ISignalSubscriptionContract Subscribe(Action action)
    {
        var subscription = new SignalSubscription(Guid.NewGuid(), action);
        Subscribers.Add(subscription);
        return subscription;
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        var subscriber = Subscribers.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
        if (subscriber is not null)
        {
            Subscribers.Remove(subscriber);
        }
    }

    public void Notify()
    {
        var subscribersCopy = Subscribers.OfType<SignalSubscription>().ToList();
        foreach (var sub in subscribersCopy)
        {
            sub.Action?.Invoke();
        }
    }

    public void Dispose()
    {
        Subscribers.Clear();
        SignalValue.Dispose();
        IsConnected.Dispose();
        Error.Dispose();
        ConnectionState.Dispose();
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        
        _hubConnection.Reconnecting -= OnReconnecting;
        _hubConnection.Reconnected -= OnReconnected;
        _hubConnection.Closed -= OnClosed;
        
        Dispose();
    }
}

/// <summary>
/// Delegate for handling SignalR messages through middleware
/// </summary>
public delegate Task SignalRMessageHandler(object message, SignalRContext context, CancellationToken cancellationToken);

/// <summary>
/// Context passed through the SignalR middleware pipeline
/// </summary>
public class SignalRContext
{
    public string MethodName { get; set; } = string.Empty;
    public string? ConnectionId { get; set; }
    public HubConnectionState State { get; set; }
    public Dictionary<string, object?> Items { get; } = new();
}