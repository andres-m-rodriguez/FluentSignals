namespace FluentSignals.SignalBus;

/// <summary>
/// Interface for publishing messages through the signal bus
/// </summary>
public interface ISignalPublisher
{
    /// <summary>
    /// Publishes a message of type T to all subscribers
    /// </summary>
    /// <typeparam name="T">The type of message to publish</typeparam>
    /// <param name="message">The message to publish</param>
    void Publish<T>(T message) where T : class;
    
    /// <summary>
    /// Publishes a message of type T to all subscribers asynchronously
    /// </summary>
    /// <typeparam name="T">The type of message to publish</typeparam>
    /// <param name="message">The message to publish</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<T>(T message) where T : class;
}