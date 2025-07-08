using FluentSignals.SignalBus.Middleware;

namespace FluentSignals.SignalBus.Configuration;

/// <summary>
/// Configuration options for SignalBus
/// </summary>
public class SignalBusOptions
{
    /// <summary>
    /// Enable or disable statistics collection
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// Enable or disable automatic correlation ID generation
    /// </summary>
    public bool EnableCorrelationId { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent message processing tasks
    /// </summary>
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount * 2;

    /// <summary>
    /// Default timeout for async operations
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Configure the middleware pipeline
    /// </summary>
    internal Action<SignalBusMiddlewareBuilder>? MiddlewareConfiguration { get; set; }

    /// <summary>
    /// Configure middleware for the SignalBus
    /// </summary>
    public SignalBusOptions UseMiddleware(Action<SignalBusMiddlewareBuilder> configure)
    {
        MiddlewareConfiguration = configure;
        return this;
    }
}