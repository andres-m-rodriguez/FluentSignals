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
        // Create a copy to avoid concurrent modification exceptions
        var subscribersCopy = Subscribers.OfType<SignalSubscription>().ToList();
        
        foreach (var sub in subscribersCopy)
        {
            sub.Action?.Invoke();
        }
    }

    public virtual void Dispose()
    {
        Subscribers.Clear();
    }
}
