using FluentSignals.Blazor.Http;
using FluentSignals.Http.Options;
using FluentSignals.Http.Extensions;
using FluentSignals.SignalBus;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSignals.Blazor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluentSignalsBlazor(this IServiceCollection services, Action<HttpResourceOptions>? configureHttpResource = null)
    {
        // Add base FluentSignals services with HttpResource support
        services.AddFluentSignalsHttp(configureHttpResource);
        
        // Add Blazor-specific services
        services.AddScoped<IHttpResourceFactory, HttpResourceFactory>();
        
        return services;
    }
    
    public static IServiceCollection AddFluentSignalsBlazorWithFactory(this IServiceCollection services, Func<IServiceProvider, IHttpResourceFactory> factoryProvider)
    {
        // Add base FluentSignals services
        services.AddFluentSignalsHttp();
        
        // Add custom factory
        services.AddScoped(factoryProvider);
        
        return services;
    }
    
    public static IServiceCollection AddFluentSignalsBlazorWithSignalBus(this IServiceCollection services, Action<HttpResourceOptions>? configureHttpResource = null)
    {
        // Add base FluentSignals Blazor services
        services.AddFluentSignalsBlazor(configureHttpResource);
        
        // Add SignalBus services
        services.AddSignalBus();
        
        return services;
    }
}