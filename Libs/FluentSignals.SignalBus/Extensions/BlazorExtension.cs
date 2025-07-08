using FluentSignals.SignalBus.Configuration;
using FluentSignals.SignalBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentSignals.SignalBus.Extensions;

public static class SignalBusExtensions
{
    /// <summary>
    /// Adds SignalBus services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">Service lifetime (default: Scoped for Blazor compatibility)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSignalBus(
        this IServiceCollection services, 
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return AddSignalBus(services, _ => { }, lifetime);
    }

    /// <summary>
    /// Adds SignalBus services to the service collection with configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <param name="lifetime">Service lifetime (default: Scoped for Blazor compatibility)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSignalBus(
        this IServiceCollection services,
        Action<SignalBusOptions> configure,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        // Register options
        services.Configure(configure);

        // Register the SignalBus service
        services.Add(new ServiceDescriptor(
            typeof(ISignalBus),
            provider =>
            {
                var logger = provider.GetService<ILogger<SignalBusService>>();
                var options = provider.GetRequiredService<IOptions<SignalBusOptions>>();
                return new SignalBusService(logger, options.Value, provider);
            },
            lifetime));

        return services;
    }
}
