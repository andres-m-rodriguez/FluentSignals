namespace FluentSignals.SignalBus.Contracts;

/// <summary>
/// Context information about an error that occurred during message processing
/// </summary>
public class SignalBusErrorContext
{
    /// <summary>
    /// The message that was being processed when the error occurred
    /// </summary>
    public object Message { get; init; } = null!;

    /// <summary>
    /// The type of the message
    /// </summary>
    public Type MessageType { get; init; } = null!;

    /// <summary>
    /// The exception that was thrown
    /// </summary>
    public Exception Exception { get; init; } = null!;

    /// <summary>
    /// The subscription that failed
    /// </summary>
    public SignalBusSubscription? Subscription { get; init; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime ErrorTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the error was from an async handler
    /// </summary>
    public bool IsAsyncHandler { get; init; }
}