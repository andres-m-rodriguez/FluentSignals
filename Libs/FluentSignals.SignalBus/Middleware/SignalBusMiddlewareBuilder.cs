using Microsoft.Extensions.DependencyInjection;

namespace FluentSignals.SignalBus.Middleware;

/// <summary>
/// Builder for configuring the SignalBus middleware pipeline
/// </summary>
public class SignalBusMiddlewareBuilder
{
    private readonly List<Func<SignalBusDelegate, SignalBusDelegate>> _components = new();
    private readonly IServiceProvider _serviceProvider;

    public SignalBusMiddlewareBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Add a middleware type to the pipeline
    /// </summary>
    public SignalBusMiddlewareBuilder Use<TMiddleware>() where TMiddleware : ISignalBusMiddleware
    {
        _components.Add(next =>
        {
            var middleware = ActivatorUtilities.CreateInstance<TMiddleware>(_serviceProvider);
            return context => middleware.InvokeAsync(context, next);
        });
        return this;
    }

    /// <summary>
    /// Add a middleware delegate to the pipeline
    /// </summary>
    public SignalBusMiddlewareBuilder Use(Func<SignalBusContext, SignalBusDelegate, Task> middleware)
    {
        _components.Add(next => context => middleware(context, next));
        return this;
    }

    /// <summary>
    /// Add a simple middleware action that doesn't call next
    /// </summary>
    public SignalBusMiddlewareBuilder Run(Func<SignalBusContext, Task> handler)
    {
        _components.Add(_ => context => handler(context));
        return this;
    }

    /// <summary>
    /// Build the middleware pipeline
    /// </summary>
    internal SignalBusDelegate Build()
    {
        SignalBusDelegate pipeline = _ => Task.CompletedTask;

        // Build the pipeline in reverse order
        for (int i = _components.Count - 1; i >= 0; i--)
        {
            pipeline = _components[i](pipeline);
        }

        return pipeline;
    }
}