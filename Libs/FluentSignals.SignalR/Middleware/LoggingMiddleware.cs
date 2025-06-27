using FluentSignals.SignalR.Core;
using Microsoft.Extensions.Logging;

namespace FluentSignals.SignalR.Middleware;

/// <summary>
/// Middleware that logs SignalR messages
/// </summary>
public class LoggingMiddleware : ISignalRMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(
        object message, 
        SignalRContext context, 
        SignalRMessageHandler next, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Received SignalR message on method {MethodName} from connection {ConnectionId}", 
            context.MethodName, 
            context.ConnectionId);

        var startTime = DateTime.UtcNow;
        
        try
        {
            await next(message, context, cancellationToken);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogDebug(
                "Processed SignalR message on method {MethodName} in {Duration}ms", 
                context.MethodName, 
                duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Error processing SignalR message on method {MethodName}", 
                context.MethodName);
            throw;
        }
    }
}