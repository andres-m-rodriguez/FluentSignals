using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace FluentSignals.SignalBus;

/// <summary>
/// A simple signal bus implementation that provides publish-subscribe functionality
/// </summary>
public class SignalBus : ISignalBus, ISignalPublisher, ISignalConsumerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object> _signals = new();
    private readonly ConcurrentDictionary<Type, MessageQueue> _messageQueues = new();

    public SignalBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public ISignal<T> GetOrCreateSignal<T>() where T : class
    {
        return (ISignal<T>)_signals.GetOrAdd(typeof(T), _ => new TypedSignal<T>(default!));
    }

    /// <inheritdoc/>
    public ISignalConsumer<T> GetConsumer<T>() where T : class
    {
        var signal = GetOrCreateSignal<T>();
        var queue = GetOrCreateMessageQueue<T>();
        return new SignalConsumerAdapter<T>(signal, queue, this);
    }

    /// <inheritdoc/>
    public ISignalConsumer<T> CreateConsumer<T>() where T : class
    {
        return GetConsumer<T>();
    }

    /// <inheritdoc/>
    public void Publish<T>(T message) where T : class
    {
        var signal = GetOrCreateSignal<T>();
        signal.Value = message;
        
        // Also add to queue for queue-based subscribers
        var queue = GetOrCreateMessageQueue<T>();
        queue.Enqueue(message);
    }

    /// <inheritdoc/>
    public Task PublishAsync<T>(T message) where T : class
    {
        Publish(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        return PublishAsync(message);
    }

    /// <inheritdoc/>
    public MessageBusMetrics? GetMetrics()
    {
        // Simple metrics implementation
        return new MessageBusMetrics
        {
            TotalMessagesPublished = 0,
            TotalMessagesProcessed = 0,
            ActiveSubscriptions = _signals.Count,
            ErrorCount = 0,
            MessageTypeMetrics = new Dictionary<Type, MessageTypeMetricsSnapshot>()
        };
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _signals.Clear();
        _messageQueues.Clear();
    }

    private MessageQueue<T> GetOrCreateMessageQueue<T>() where T : class
    {
        return (MessageQueue<T>)_messageQueues.GetOrAdd(typeof(T), _ => new MessageQueue<T>());
    }
}

/// <summary>
/// Base class for message queues
/// </summary>
internal abstract class MessageQueue
{
    public abstract void Clear();
}

/// <summary>
/// Typed message queue that stores messages for later consumption
/// </summary>
internal class MessageQueue<T> : MessageQueue where T : class
{
    private readonly ConcurrentQueue<T> _messages = new();
    private readonly List<QueueSubscription<T>> _queueSubscriptions = new();
    private readonly object _lock = new();

    public void Enqueue(T message)
    {
        _messages.Enqueue(message);
        
        // Notify all queue subscribers
        lock (_lock)
        {
            foreach (var subscription in _queueSubscriptions.ToList())
            {
                try
                {
                    subscription.Handler(message);
                }
                catch
                {
                    // Ignore handler exceptions for now
                }
            }
        }
    }

    public IEnumerable<T> DequeueAll()
    {
        var messages = new List<T>();
        while (_messages.TryDequeue(out var message))
        {
            messages.Add(message);
        }
        return messages;
    }

    public IDisposable SubscribeToQueue(Action<T> handler, bool processExisting)
    {
        var subscription = new QueueSubscription<T>(handler, this);
        
        lock (_lock)
        {
            _queueSubscriptions.Add(subscription);
        }

        // Process existing messages if requested
        if (processExisting)
        {
            foreach (var message in DequeueAll())
            {
                try
                {
                    handler(message);
                }
                catch
                {
                    // Ignore handler exceptions for now
                }
            }
        }

        return subscription;
    }

    public void Unsubscribe(QueueSubscription<T> subscription)
    {
        lock (_lock)
        {
            _queueSubscriptions.Remove(subscription);
        }
    }

    public override void Clear()
    {
        _messages.Clear();
        lock (_lock)
        {
            _queueSubscriptions.Clear();
        }
    }
}

/// <summary>
/// Represents a queue-based subscription
/// </summary>
internal class QueueSubscription<T> : IDisposable where T : class
{
    public Action<T> Handler { get; }
    private readonly MessageQueue<T> _queue;

    public QueueSubscription(Action<T> handler, MessageQueue<T> queue)
    {
        Handler = handler;
        _queue = queue;
    }

    public void Dispose()
    {
        _queue.Unsubscribe(this);
    }
}

/// <summary>
/// Adapter to make ISignal T work as ISignalConsumer T
/// </summary>
internal class SignalConsumerAdapter<T> : ISignalConsumer<T> where T : class
{
    private readonly ISignal<T> _signal;
    private readonly MessageQueue<T> _messageQueue;
    private readonly SignalBus _signalBus;

    public SignalConsumerAdapter(ISignal<T> signal, MessageQueue<T> messageQueue, SignalBus signalBus)
    {
        _signal = signal;
        _messageQueue = messageQueue;
        _signalBus = signalBus;
    }

    public IDisposable Subscribe(Action<T> handler)
    {
        if (_signal is TypedSignal<T> typedSignal)
        {
            var subscription = typedSignal.Subscribe(handler);
            return new SubscriptionWrapper(subscription, _signal);
        }
        throw new InvalidOperationException("Signal is not a TypedSignal");
    }

    public IDisposable SubscribeDisposable(Action<T> handler)
    {
        return Subscribe(handler);
    }

    public Task<IDisposable> SubscribeAsync(Func<T, Task> handler, CancellationToken cancellationToken = default)
    {
        // Convert async handler to sync for now
        var disposable = Subscribe(value =>
        {
            // Fire and forget - not ideal but keeps it simple
            _ = handler(value);
        });
        return Task.FromResult(disposable);
    }

    public IAsyncDisposable SubscribeAsyncDisposable(Func<T, Task> handler)
    {
        var disposable = Subscribe(value =>
        {
            // Fire and forget - not ideal but keeps it simple
            _ = handler(value);
        });
        return new AsyncDisposableWrapper(disposable);
    }

    public IDisposable SubscribeByQueue(Action<T> handler, bool processExistingMessages = true)
    {
        return _messageQueue.SubscribeToQueue(handler, processExistingMessages);
    }

    public IDisposable SubscribeByQueue(Func<T, Task> handler, bool processExistingMessages = true)
    {
        // Convert async handler to sync
        return _messageQueue.SubscribeToQueue(value =>
        {
            // Fire and forget - not ideal but keeps it simple
            _ = handler(value);
        }, processExistingMessages);
    }
}

/// <summary>
/// Wrapper to make ISignalSubscriptionContract work as IDisposable
/// </summary>
internal class SubscriptionWrapper : IDisposable
{
    private readonly ISignalSubscriptionContract _subscription;
    private readonly ISignal _signal;

    public SubscriptionWrapper(ISignalSubscriptionContract subscription, ISignal signal)
    {
        _subscription = subscription;
        _signal = signal;
    }

    public void Dispose()
    {
        _signal.Unsubscribe(_subscription.SubscriptionId);
    }
}

/// <summary>
/// Wrapper to make IDisposable work as IAsyncDisposable
/// </summary>
internal class AsyncDisposableWrapper : IAsyncDisposable
{
    private readonly IDisposable _disposable;

    public AsyncDisposableWrapper(IDisposable disposable)
    {
        _disposable = disposable;
    }

    public ValueTask DisposeAsync()
    {
        _disposable.Dispose();
        return ValueTask.CompletedTask;
    }
}