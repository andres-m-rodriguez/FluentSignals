using FluentSignals.Contracts;

namespace FluentSignals.Implementations.Subscriptions;

public record SignalSubscription(Guid SubscriptionId, Action Action)
    : ISignalSubscriptionContract;
