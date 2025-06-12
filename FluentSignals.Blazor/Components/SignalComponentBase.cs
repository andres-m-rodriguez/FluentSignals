using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;
using FluentSignals.Blazor.Extensions;
using Microsoft.AspNetCore.Components;

namespace FluentSignals.Blazor.Components;

/// <summary>
/// Base class for Blazor components that use signals.
/// Provides automatic subscription management and StateHasChanged calls.
/// </summary>
public abstract class SignalComponentBase : ComponentBase, IDisposable
{
    private readonly List<ISignalSubscriptionContract> _subscriptions = new();
    private readonly List<ISignal> _subscribedSignals = new();
    private readonly HashSet<ISignal> _processingSignals = new();
    private bool _disposed;

    /// <summary>
    /// Subscribes to a typed signal with automatic StateHasChanged.
    /// </summary>
    protected void SubscribeToSignal<T>(ISignal<T> signal, Action<T> handler)
    {
        // Check if we're already processing this signal to prevent infinite loops
        if (!_processingSignals.Add(signal))
        {
            return;
        }

        try
        {
            if (signal is TypedSignal<T> typedSignal)
            {
                var subscription = typedSignal.Subscribe(value =>
                {
                    handler(value);
                    InvokeAsync(StateHasChanged);
                });
                _subscriptions.Add(subscription);
                _subscribedSignals.Add(signal);
            }
            
            // Check if this is a composite signal and subscribe to internal signals
            if (signal is ICompositeSignal compositeSignal)
            {
                foreach (var internalSignal in compositeSignal.GetInternalSignals())
                {
                    SubscribeToSignal(internalSignal, () => { });
                }
            }
        }
        finally
        {
            _processingSignals.Remove(signal);
        }
    }

    /// <summary>
    /// Subscribes to a signal with automatic StateHasChanged.
    /// </summary>
    protected void SubscribeToSignal(ISignal signal, Action handler)
    {
        // Check if we're already processing this signal to prevent infinite loops
        if (!_processingSignals.Add(signal))
        {
            return;
        }

        try
        {
            var subscription = signal.Subscribe(() =>
            {
                handler();
                InvokeAsync(StateHasChanged);
            });
            _subscriptions.Add(subscription);
            _subscribedSignals.Add(signal);
            
            // Check if this is a composite signal and subscribe to internal signals
            if (signal is ICompositeSignal compositeSignal)
            {
                foreach (var internalSignal in compositeSignal.GetInternalSignals())
                {
                    SubscribeToSignal(internalSignal, () => { });
                }
            }
        }
        finally
        {
            _processingSignals.Remove(signal);
        }
    }

    /// <summary>
    /// Subscribes to multiple signals with the same handler.
    /// Useful when multiple signals should trigger the same UI update.
    /// </summary>
    protected void SubscribeToSignals(IEnumerable<ISignal> signals, Action handler)
    {
        foreach (var signal in signals)
        {
            SubscribeToSignal(signal, handler);
        }
    }

    /// <summary>
    /// Subscribes to multiple typed signals with the same handler.
    /// All signals must be of the same type T.
    /// </summary>
    protected void SubscribeToSignals<T>(IEnumerable<ISignal<T>> signals, Action<T> handler)
    {
        foreach (var signal in signals)
        {
            SubscribeToSignal(signal, handler);
        }
    }

    /// <summary>
    /// Subscribes to a signal without automatic StateHasChanged.
    /// Useful for signals that don't require UI updates.
    /// </summary>
    protected void SubscribeToSignalWithoutUpdate<T>(ISignal<T> signal, Action<T> handler)
    {
        // Check if we're already processing this signal to prevent infinite loops
        if (!_processingSignals.Add(signal))
        {
            return;
        }

        try
        {
            if (signal is TypedSignal<T> typedSignal)
            {
                var subscription = typedSignal.Subscribe(handler);
                _subscriptions.Add(subscription);
                _subscribedSignals.Add(signal);
            }
            
            // Check if this is a composite signal and subscribe to internal signals
            if (signal is ICompositeSignal compositeSignal)
            {
                foreach (var internalSignal in compositeSignal.GetInternalSignals())
                {
                    SubscribeToSignalWithoutUpdate(internalSignal, () => { });
                }
            }
        }
        finally
        {
            _processingSignals.Remove(signal);
        }
    }

    /// <summary>
    /// Subscribes to a signal without automatic StateHasChanged.
    /// Useful for signals that don't require UI updates.
    /// </summary>
    protected void SubscribeToSignalWithoutUpdate(ISignal signal, Action handler)
    {
        // Check if we're already processing this signal to prevent infinite loops
        if (!_processingSignals.Add(signal))
        {
            return;
        }

        try
        {
            var subscription = signal.Subscribe(handler);
            _subscriptions.Add(subscription);
            _subscribedSignals.Add(signal);
            
            // Check if this is a composite signal and subscribe to internal signals
            if (signal is ICompositeSignal compositeSignal)
            {
                foreach (var internalSignal in compositeSignal.GetInternalSignals())
                {
                    SubscribeToSignalWithoutUpdate(internalSignal, () => { });
                }
            }
        }
        finally
        {
            _processingSignals.Remove(signal);
        }
    }

    /// <summary>
    /// Convenience method to subscribe to a signal and trigger StateHasChanged only.
    /// No additional handler logic required.
    /// </summary>
    protected void SubscribeForUpdate(ISignal signal)
    {
        // For composite signals, we need to ensure we subscribe to internal signals
        // The SubscribeToSignal method already handles this
        SubscribeToSignal(signal, () => { });
    }

    /// <summary>
    /// Convenience method to subscribe to multiple signals and trigger StateHasChanged only.
    /// No additional handler logic required.
    /// </summary>
    protected void SubscribeForUpdate(params ISignal[] signals)
    {
        // The SubscribeToSignals method uses SubscribeToSignal internally,
        // which already handles composite signals
        SubscribeToSignals(signals, () => { });
    }

    /// <summary>
    /// Unsubscribes from a specific signal.
    /// </summary>
    protected void UnsubscribeFromSignal(ISignal signal)
    {
        // Check if we're already processing this signal to prevent infinite loops
        if (!_processingSignals.Add(signal))
        {
            return;
        }

        try
        {
            // If this is a composite signal, first unsubscribe from internal signals
            if (signal is ICompositeSignal compositeSignal)
            {
                foreach (var internalSignal in compositeSignal.GetInternalSignals())
                {
                    UnsubscribeFromSignal(internalSignal);
                }
            }
            
            // Then unsubscribe from the main signal
            for (int i = _subscribedSignals.Count - 1; i >= 0; i--)
            {
                if (_subscribedSignals[i] == signal)
                {
                    signal.Unsubscribe(_subscriptions[i].SubscriptionId);
                    _subscribedSignals.RemoveAt(i);
                    _subscriptions.RemoveAt(i);
                }
            }
        }
        finally
        {
            _processingSignals.Remove(signal);
        }
    }

    /// <summary>
    /// Unsubscribes from all signals.
    /// </summary>
    protected void UnsubscribeAll()
    {
        for (int i = 0; i < _subscriptions.Count; i++)
        {
            if (_subscribedSignals[i] != null && _subscriptions[i] != null)
            {
                _subscribedSignals[i].Unsubscribe(_subscriptions[i].SubscriptionId);
            }
        }
        _subscriptions.Clear();
        _subscribedSignals.Clear();
    }

    /// <summary>
    /// Disposes all signal subscriptions.
    /// </summary>
    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes all signal subscriptions.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                UnsubscribeAll();
            }
            _disposed = true;
        }
    }
}