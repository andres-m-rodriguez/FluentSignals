using Microsoft.Extensions.DependencyInjection;

namespace FluentSignals.Blazor.SignalBus;

/// <summary>
/// Extension methods for registering SignalBus services
/// </summary>
public static class SignalBusServiceCollectionExtensions
{
    /// <summary>
    /// Adds the SignalBus services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSignalBus(this IServiceCollection services)
    {
        // Register the signal bus as a singleton to maintain state across the application
        services.AddSingleton<SignalBus>();
        services.AddSingleton<ISignalBus>(provider => provider.GetRequiredService<SignalBus>());
        services.AddSingleton<ISignalConsumerFactory>(provider => provider.GetRequiredService<SignalBus>());
        
        // Register the publisher as scoped to align with typical Blazor component lifecycle
        services.AddScoped<ISignalPublisher>(provider =>
        {
            var signalBus = provider.GetRequiredService<SignalBus>();
            return new SignalPublisher(signalBus);
        });
        
        return services;
    }
    
    /// <summary>
    /// Adds a signal consumer for a specific message type
    /// </summary>
    /// <typeparam name="T">The type of message to consume</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSignalConsumer<T>(this IServiceCollection services) where T : class
    {
        services.AddScoped<ISignalConsumer<T>>(provider =>
        {
            var signalBus = provider.GetRequiredService<ISignalBus>();
            return signalBus.GetConsumer<T>();
        });
        
        return services;
    }
}