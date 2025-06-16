using FluentSignals.Resources;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace FluentSignals.Blazor.Resources;

/// <summary>
/// A ResourceSignal that integrates with SignalR for real-time updates
/// </summary>
/// <typeparam name="T">The type of data the resource holds</typeparam>
public class ResourceSignalR<T> : ResourceSignal<T>, IAsyncDisposable
{
    private readonly HubConnection? _hubConnection;
    private readonly string? _methodName;
    private readonly ILogger<ResourceSignalR<T>>? _logger;
    private IDisposable? _signalRSubscription;

    /// <summary>
    /// Creates a ResourceSignalR that connects to a SignalR hub for real-time updates
    /// </summary>
    /// <param name="hubUrl">The URL of the SignalR hub</param>
    /// <param name="methodName">The hub method to listen to for updates</param>
    /// <param name="fetcher">Optional function to fetch initial data</param>
    /// <param name="configureConnection">Optional action to configure the hub connection</param>
    /// <param name="logger">Optional logger</param>
    public ResourceSignalR(
        string hubUrl,
        string methodName,
        Func<CancellationToken, Task<T>>? fetcher = null,
        Action<IHubConnectionBuilder>? configureConnection = null,
        ILogger<ResourceSignalR<T>>? logger = null)
        : base(fetcher)
    {
        _methodName = methodName;
        _logger = logger;

        // Build the hub connection
        var builder = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect();

        // Apply custom configuration
        configureConnection?.Invoke(builder);

        _hubConnection = builder.Build();

        // Set up reconnection handlers
        _hubConnection.Reconnecting += OnReconnecting;
        _hubConnection.Reconnected += OnReconnected;
        _hubConnection.Closed += OnClosed;
    }

    /// <summary>
    /// Creates a ResourceSignalR with an existing HubConnection
    /// </summary>
    /// <param name="hubConnection">Existing hub connection to use</param>
    /// <param name="methodName">The hub method to listen to for updates</param>
    /// <param name="fetcher">Optional function to fetch initial data</param>
    /// <param name="logger">Optional logger</param>
    public ResourceSignalR(
        HubConnection hubConnection,
        string methodName,
        Func<CancellationToken, Task<T>>? fetcher = null,
        ILogger<ResourceSignalR<T>>? logger = null)
        : base(fetcher)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _methodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        _logger = logger;
    }

    /// <summary>
    /// Connects to the SignalR hub and starts listening for updates
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("No hub connection available");
        }

        try
        {
            // Start the connection if not already connected
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                SetMetadata("connectionState", "connecting");
                await _hubConnection.StartAsync(cancellationToken);
            }

            // Subscribe to the SignalR method
            if (!string.IsNullOrEmpty(_methodName))
            {
                _signalRSubscription = _hubConnection.On<T>(_methodName, data =>
                {
                    _logger?.LogDebug("Received SignalR update for method {MethodName}", _methodName);
                    SetData(data);
                    SetMetadata("lastSignalRUpdate", DateTime.UtcNow);
                });
            }

            SetMetadata("connectionState", "connected");
            _logger?.LogInformation("Connected to SignalR hub");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to SignalR hub");
            SetError(ex);
            SetMetadata("connectionState", "error");
            throw;
        }
    }

    /// <summary>
    /// Disconnects from the SignalR hub
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
        {
            _signalRSubscription?.Dispose();
            _signalRSubscription = null;

            await _hubConnection.StopAsync(cancellationToken);
            SetMetadata("connectionState", "disconnected");
            _logger?.LogInformation("Disconnected from SignalR hub");
        }
    }

    /// <summary>
    /// Sends data to the SignalR hub
    /// </summary>
    /// <param name="methodName">The hub method to invoke</param>
    /// <param name="args">Arguments to send to the hub method</param>
    public async Task SendAsync(string methodName, params object?[] args)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("No hub connection available");
        }

        if (_hubConnection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("Hub is not connected");
        }

        await _hubConnection.SendAsync(methodName, args);
    }

    /// <summary>
    /// Invokes a hub method and returns the result
    /// </summary>
    /// <typeparam name="TResult">The type of the result</typeparam>
    /// <param name="methodName">The hub method to invoke</param>
    /// <param name="args">Arguments to send to the hub method</param>
    /// <returns>The result from the hub method</returns>
    public async Task<TResult> InvokeAsync<TResult>(string methodName, params object?[] args)
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("No hub connection available");
        }

        if (_hubConnection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("Hub is not connected");
        }

        return await _hubConnection.InvokeAsync<TResult>(methodName, args);
    }

    /// <summary>
    /// Gets the current SignalR connection state
    /// </summary>
    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Gets whether the SignalR connection is active
    /// </summary>
    public bool IsConnected => ConnectionState == HubConnectionState.Connected;

    private Task OnReconnecting(Exception? exception)
    {
        _logger?.LogWarning(exception, "SignalR connection lost, attempting to reconnect");
        SetMetadata("connectionState", "reconnecting");
        SetMetadata("reconnectingAt", DateTime.UtcNow);
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        _logger?.LogInformation("SignalR connection restored with ID: {ConnectionId}", connectionId);
        SetMetadata("connectionState", "connected");
        SetMetadata("reconnectedAt", DateTime.UtcNow);
        SetMetadata("connectionId", connectionId ?? "");
        return Task.CompletedTask;
    }

    private Task OnClosed(Exception? exception)
    {
        if (exception != null)
        {
            _logger?.LogError(exception, "SignalR connection closed with error");
            SetError(exception);
        }
        else
        {
            _logger?.LogInformation("SignalR connection closed");
        }
        
        SetMetadata("connectionState", "disconnected");
        SetMetadata("disconnectedAt", DateTime.UtcNow);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the resource and closes the SignalR connection
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _signalRSubscription?.Dispose();

        if (_hubConnection != null)
        {
            _hubConnection.Reconnecting -= OnReconnecting;
            _hubConnection.Reconnected -= OnReconnected;
            _hubConnection.Closed -= OnClosed;

            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StopAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error stopping SignalR connection during disposal");
                }
            }

            await _hubConnection.DisposeAsync();
        }

        Dispose();
    }
}