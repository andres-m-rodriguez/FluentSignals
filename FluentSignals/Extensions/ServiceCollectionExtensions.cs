using System;
using System.Linq;
using System.Reflection;
using FluentSignals.Options.HttpResource;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentSignals.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds FluentSignals core services with HTTP resource support
        /// </summary>
        public static IServiceCollection AddFluentSignalsHttpResource(this IServiceCollection services, Action<HttpResourceOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            
            return services;
        }
        
        /// <summary>
        /// Registers a typed HTTP resource
        /// </summary>
        /// <typeparam name="TResource">The typed resource class that inherits from TypedHttpResource</typeparam>
        public static IServiceCollection AddTypedHttpResource<TResource>(this IServiceCollection services)
            where TResource : TypedHttpResource
        {
            services.TryAddScoped<TResource>();
            return services;
        }
        
        /// <summary>
        /// Registers a typed HTTP resource with a specific lifetime
        /// </summary>
        /// <typeparam name="TResource">The typed resource class that inherits from TypedHttpResource</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="lifetime">The service lifetime</param>
        public static IServiceCollection AddTypedHttpResource<TResource>(this IServiceCollection services, ServiceLifetime lifetime)
            where TResource : TypedHttpResource
        {
            services.TryAdd(new ServiceDescriptor(typeof(TResource), typeof(TResource), lifetime));
            return services;
        }
        
        /// <summary>
        /// Registers multiple typed HTTP resources from an assembly
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="assembly">The assembly to scan for typed resources</param>
        /// <param name="lifetime">The service lifetime (defaults to Scoped)</param>
        public static IServiceCollection AddTypedHttpResourcesFromAssembly(this IServiceCollection services, Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            var resourceTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(TypedHttpResource)));
            
            foreach (var resourceType in resourceTypes)
            {
                services.TryAdd(new ServiceDescriptor(resourceType, resourceType, lifetime));
            }
            
            return services;
        }
        
        /// <summary>
        /// Registers typed HTTP resources from the calling assembly
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="lifetime">The service lifetime (defaults to Scoped)</param>
        public static IServiceCollection AddTypedHttpResourcesFromAssemblyContaining<TMarker>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddTypedHttpResourcesFromAssembly(typeof(TMarker).Assembly, lifetime);
        }
    }
}