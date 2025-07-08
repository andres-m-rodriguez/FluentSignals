namespace FluentSignals.SignalBus.Middleware;

/// <summary>
/// Defines a middleware component in the SignalBus pipeline
/// </summary>
public interface ISignalBusMiddleware
{
    /// <summary>
    /// Process the message through the middleware pipeline
    /// </summary>
    /// <param name="context">The context containing message information</param>
    /// <param name="next">The next middleware in the pipeline</param>
    Task InvokeAsync(SignalBusContext context, SignalBusDelegate next);
}

/// <summary>
/// Delegate for the next middleware in the pipeline
/// </summary>
public delegate Task SignalBusDelegate(SignalBusContext context);