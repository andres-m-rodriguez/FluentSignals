
using FluentSignals.SignalBus.Contracts;

namespace FluentSignals.SignalBus.Services;

public interface ISignalBus
{
    /// <summary>
    /// Publishes a message asynchronously with all configured processing
    /// </summary>
    /// <typeparam name="T">The type of message</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Subscribe to messages with a synchronous handler
    /// </summary>
    Task<SignalBusSubscription> Subscribe<TMessage>(Action<TMessage> action, CancellationToken cancellationToken = default) 
        where TMessage : class;

    /// <summary>
    /// Subscribe to messages with an asynchronous handler
    /// </summary>
    Task<SignalBusSubscription> SubscribeAsync<TMessage>(Func<TMessage, Task> handler, CancellationToken cancellationToken = default) 
        where TMessage : class;

    /// <summary>
    /// Subscribe only once per target type (prevents duplicates)
    /// </summary>
    Task<SignalBusSubscription> SubscribeSingle<TMessage>(Action<TMessage> action, CancellationToken cancellationToken = default) 
        where TMessage : class;

    /// <summary>
    /// Subscribe only once per target type with async handler
    /// </summary>
    Task<SignalBusSubscription> SubscribeSingleAsync<TMessage>(Func<TMessage, Task> handler, CancellationToken cancellationToken = default) 
        where TMessage : class;

    /// <summary>
    /// Subscribe with weak reference to prevent memory leaks
    /// </summary>
    Task<SignalBusSubscription> SubscribeWeak<TMessage>(Action<TMessage> action, CancellationToken cancellationToken = default) 
        where TMessage : class;

    /// <summary>
    /// Subscribe with weak reference and async handler
    /// </summary>
    Task<SignalBusSubscription> SubscribeWeakAsync<TMessage>(Func<TMessage, Task> handler, CancellationToken cancellationToken = default) 
        where TMessage : class;

    /// <summary>
    /// Get current statistics about the signal bus
    /// </summary>
    SignalBusStatistics GetStatistics();

    /// <summary>
    /// Subscribe to error events
    /// </summary>
    event EventHandler<SignalBusErrorContext>? ErrorOccurred;
}
