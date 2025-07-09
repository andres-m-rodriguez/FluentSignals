using FluentSignals.Contracts;

namespace FluentSignals.Implementations.Subscriptions;

public class WeakTypedSignalSubscription<T> : ISignalSubscriptionContract
{
    private readonly WeakReference _actionRef;
    
    public Guid SubscriptionId { get; private set; }
    
    public WeakTypedSignalSubscription(Guid subscriptionId, Action<T> action)
    {
        SubscriptionId = subscriptionId;
        _actionRef = new WeakReference(action);
    }
    
    public bool IsAlive => _actionRef.IsAlive;
    
    public void Invoke(T value)
    {
        if (_actionRef.Target is Action<T> action)
        {
            action.Invoke(value);
        }
    }
    
    public void Dispose()
    {
        // WeakReference doesn't need explicit disposal
    }
}