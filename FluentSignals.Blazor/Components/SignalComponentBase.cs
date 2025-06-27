using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;
using Microsoft.AspNetCore.Components;

namespace FluentSignals.Blazor.Components;

/// <summary>
/// Base class for Blazor components that use signals.
/// Provides automatic subscription management and StateHasChanged calls.
/// </summary>
public abstract class SignalComponentBase : ComponentBase, IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
