using FluentSignals.SignalBus.Middleware;
using FluentSignals.SignalBus.Middleware.BuiltIn;
using Microsoft.Extensions.Logging;

namespace FluentSignals.SignalBus.Extensions;

/// <summary>
/// Extension methods for configuring SignalBus middleware
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Add logging middleware to the pipeline
    /// </summary>
    public static SignalBusMiddlewareBuilder UseLogging(
        this SignalBusMiddlewareBuilder builder, 
        LogLevel logLevel = LogLevel.Debug)
    {
        return builder.Use<LoggingMiddleware>();
    }

    /// <summary>
    /// Add exception handling middleware to the pipeline
    /// </summary>
    public static SignalBusMiddlewareBuilder UseExceptionHandling(
        this SignalBusMiddlewareBuilder builder,
        bool swallowExceptions = false,
        Action<Exception, SignalBusContext>? onException = null)
    {
        return builder.Use(async (context, next) =>
        {
            var middleware = new ExceptionHandlingMiddleware(null, swallowExceptions, onException);
            await middleware.InvokeAsync(context, next);
        });
    }

    /// <summary>
    /// Add performance tracking middleware to the pipeline
    /// </summary>
    public static SignalBusMiddlewareBuilder UsePerformanceTracking(
        this SignalBusMiddlewareBuilder builder,
        TimeSpan? slowMessageThreshold = null,
        Action<SignalBusContext, TimeSpan>? onSlowMessage = null)
    {
        return builder.Use(async (context, next) =>
        {
            var middleware = new PerformanceMiddleware(slowMessageThreshold, onSlowMessage);
            await middleware.InvokeAsync(context, next);
        });
    }

    /// <summary>
    /// Add validation middleware to the pipeline
    /// </summary>
    public static SignalBusMiddlewareBuilder UseValidation(
        this SignalBusMiddlewareBuilder builder,
        Action<ValidationMiddleware> configure)
    {
        return builder.Use(async (context, next) =>
        {
            var middleware = new ValidationMiddleware();
            configure(middleware);
            await middleware.InvokeAsync(context, next);
        });
    }

    /// <summary>
    /// Add correlation ID generation
    /// </summary>
    public static SignalBusMiddlewareBuilder UseCorrelationId(
        this SignalBusMiddlewareBuilder builder,
        Func<string>? idGenerator = null)
    {
        return builder.Use(async (context, next) =>
        {
            if (string.IsNullOrEmpty(context.CorrelationId))
            {
                context.CorrelationId = idGenerator?.Invoke() ?? Guid.NewGuid().ToString();
            }
            await next(context);
        });
    }

    /// <summary>
    /// Add custom inline middleware
    /// </summary>
    public static SignalBusMiddlewareBuilder UseCustom(
        this SignalBusMiddlewareBuilder builder,
        string name,
        Func<SignalBusContext, SignalBusDelegate, Task> middleware)
    {
        return builder.Use(async (context, next) =>
        {
            context.Items[$"Middleware.{name}.Start"] = DateTime.UtcNow;
            await middleware(context, next);
            context.Items[$"Middleware.{name}.End"] = DateTime.UtcNow;
        });
    }
}