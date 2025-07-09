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
    
    public ISignalSubscriptionContract Subscribe(Action<T> action, Func<T, bool> condition)
    {
        var subscription = new ConditionalTypedSignalSubscription<T>(Guid.NewGuid(), action, condition);
        Subscribers.Add(subscription);
        return subscription;
    }
    
    public ISignalSubscriptionContract SubscribeWeak(Action<T> action)
    {
        var subscription = new WeakTypedSignalSubscription<T>(Guid.NewGuid(), action);
        Subscribers.Add(subscription);
        return subscription;
    }

    public override void Notify()
    {
        // Clean up dead weak references first
        var deadSubscribers = Subscribers
            .Where(s => s is WeakSignalSubscription weak && !weak.IsAlive || 
                       s is WeakTypedSignalSubscription<T> weakTyped && !weakTyped.IsAlive)
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
                case ConditionalTypedSignalSubscription<T> conditionalTyped:
                    if (conditionalTyped.Condition?.Invoke(_value) == true)
                    {
                        conditionalTyped.ActionWithPayload?.Invoke(_value);
                    }
                    break;
                case WeakTypedSignalSubscription<T> weakTyped:
                    weakTyped.Invoke(_value);
                    break;
                case TypedSignalSubscription<T> typed:
                    typed.ActionWithPayload?.Invoke(_value);
                    break;
                case ConditionalSignalSubscription conditional:
                    if (conditional.Condition?.Invoke() == true)
                    {
                        conditional.Action?.Invoke();
                    }
                    break;
                case WeakSignalSubscription weak:
                    weak.Invoke();
                    break;
                case SignalSubscription generic:
                    generic.Action?.Invoke();
                    break;
            }
        }
    }
}
