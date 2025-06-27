using FluentSignals.SignalR.Factories;
using FluentSignals.SignalR.Middleware;
using FluentSignals.SignalR.Options;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSignals.SignalR.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SignalR resource factory and related services
    /// </summary>
    public static IServiceCollection AddSignalRResourceFactory(this IServiceCollection services)
    {
        return services.AddSignalRResourceFactory(_ => { });
    }

    /// <summary>
    /// Adds SignalR resource factory with configuration
    /// </summary>
    public static IServiceCollection AddSignalRResourceFactory(
        this IServiceCollection services,
        Action<SignalRResourceOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<SignalRResourceFactory>();
        
        // Register default middleware
        services.AddTransient<LoggingMiddleware>();
        services.AddTransient<ReconnectionMiddleware>();
        
        return services;
    }

    /// <summary>
    /// Adds a custom SignalR middleware
    /// </summary>
    public static IServiceCollection AddSignalRMiddleware<TMiddleware>(this IServiceCollection services)
        where TMiddleware : class, ISignalRMiddleware
    {
        services.AddTransient<TMiddleware>();
        return services;
    }
}