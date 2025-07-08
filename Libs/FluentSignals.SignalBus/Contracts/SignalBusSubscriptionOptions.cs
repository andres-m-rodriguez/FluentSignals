namespace FluentSignals.SignalBus.Contracts;

/// <summary>
/// Options for configuring a signal bus subscription
/// </summary>
public class SignalBusSubscriptionOptions
{
    /// <summary>
    /// Whether to use weak references for the subscription.
    /// Useful to prevent memory leaks when subscribers might be garbage collected.
    /// </summary>
    public bool UseWeakReference { get; set; }

    /// <summary>
    /// Whether to run the handler on a background thread.
    /// Useful for long-running handlers that shouldn't block the publisher.
    /// </summary>
    public bool RunInBackground { get; set; }

    /// <summary>
    /// Maximum number of retry attempts if the handler fails.
    /// Default is 0 (no retries).
    /// </summary>
    public int MaxRetries { get; set; } = 0;

    /// <summary>
    /// Delay between retry attempts.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Filter predicate to determine if the message should be processed.
    /// If null, all messages are processed.
    /// </summary>
    public Func<object, bool>? Filter { get; set; }

    /// <summary>
    /// Priority of this subscription. Higher values are processed first.
    /// Default is 0.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Timeout for async handlers. If null, no timeout is applied.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}