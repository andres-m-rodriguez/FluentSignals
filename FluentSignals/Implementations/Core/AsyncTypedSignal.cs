using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentSignals.Contracts;

namespace FluentSignals.Implementations.Core;

/// <summary>
/// Represents an asynchronous signal that manages loading state and error handling
/// for values that are loaded asynchronously. This signal implements ICompositeSignal
/// to expose its internal state signals.
/// </summary>
/// <typeparam name="T">The type of value this signal holds.</typeparam>
public class AsyncTypedSignal<T> : TypedSignal<T>, IAsyncSignal<T>, ICompositeSignal
{
    public AsyncTypedSignal(T value)
        : base(value) { }

    public ISignal<bool> IsLoading { get; } = new TypedSignal<bool>(false);
    public ISignal<Exception?> Error { get; } = new TypedSignal<Exception?>(default);

    public ISignal<T> SignalValue { get;  }

    public async Task LoadAsync(Func<Task<T>> loader)
    {
        IsLoading.Value = true;
        Error.Value = null;

        try
        {
            var result = await loader();
            Value = result;
        }
        catch (Exception ex)
        {
            Error.Value = ex;
        }
        finally
        {
            IsLoading.Value = false;
        }
    }

    /// <summary>
    /// Gets all internal signals that compose this async signal.
    /// Returns the IsLoading signal, Error signal, and this signal itself (for value changes).
    /// </summary>
    /// <returns>A collection containing the IsLoading, Error, and value signals.</returns>
    public virtual IEnumerable<ISignal> GetInternalSignals()
    {
        yield return IsLoading;
        yield return Error;
        yield return this; // The signal itself for value changes
    }
}
