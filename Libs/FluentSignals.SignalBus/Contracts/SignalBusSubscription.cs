using FluentSignals.SignalBus.Services;

namespace FluentSignals.SignalBus.Contracts;

public sealed record SignalBusSubscription : IDisposable, IAsyncDisposable
{
    public Guid SubscriptionId { get; init; }
    public Type MessageContractType { get; init; }
    public Delegate? Delegate { get; init; }
    public WeakReference? WeakDelegate { get; init; }
    public bool IsWeakReference => WeakDelegate != null;
    public bool IsAsync { get; init; }

    private SignalBusService Bus { get; init; }
    private bool _disposed;

    internal void Unsubscribe()
    {
        if (_disposed) return;
        
        // Fire and forget - we don't want to block dispose
        _ = Bus.UnsubscribeAsync(SubscriptionId);
        _disposed = true;
    }

    internal async Task UnsubscribeAsync()
    {
        if (_disposed) return;
        
        await Bus.UnsubscribeAsync(SubscriptionId);
        _disposed = true;
    }

    public void Dispose()
    {
        Unsubscribe();
    }

    public async ValueTask DisposeAsync()
    {
        await UnsubscribeAsync();
    }

    internal SignalBusSubscription(
        Guid subscriptionId, 
        Type messageContractType, 
        Delegate @delegate, 
        SignalBusService bus,
        bool isWeak = false)
    {
        SubscriptionId = subscriptionId;
        MessageContractType = messageContractType;
        Bus = bus;
        
        if (isWeak && @delegate.Target != null)
        {
            WeakDelegate = new WeakReference(@delegate.Target);
            Delegate = @delegate;
        }
        else
        {
            Delegate = @delegate;
        }

        IsAsync = @delegate switch
        {
            Func<object, Task> => true,
            _ => false
        };
    }

    /// <summary>
    /// Try to get the delegate if it's still alive (for weak references)
    /// </summary>
    internal bool TryGetDelegate<T>(out Delegate? del) where T : class
    {
        del = null;

        if (!IsWeakReference)
        {
            del = Delegate;
            return del != null;
        }

        if (WeakDelegate?.Target is object target && Delegate != null)
        {
            // Recreate the delegate with the weak reference target
            try
            {
                if (Delegate is Action<T>)
                {
                    del = Delegate.Method.CreateDelegate(typeof(Action<T>), target);
                }
                else if (Delegate is Func<T, Task>)
                {
                    del = Delegate.Method.CreateDelegate(typeof(Func<T, Task>), target);
                }
                return true;
            }
            catch
            {
                // Target might have been disposed
                return false;
            }
        }

        return false;
    }
}
