using System;
using System.Linq;
using System.Reflection;
using FluentSignals.Http.Contracts;
using FluentSignals.Http.Options;
using FluentSignals.Http.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentSignals.Http.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds FluentSignals HTTP services
    /// </summary>
    public static IServiceCollection AddFluentSignalsHttp(
        this IServiceCollection services,
        Action<HttpResourceOptions>? configureOptions = null
    )
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register default factory

        return services;
    }

    /// <summary>
    /// Registers a typed HTTP resource
    /// </summary>
    /// <typeparam name="TResource">The typed resource class that inherits from TypedHttpResource</typeparam>
    public static IServiceCollection AddTypedHttpResource<TResource>(
        this IServiceCollection services
    )
        where TResource : class, ITypedHttpResource
    {
        services.TryAddScoped<TResource>();
        services.TryAddScoped<
            ITypedHttpResourceFactory<TResource>,
            TypedHttpResourceFactory<TResource>
        >();
        return services;
    }

    /// <summary>
    /// Registers a typed HTTP resource with a base URL
    /// </summary>
    /// <typeparam name="TResource">The typed resource class that inherits from TypedHttpResource</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="baseUrl">The base URL for the resource</param>
    public static IServiceCollection AddTypedHttpResource<TResource>(
        this IServiceCollection services,
        string baseUrl
    )
        where TResource : class, ITypedHttpResource
    {
        services.Configure<HttpResourceOptions>(options =>
        {
            options.BaseUrl = baseUrl;
        });

        return services.AddTypedHttpResource<TResource>();
    }

    /// <summary>
    /// Registers a typed HTTP resource with a specific lifetime
    /// </summary>
    /// <typeparam name="TResource">The typed resource class that inherits from TypedHttpResource</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">The service lifetime</param>
    public static IServiceCollection AddTypedHttpResource<TResource>(
        this IServiceCollection services,
        ServiceLifetime lifetime
    )
        where TResource : class, ITypedHttpResource
    {
        services.TryAdd(new ServiceDescriptor(typeof(TResource), typeof(TResource), lifetime));
        services.TryAdd(
            new ServiceDescriptor(
                typeof(ITypedHttpResourceFactory<TResource>),
                typeof(TypedHttpResourceFactory<TResource>),
                lifetime
            )
        );
        return services;
    }

    /// <summary>
    /// Registers a typed HTTP resource factory
    /// </summary>
    /// <typeparam name="TResource">The typed resource class that inherits from TypedHttpResource</typeparam>
    public static IServiceCollection AddTypedHttpResourceFactory<TResource>(
        this IServiceCollection services
    )
        where TResource : class, ITypedHttpResource
    {
        services.TryAddScoped<
            ITypedHttpResourceFactory<TResource>,
            TypedHttpResourceFactory<TResource>
        >();
        services.TryAddScoped(provider =>
            provider.GetRequiredService<ITypedHttpResourceFactory<TResource>>().Create()
        );
        return services;
    }

    /// <summary>
    /// Registers multiple typed HTTP resources from an assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan for typed resources</param>
    /// <param name="lifetime">The service lifetime (defaults to Scoped)</param>
    public static IServiceCollection AddTypedHttpResourcesFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        var resourceTypes = assembly
            .GetTypes()
            .Where(t =>
                t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(TypedHttpResource))
            );

        foreach (var resourceType in resourceTypes)
        {
            services.TryAdd(new ServiceDescriptor(resourceType, resourceType, lifetime));

            // Add factory for each type
            var factoryInterface = typeof(ITypedHttpResourceFactory<>).MakeGenericType(
                resourceType
            );
            var factoryImplementation = typeof(TypedHttpResourceFactory<>).MakeGenericType(
                resourceType
            );
            services.TryAdd(
                new ServiceDescriptor(factoryInterface, factoryImplementation, lifetime)
            );
        }

        return services;
    }

    /// <summary>
    /// Registers typed HTTP resources from the calling assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="lifetime">The service lifetime (defaults to Scoped)</param>
    public static IServiceCollection AddTypedHttpResourcesFromAssemblyContaining<TMarker>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        return services.AddTypedHttpResourcesFromAssembly(typeof(TMarker).Assembly, lifetime);
    }
}
