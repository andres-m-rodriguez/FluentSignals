using FluentSignals.Contracts;

namespace FluentSignals.Implementations.Subscriptions;

public record ConditionalTypedSignalSubscription<T>(Guid SubscriptionId, Action<T> ActionWithPayload, Func<T, bool> Condition)
    : TypedSignalSubscription<T>(SubscriptionId, ActionWithPayload);