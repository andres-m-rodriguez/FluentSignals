using FluentSignals.Contracts;

namespace FluentSignals.SignalBus;

/// <summary>
/// Interface for the signal bus implementation
/// </summary>
public interface ISignalBus
{
    /// <summary>
    /// Gets or creates a signal for the specified message type
    /// </summary>
    /// <typeparam name="T">The type of message</typeparam>
    /// <returns>The signal for the message type</returns>
    ISignal<T> GetOrCreateSignal<T>() where T : class;
    
    /// <summary>
    /// Registers a consumer for a specific message type
    /// </summary>
    /// <typeparam name="T">The type of message</typeparam>
    /// <returns>The consumer for the message type</returns>
    ISignalConsumer<T> GetConsumer<T>() where T : class;
    
    /// <summary>
    /// Publishes a message asynchronously with all configured processing
    /// </summary>
    /// <typeparam name="T">The type of message</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Gets current metrics if performance monitoring is enabled
    /// </summary>
    MessageBusMetrics? GetMetrics();
    
    /// <summary>
    /// Clears all signals and subscriptions
    /// </summary>
    void Clear();
}

/// <summary>
/// Simple metrics class
/// </summary>
public class MessageBusMetrics
{
    public int TotalMessagesPublished { get; set; }
    public int TotalMessagesProcessed { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<Type, MessageTypeMetricsSnapshot> MessageTypeMetrics { get; set; } = new();
}

/// <summary>
/// Metrics for a specific message type
/// </summary>
public class MessageTypeMetricsSnapshot
{
    public int PublishedCount { get; set; }
    public int ProcessedCount { get; set; }
}