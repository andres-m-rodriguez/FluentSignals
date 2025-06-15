namespace FluentSignals.Blazor.SignalBus;

/// <summary>
/// Factory for creating signal consumers
/// </summary>
public interface ISignalConsumerFactory
{
    /// <summary>
    /// Creates a consumer for the specified message type
    /// </summary>
    /// <typeparam name="T">The type of message to consume</typeparam>
    /// <returns>A consumer for the message type</returns>
    ISignalConsumer<T> CreateConsumer<T>() where T : class;
}