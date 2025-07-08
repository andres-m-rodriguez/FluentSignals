using System.Diagnostics;

namespace FluentSignals.SignalBus.Middleware.BuiltIn;

/// <summary>
/// Middleware that tracks performance metrics
/// </summary>
public class PerformanceMiddleware : ISignalBusMiddleware
{
    private readonly TimeSpan _slowMessageThreshold;
    private readonly Action<SignalBusContext, TimeSpan>? _onSlowMessage;

    public PerformanceMiddleware(
        TimeSpan? slowMessageThreshold = null,
        Action<SignalBusContext, TimeSpan>? onSlowMessage = null)
    {
        _slowMessageThreshold = slowMessageThreshold ?? TimeSpan.FromMilliseconds(100);
        _onSlowMessage = onSlowMessage;
    }

    public async Task InvokeAsync(SignalBusContext context, SignalBusDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await next(context);
        
        stopwatch.Stop();
        
        // Store the elapsed time for other middleware to use
        context.Items["ElapsedTime"] = stopwatch.Elapsed;
        
        if (stopwatch.Elapsed > _slowMessageThreshold)
        {
            _onSlowMessage?.Invoke(context, stopwatch.Elapsed);
        }
    }
}