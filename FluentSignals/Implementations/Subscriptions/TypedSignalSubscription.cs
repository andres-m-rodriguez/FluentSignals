﻿using FluentSignals.Contracts;

namespace FluentSignals.Implementations.Subscriptions;

public record TypedSignalSubscription<T>(Guid SubscriptionId, Action<T> ActionWithPayload)
    : ISignalSubscriptionContract;