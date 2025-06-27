using FluentSignals.Contracts;

namespace FluentSignals.Contracts;

public interface ISignal : IDisposable
{
    List<ISignalSubscriptionContract> Subscribers { get; }
    ISignalSubscriptionContract Subscribe(ISignal subscriber);
    ISignalSubscriptionContract Subscribe(Action action);
    void Unsubscribe(Guid subscriptionId);

    void Notify();
}
public interface ISignal<T> : ISignal
{
    T Value { get; set; }
    ISignalSubscriptionContract Subscribe(Action<T> action);
}
