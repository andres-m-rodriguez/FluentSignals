using FluentSignals.Contracts;

namespace FluentSignals.SignalBus;

/// <summary>
/// Wrapper for signal subscriptions that implements IDisposable for easier cleanup
/// </summary>
public sealed class SignalSubscriptionWrapper : IDisposable
{
    private readonly ISignal _signal;
    private readonly ISignalSubscriptionContract _subscription;
    private bool _disposed;

    public SignalSubscriptionWrapper(ISignal signal, ISignalSubscriptionContract subscription)
    {
        _signal = signal ?? throw new ArgumentNullException(nameof(signal));
        _subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
    }

    public ISignalSubscriptionContract Subscription => _subscription;

    public void Dispose()
    {
        if (!_disposed)
        {
            _signal.Unsubscribe(_subscription.SubscriptionId);
            _disposed = true;
        }
    }
}