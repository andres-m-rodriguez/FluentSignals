using FluentSignals.SignalBus.Configuration;
using FluentSignals.SignalBus.Contracts;
using FluentSignals.SignalBus.Middleware;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FluentSignals.SignalBus.Services;

public sealed class SignalBusService : ISignalBus, IDisposable
{
    private readonly ConcurrentDictionary<Type, List<SignalBusSubscription>> _subscriptions = new();
    private readonly ILogger<SignalBusService>? _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SignalBusOptions _options;
    private readonly SignalBusDelegate _middleware;
    
    // Statistics
    private long _totalMessagesPublished;
    private long _totalErrors;
    private readonly DateTime _startTime = DateTime.UtcNow;
    private readonly ConcurrentDictionary<Type, long> _messageCountByType = new();

    // Error handling
    public event EventHandler<SignalBusErrorContext>? ErrorOccurred;

    public SignalBusService(
        ILogger<SignalBusService>? logger = null, 
        SignalBusOptions? options = null,
        IServiceProvider? serviceProvider = null)
    {
        _logger = logger;
        _options = options ?? new SignalBusOptions();
        
        // Build middleware pipeline
        var builder = new SignalBusMiddlewareBuilder(serviceProvider ?? new EmptyServiceProvider());
        _options.MiddlewareConfiguration?.Invoke(builder);
        _middleware = builder.Build();
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        
        var messageType = typeof(T);
        
        if (_options.EnableStatistics)
        {
            Interlocked.Increment(ref _totalMessagesPublished);
            _messageCountByType.AddOrUpdate(messageType, 1, (_, count) => count + 1);
        }

        // Get subscriptions
        if (!_subscriptions.TryGetValue(messageType, out var subscriptions) || subscriptions.Count == 0)
        {
            _logger?.LogDebug("No subscriptions found for message type {MessageType}", messageType.Name);
            return;
        }

        // Create a copy to avoid collection modified exceptions and clean up dead weak refs
        List<SignalBusSubscription> subscriptionsCopy;
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Clean up dead weak references while copying
            var aliveSubscriptions = new List<SignalBusSubscription>();
            var deadSubscriptions = new List<Guid>();

            foreach (var sub in subscriptions)
            {
                if (sub.IsWeakReference && !sub.TryGetDelegate<T>(out _))
                {
                    deadSubscriptions.Add(sub.SubscriptionId);
                }
                else
                {
                    aliveSubscriptions.Add(sub);
                }
            }

            // Remove dead subscriptions
            foreach (var deadId in deadSubscriptions)
            {
                subscriptions.RemoveAll(s => s.SubscriptionId == deadId);
                _logger?.LogDebug("Removed dead weak subscription {SubscriptionId}", deadId);
            }

            subscriptionsCopy = aliveSubscriptions;
        }
        finally
        {
            _semaphore.Release();
        }

        // Create context for middleware
        var context = new SignalBusContext
        {
            Message = message,
            MessageType = messageType,
            CancellationToken = cancellationToken,
            SubscriberCount = subscriptionsCopy.Count,
            CorrelationId = _options.EnableCorrelationId ? Guid.NewGuid().ToString() : null
        };

        // Run through middleware pipeline
        await _middleware(context);

        // If middleware cancelled the message, don't process subscriptions
        if (context.IsCancelled)
        {
            _logger?.LogDebug("Message {MessageType} was cancelled by middleware", messageType.Name);
            return;
        }

        // Process subscriptions
        var tasks = new List<Task>();
        foreach (var subscription in subscriptionsCopy)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var task = ProcessSubscriptionAsync(subscription, message, messageType);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessSubscriptionAsync<T>(SignalBusSubscription subscription, T message, Type messageType) where T : class
    {
        try
        {
            Delegate? handler = null;

            if (subscription.IsWeakReference)
            {
                if (!subscription.TryGetDelegate<T>(out handler))
                {
                    _logger?.LogDebug("Weak reference target is no longer alive for subscription {SubscriptionId}", subscription.SubscriptionId);
                    return;
                }
            }
            else
            {
                handler = subscription.Delegate;
            }

            switch (handler)
            {
                case Action<T> action:
                    await Task.Run(() => action(message));
                    break;
                    
                case Func<T, Task> asyncHandler:
                    await asyncHandler(message);
                    break;
                    
                default:
                    _logger?.LogWarning("Unknown delegate type for subscription {SubscriptionId}", subscription.SubscriptionId);
                    break;
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _totalErrors);
            
            var errorContext = new SignalBusErrorContext
            {
                Message = message,
                MessageType = messageType,
                Exception = ex,
                Subscription = subscription,
                IsAsyncHandler = subscription.IsAsync
            };

            _logger?.LogError(ex, "Error processing message {MessageType} in subscription {SubscriptionId}", 
                messageType.Name, subscription.SubscriptionId);

            try
            {
                ErrorOccurred?.Invoke(this, errorContext);
            }
            catch (Exception errorHandlerEx)
            {
                _logger?.LogError(errorHandlerEx, "Error in error handler");
            }
        }
    }

