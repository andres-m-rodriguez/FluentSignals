using FluentSignals.SignalR.Core;

namespace FluentSignals.SignalR;

/// <summary>
/// Interface for SignalR middleware components
/// </summary>
public interface ISignalRMiddleware
{
    /// <summary>
    /// Processes a SignalR message through the middleware pipeline
    /// </summary>
    Task InvokeAsync(object message, SignalRContext context, SignalRMessageHandler next, CancellationToken cancellationToken);
}