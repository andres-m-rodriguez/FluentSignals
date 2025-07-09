using FluentSignals.Contracts;

namespace FluentSignals.Implementations.Subscriptions;

public record ConditionalSignalSubscription(Guid SubscriptionId, Action Action, Func<bool> Condition)
    : SignalSubscription(SubscriptionId, Action);