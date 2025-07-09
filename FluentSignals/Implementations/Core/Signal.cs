using FluentSignals.Contracts;
using FluentSignals.Implementations.Subscriptions;

namespace FluentSignals.Implementations.Core;
public class Signal : ISignal
{
    public List<ISignalSubscriptionContract> Subscribers { get; } = [];

    public ISignalSubscriptionContract Subscribe(ISignal subscriber)
    {
        var subscription = new SignalSubscription(Guid.NewGuid(), subscriber.Notify);
        Subscribers.Add(subscription);
        return subscription;
    }

    public ISignalSubscriptionContract Subscribe(Action action)
    {
        var subscription = new SignalSubscription(Guid.NewGuid(), action);
        Subscribers.Add(subscription);
        return subscription;
    }
    
    public ISignalSubscriptionContract Subscribe(Action action, Func<bool> condition)
    {
        var subscription = new ConditionalSignalSubscription(Guid.NewGuid(), action, condition);
        Subscribers.Add(subscription);
        return subscription;
    }
    
    public ISignalSubscriptionContract SubscribeWeak(Action action)
    {
        var subscription = new WeakSignalSubscription(Guid.NewGuid(), action);
        Subscribers.Add(subscription);
        return subscription;
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        var subscriber = Subscribers.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
        if (subscriber is not null)
        {
            Subscribers.Remove(subscriber);
        }
    }

    public virtual void Notify()
    {
        // Clean up dead weak references first
        var deadSubscribers = Subscribers
            .OfType<WeakSignalSubscription>()
            .Where(w => !w.IsAlive)
            .Cast<ISignalSubscriptionContract>()
            .ToList();
            
        foreach (var dead in deadSubscribers)
        {
            Subscribers.Remove(dead);
        }
        
        // Create a copy to avoid concurrent modification exceptions
        var subscribersCopy = Subscribers.ToList();
        
        foreach (var sub in subscribersCopy)
        {
            switch (sub)
            {
                case ConditionalSignalSubscription conditional:
                    if (conditional.Condition?.Invoke() == true)
                    {
                        conditional.Action?.Invoke();
                    }
                    break;
                case WeakSignalSubscription weak:
                    weak.Invoke();
                    break;
                case SignalSubscription regular:
                    regular.Action?.Invoke();
                    break;
            }
        }
    }

    public virtual void Dispose()
    {
        Subscribers.Clear();
    }
}
