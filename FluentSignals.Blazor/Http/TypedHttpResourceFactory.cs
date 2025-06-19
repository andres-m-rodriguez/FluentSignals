using System;
using System.Net.Http;
using FluentSignals.Options.HttpResource;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentSignals.Blazor.Http
{
    /// <summary>
    /// Factory for creating typed HTTP resources with dependency injection support
    /// </summary>
    /// <typeparam name="TResource">The type of HTTP resource to create</typeparam>
    public class TypedHttpResourceFactory<TResource> : ITypedHttpResourceFactory<TResource> 
        where TResource : TypedHttpResource, new()
    {
        private readonly IHttpResourceFactory _httpResourceFactory;
        private readonly IOptions<HttpResourceOptions> _options;
        private readonly IServiceProvider _serviceProvider;

        public TypedHttpResourceFactory(
            IHttpResourceFactory httpResourceFactory,
            IOptions<HttpResourceOptions> options,
            IServiceProvider serviceProvider)
        {
            _httpResourceFactory = httpResourceFactory ?? throw new ArgumentNullException(nameof(httpResourceFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Creates an instance of the typed HTTP resource
        /// </summary>
        public TResource Create()
        {
            return Create(null);
        }

        /// <summary>
        /// Creates an instance of the typed HTTP resource with custom options
        /// </summary>
        public TResource Create(Action<HttpResourceOptions>? configure)
        {
            // Get the base URL from the attribute if present
            var resourceType = typeof(TResource);
            var attribute = (HttpResourceAttribute?)Attribute.GetCustomAttribute(resourceType, typeof(HttpResourceAttribute));
            var baseUrl = attribute?.BaseUrl ?? string.Empty;

            // Create HttpClient using the factory
            HttpResource httpResource;
            if (configure != null)
            {
                httpResource = _httpResourceFactory.CreateWithOptions(configure);
            }
            else if (!string.IsNullOrEmpty(_options.Value.BaseUrl))
            {
                httpResource = _httpResourceFactory.CreateWithBaseUrl(_options.Value.BaseUrl);
            }
            else
            {
                httpResource = _httpResourceFactory.Create();
            }

            // Extract HttpClient from the resource (this is a workaround)
            var httpClient = GetHttpClient(httpResource);

            // Create the typed resource instance
            var resource = new TResource();
            
            // Initialize with the HttpClient and options
            var options = _options.Value.Clone();
            if (configure != null)
            {
                configure(options);
            }
            
            resource.Initialize(httpClient, baseUrl, options);
            
            return resource;
        }

        private static HttpClient GetHttpClient(HttpResource resource)
        {
            // This is a workaround to get the HttpClient from the resource
            // In a real implementation, HttpResource should expose this property
            var field = resource.GetType().GetField("_httpClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(resource) as HttpClient ?? throw new InvalidOperationException("Unable to extract HttpClient from HttpResource");
        }
    }

    /// <summary>
    /// Extension methods for registering typed HTTP resource factories
    /// </summary>
    public static class TypedHttpResourceFactoryExtensions
    {
        /// <summary>
        /// Registers a typed HTTP resource factory
        /// </summary>
        public static IServiceCollection AddTypedHttpResourceFactory<TResource>(this IServiceCollection services)
            where TResource : TypedHttpResource, new()
        {
            services.AddScoped<ITypedHttpResourceFactory<TResource>, TypedHttpResourceFactory<TResource>>();
            services.AddScoped<TResource>(provider =>
            {
                var factory = provider.GetRequiredService<ITypedHttpResourceFactory<TResource>>();
                return factory.Create();
            });
            return services;
        }
    }
}