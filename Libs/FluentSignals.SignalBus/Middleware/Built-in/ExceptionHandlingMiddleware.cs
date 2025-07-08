using Microsoft.Extensions.Logging;

namespace FluentSignals.SignalBus.Middleware.BuiltIn;

/// <summary>
/// Middleware that handles exceptions in the pipeline
/// </summary>
public class ExceptionHandlingMiddleware : ISignalBusMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware>? _logger;
    private readonly bool _swallowExceptions;
    private readonly Action<Exception, SignalBusContext>? _onException;

    public ExceptionHandlingMiddleware(
        ILogger<ExceptionHandlingMiddleware>? logger = null,
        bool swallowExceptions = false,
        Action<Exception, SignalBusContext>? onException = null)
    {
        _logger = logger;
        _swallowExceptions = swallowExceptions;
        _onException = onException;
    }

    public async Task InvokeAsync(SignalBusContext context, SignalBusDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, 
                "Exception in SignalBus pipeline for message {MessageType}", 
                context.MessageType.Name);

            _onException?.Invoke(ex, context);

            if (!_swallowExceptions)
            {
                throw;
            }

            context.IsCancelled = true;
        }
    }
}