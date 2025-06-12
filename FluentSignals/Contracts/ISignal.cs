using FluentSignals.Contracts;

namespace FluentSignals.Contracts;

public interface ISignal : IDisposable
{
    List<ISignalSubscriptionContract> Subscribers { get; }
    ISignalSubscriptionContract Subscribe(ISignal subscriber);
    void Unsubscribe(Guid subscriptionId);

    void Notify();
}
public interface ISignal<T> : ISignal
{
    T Value { get; set; }
}
