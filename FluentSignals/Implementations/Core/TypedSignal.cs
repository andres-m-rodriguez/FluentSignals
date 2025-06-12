using FluentSignals.Contracts;
using FluentSignals.Implementations.Subscriptions;

namespace FluentSignals.Implementations.Core;

public class TypedSignal<T> : Signal, ISignal<T>
{
    public TypedSignal(T value)
    {
        Value = value;
    }

    private T _value = default!;
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                Notify();
            }
        }
    }

    public ISignalSubscriptionContract Subscribe(Action<T> action)
    {
        var subscription = new TypedSignalSubscription<T>(Guid.NewGuid(), action);
        Subscribers.Add(subscription);
        return subscription;
    }

    public override void Notify()
    {
        // Create a copy to avoid concurrent modification exceptions
        var subscribersCopy = Subscribers.ToList();
        
        foreach (var sub in subscribersCopy)
        {
            switch (sub)
            {
                case TypedSignalSubscription<T> typed:
                    typed.ActionWithPayload?.Invoke(_value);
                    break;
                case SignalSubscription generic:
                    generic.Action?.Invoke();
                    break;
            }
        }
    }
}
