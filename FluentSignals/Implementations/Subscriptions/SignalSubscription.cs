using FluentSignals.Contracts;

namespace FluentSignals.Implementations.Subscriptions;

public record SignalSubscription(Guid SubscriptionId, Action Action)
    : ISignalSubscriptionContract
{
    public void Dispose()
    {
        // No resources to dispose in this implementation
    }
}
