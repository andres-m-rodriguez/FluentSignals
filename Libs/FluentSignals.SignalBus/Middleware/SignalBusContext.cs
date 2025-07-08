namespace FluentSignals.SignalBus.Middleware;

/// <summary>
/// Context passed through the middleware pipeline
/// </summary>
public class SignalBusContext
{
    /// <summary>
    /// The message being published
    /// </summary>
    public object Message { get; init; } = null!;

    /// <summary>
    /// The type of the message
    /// </summary>
    public Type MessageType { get; init; } = null!;

    /// <summary>
    /// Items that can be shared between middleware
    /// </summary>
    public Dictionary<string, object?> Items { get; } = new();

    /// <summary>
    /// Cancellation token for the operation
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Timestamp when the message was published
    /// </summary>
    public DateTime PublishedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the message should continue to be processed
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Optional correlation ID for tracking
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The number of subscribers that will receive this message
    /// </summary>
    public int SubscriberCount { get; set; }
}