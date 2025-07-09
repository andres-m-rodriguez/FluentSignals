using FluentSignals.Contracts;

namespace FluentSignals.Implementations.Subscriptions;

public class WeakSignalSubscription : ISignalSubscriptionContract
{
    private readonly WeakReference _actionRef;
    
    public Guid SubscriptionId { get; private set; }
    
    public WeakSignalSubscription(Guid subscriptionId, Action action)
    {
        SubscriptionId = subscriptionId;
        _actionRef = new WeakReference(action);
    }
    
    public bool IsAlive => _actionRef.IsAlive;
    
    public void Invoke()
    {
        if (_actionRef.Target is Action action)
        {
            action.Invoke();
        }
    }
    
    public void Dispose()
    {
        // WeakReference doesn't need explicit disposal
    }
}