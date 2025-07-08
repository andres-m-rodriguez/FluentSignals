namespace FluentSignals.SignalBus.Contracts;

/// <summary>
/// Statistics about the signal bus state and performance
/// </summary>
public class SignalBusStatistics
{
    /// <summary>
    /// Total number of messages published
    /// </summary>
    public long TotalMessagesPublished { get; init; }

    /// <summary>
    /// Total number of active subscriptions
    /// </summary>
    public int ActiveSubscriptions { get; init; }

    /// <summary>
    /// Number of subscriptions by message type
    /// </summary>
    public Dictionary<string, int> SubscriptionsByType { get; init; } = new();

    /// <summary>
    /// Number of messages published by type
    /// </summary>
    public Dictionary<string, long> MessagesByType { get; init; } = new();

    /// <summary>
    /// Total errors encountered
    /// </summary>
    public long TotalErrors { get; init; }

    /// <summary>
    /// Timestamp when statistics started being collected
    /// </summary>
    public DateTime StatisticsStartTime { get; init; }
}