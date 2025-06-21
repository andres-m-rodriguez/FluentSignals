namespace FluentSignals.SignalBus;

/// <summary>
/// Simple interface for handling messages in components
/// </summary>
/// <typeparam name="T">The type of message to handle</typeparam>
public interface ISignalHandler<T> where T : class
{
    /// <summary>
    /// Handles the incoming message
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <returns>A task representing the async operation</returns>
    Task Handle(T message);
}