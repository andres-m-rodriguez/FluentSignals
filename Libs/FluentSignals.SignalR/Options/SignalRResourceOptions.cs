namespace FluentSignals.SignalR.Options;

/// <summary>
/// Options for configuring SignalR resources
/// </summary>
public class SignalRResourceOptions
{
    /// <summary>
    /// Whether to reuse hub connections for the same URL
    /// </summary>
    public bool ReuseConnections { get; set; } = true;

    /// <summary>
    /// Whether to enable automatic reconnection by default
    /// </summary>
    public bool EnableAutomaticReconnect { get; set; } = true;

    /// <summary>
    /// Reconnect intervals for automatic reconnection
    /// </summary>
    public TimeSpan[] ReconnectIntervals { get; set; } = new[]
    {
        TimeSpan.FromSeconds(0),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Default server timeout
    /// </summary>
    public TimeSpan? DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
}