    public Task<SignalBusSubscription> Subscribe<TMessage>(Action<TMessage> action, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        return SubscribeInternal<TMessage>(action, false, false);
    }

    public Task<SignalBusSubscription> SubscribeAsync<TMessage>(Func<TMessage, Task> handler, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        return SubscribeInternal<TMessage>(handler, false, false);
    }

    public Task<SignalBusSubscription> SubscribeSingle<TMessage>(Action<TMessage> action, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        return SubscribeInternal<TMessage>(action, true, false);
    }

    public Task<SignalBusSubscription> SubscribeSingleAsync<TMessage>(Func<TMessage, Task> handler, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        return SubscribeInternal<TMessage>(handler, true, false);
    }

    public Task<SignalBusSubscription> SubscribeWeak<TMessage>(Action<TMessage> action, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        return SubscribeInternal<TMessage>(action, false, true);
    }

    public Task<SignalBusSubscription> SubscribeWeakAsync<TMessage>(Func<TMessage, Task> handler, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        return SubscribeInternal<TMessage>(handler, false, true);
    }

    private async Task<SignalBusSubscription> SubscribeInternal<TMessage>(
        Delegate handler, 
        bool singleSubscription,
        bool isWeak) 
        where TMessage : class
    {
        var subscription = new SignalBusSubscription(
            Guid.CreateVersion7(),
            typeof(TMessage),
            handler,
            this,
            isWeak);

        await _semaphore.WaitAsync();
        try
        {
            var list = _subscriptions.GetOrAdd(typeof(TMessage), _ => new List<SignalBusSubscription>());

            if (singleSubscription)
            {
                // Check if a subscription from the same source already exists
                var existingSubscription = list.FirstOrDefault(s => 
                    s.Delegate?.Target?.GetType() == handler.Target?.GetType());
                
                if (existingSubscription != null)
                {
                    _logger?.LogDebug("Subscription already exists for type {Type} from {Source}", 
                        typeof(TMessage).Name, handler.Target?.GetType().Name);
                    return existingSubscription;
                }
            }

            list.Add(subscription);
            _logger?.LogDebug("Added {SubscriptionType} subscription {SubscriptionId} for type {Type}", 
                isWeak ? "weak" : "strong", subscription.SubscriptionId, typeof(TMessage).Name);
        }
        finally
        {
            _semaphore.Release();
        }

        return subscription;
    }

    internal async Task UnsubscribeAsync(Guid subscriptionId)
    {
        await _semaphore.WaitAsync();
        try
        {
            foreach (var kvp in _subscriptions)
            {
                kvp.Value.RemoveAll(s => s.SubscriptionId == subscriptionId);
            }
            
            _logger?.LogDebug("Removed subscription {SubscriptionId}", subscriptionId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    internal void Unsubscribe(Guid subscriptionId)
    {
        UnsubscribeAsync(subscriptionId).GetAwaiter().GetResult();
    }

    public SignalBusStatistics GetStatistics()
    {
        var subscriptionsByType = new Dictionary<string, int>();
        var totalSubscriptions = 0;

        foreach (var kvp in _subscriptions)
        {
            var count = kvp.Value.Count;
            if (count > 0)
            {
                subscriptionsByType[kvp.Key.Name] = count;
                totalSubscriptions += count;
            }
        }

        var messagesByType = _messageCountByType
            .ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);

        return new SignalBusStatistics
        {
            TotalMessagesPublished = _totalMessagesPublished,
            ActiveSubscriptions = totalSubscriptions,
            SubscriptionsByType = subscriptionsByType,
            MessagesByType = messagesByType,
            TotalErrors = _totalErrors,
            StatisticsStartTime = _startTime
        };
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
        _subscriptions.Clear();
    }
}