using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;
using FluentSignals.Implementations.Subscriptions;

namespace FluentSignals.Blazor.Extensions;

public static class SignalExtensions
{
    public static ISignalSubscriptionContract Subscribe(this ISignal signal, Action action)
    {
        if (signal is Signal baseSignal)
        {
            var subscription = new SignalSubscription(Guid.NewGuid(), action);
            baseSignal.Subscribers.Add(subscription);
            return subscription;
        }
        throw new InvalidOperationException("Signal must inherit from Signal base class");
    }

    public static ISignalSubscriptionContract Subscribe<T>(this ISignal<T> signal, Action<T> action)
    {
        if (signal is TypedSignal<T> typedSignal)
        {
            return typedSignal.Subscribe(action);
        }
        throw new InvalidOperationException("Signal must inherit from TypedSignal<T> base class");
    }

    public static IDisposable SubscribeDisposable(this ISignal signal, Action action)
    {
        var subscription = signal.Subscribe(action);
        return new DisposableSubscription(signal, subscription.SubscriptionId);
    }

    public static IDisposable SubscribeDisposable<T>(this ISignal<T> signal, Action<T> action)
    {
        if (signal is TypedSignal<T> typedSignal)
        {
            var subscription = typedSignal.Subscribe(action);
            return new DisposableSubscription(signal, subscription.SubscriptionId);
        }
        throw new InvalidOperationException("Signal must inherit from TypedSignal<T> base class");
    }

    private class DisposableSubscription : IDisposable
    {
        private readonly ISignal _signal;
        private readonly Guid _subscriptionId;

        public DisposableSubscription(ISignal signal, Guid subscriptionId)
        {
            _signal = signal;
            _subscriptionId = subscriptionId;
        }

        public void Dispose()
        {
            _signal.Unsubscribe(_subscriptionId);
        }
    }
}