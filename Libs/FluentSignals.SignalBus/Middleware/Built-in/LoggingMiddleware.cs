using Microsoft.Extensions.Logging;

namespace FluentSignals.SignalBus.Middleware.BuiltIn;

/// <summary>
/// Middleware that logs all messages passing through the SignalBus
/// </summary>
public class LoggingMiddleware : ISignalBusMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly LogLevel _logLevel;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger, LogLevel logLevel = LogLevel.Debug)
    {
        _logger = logger;
        _logLevel = logLevel;
    }

    public async Task InvokeAsync(SignalBusContext context, SignalBusDelegate next)
    {
        if (_logger.IsEnabled(_logLevel))
        {
            _logger.Log(_logLevel, 
                "Publishing message {MessageType} with {SubscriberCount} subscribers. CorrelationId: {CorrelationId}",
                context.MessageType.Name,
                context.SubscriberCount,
                context.CorrelationId ?? "N/A");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            await next(context);
            
            if (_logger.IsEnabled(_logLevel))
            {
                _logger.Log(_logLevel,
                    "Message {MessageType} published successfully in {ElapsedMs}ms",
                    context.MessageType.Name,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error publishing message {MessageType} after {ElapsedMs}ms",
                context.MessageType.Name,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}