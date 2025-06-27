using FluentSignals.SignalR.Core;
using FluentSignals.SignalR.Options;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentSignals.SignalR.Factories;

/// <summary>
/// Factory for creating SignalR resources
/// </summary>
public class SignalRResourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SignalRResourceOptions _options;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly Dictionary<string, HubConnection> _hubConnections = new();

    public SignalRResourceFactory(
        IServiceProvider serviceProvider,
        IOptions<SignalRResourceOptions> options,
        ILoggerFactory? loggerFactory = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates a SignalR resource for receiving messages of type T
    /// </summary>
    public SignalRResource<T> Create<T>(string hubUrl, string methodName, Action<IHubConnectionBuilder>? configure = null)
    {
        var hubConnection = GetOrCreateHubConnection(hubUrl, configure);
        var logger = _loggerFactory?.CreateLogger<SignalRResource<T>>();
        
        return new SignalRResource<T>(hubConnection, methodName, _serviceProvider, logger);
    }

    /// <summary>
    /// Creates a SignalR resource with an existing hub connection
    /// </summary>
    public SignalRResource<T> Create<T>(HubConnection hubConnection, string methodName)
    {
        if (hubConnection == null) throw new ArgumentNullException(nameof(hubConnection));
        
        var logger = _loggerFactory?.CreateLogger<SignalRResource<T>>();
        return new SignalRResource<T>(hubConnection, methodName, _serviceProvider, logger);
    }

    /// <summary>
    /// Creates a SignalR resource with automatic method name detection using attributes
    /// </summary>
    public SignalRResource<T> Create<T>(string hubUrl) where T : class
    {
        var attribute = typeof(T).GetCustomAttributes(typeof(SignalRMethodAttribute), false)
            .FirstOrDefault() as SignalRMethodAttribute;
            
        if (attribute == null)
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} must have a SignalRMethodAttribute to use automatic method detection");
        }

        return Create<T>(hubUrl, attribute.MethodName);
    }

    private HubConnection GetOrCreateHubConnection(string hubUrl, Action<IHubConnectionBuilder>? configure)
    {
        // Reuse connections if configured to do so
        if (_options.ReuseConnections && _hubConnections.TryGetValue(hubUrl, out var existingConnection))
        {
            return existingConnection;
        }

        var builder = new HubConnectionBuilder()
            .WithUrl(hubUrl);

        // Apply default configuration
        if (_options.EnableAutomaticReconnect)
        {
            builder.WithAutomaticReconnect(_options.ReconnectIntervals);
        }

        if (_options.DefaultTimeout.HasValue)
        {
            builder.WithServerTimeout(_options.DefaultTimeout.Value);
        }

        // Apply custom configuration
        configure?.Invoke(builder);

        var hubConnection = builder.Build();

        if (_options.ReuseConnections)
        {
            _hubConnections[hubUrl] = hubConnection;
        }

        return hubConnection;
    }

    /// <summary>
    /// Disposes all cached hub connections
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _hubConnections.Values)
        {
            if (connection.State != HubConnectionState.Disconnected)
            {
                try
                {
                    await connection.StopAsync();
                }
                catch { }
            }
            await connection.DisposeAsync();
        }
        
        _hubConnections.Clear();
    }
}