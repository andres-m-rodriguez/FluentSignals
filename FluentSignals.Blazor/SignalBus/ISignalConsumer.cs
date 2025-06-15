using FluentSignals.Contracts;

namespace FluentSignals.Blazor.SignalBus;

/// <summary>
/// Interface for consuming messages from the signal bus
/// </summary>
/// <typeparam name="T">The type of message to consume</typeparam>
public interface ISignalConsumer<T> where T : class
{
    /// <summary>
    /// Subscribes to messages of type T
    /// </summary>
    /// <param name="handler">The handler to execute when a message is received</param>
    /// <returns>A subscription that can be disposed to unsubscribe</returns>
    IDisposable Subscribe(Action<T> handler);
    
    /// <summary>
    /// Subscribes to messages of type T with IDisposable wrapper
    /// </summary>
    /// <param name="handler">The handler to execute when a message is received</param>
    /// <returns>A disposable subscription wrapper</returns>
    IDisposable SubscribeDisposable(Action<T> handler);
    
    /// <summary>
    /// Subscribes to messages of type T asynchronously
    /// </summary>
    /// <param name="handler">The async handler to execute when a message is received</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes with a disposable subscription</returns>
    Task<IDisposable> SubscribeAsync(Func<T, Task> handler, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Subscribes to messages of type T asynchronously with IAsyncDisposable wrapper
    /// </summary>
    /// <param name="handler">The async handler to execute when a message is received</param>
    /// <returns>An async disposable subscription wrapper</returns>
    IAsyncDisposable SubscribeAsyncDisposable(Func<T, Task> handler);
    
    /// <summary>
    /// Subscribes to messages using a queue, ensuring messages are delivered even if the subscriber wasn't active when published
    /// </summary>
    /// <param name="handler">The handler to execute when a message is received</param>
    /// <param name="processExistingMessages">Whether to process messages that were published before subscription</param>
    /// <returns>A disposable subscription wrapper</returns>
    IDisposable SubscribeByQueue(Action<T> handler, bool processExistingMessages = true);
    
    /// <summary>
    /// Subscribes to messages using a queue with async handler
    /// </summary>
    /// <param name="handler">The async handler to execute when a message is received</param>
    /// <param name="processExistingMessages">Whether to process messages that were published before subscription</param>
    /// <returns>A disposable subscription wrapper</returns>
    IDisposable SubscribeByQueue(Func<T, Task> handler, bool processExistingMessages = true);
}