using System;
using System.Linq;
using System.Net.Http;
using FluentSignals.Http.Contracts;
using FluentSignals.Http.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentSignals.Http.Resources
{
    /// <summary>
    /// Factory for creating typed HTTP resources
    /// </summary>
    public class TypedHttpResourceFactory<TResource> : ITypedHttpResourceFactory<TResource>
        where TResource : class, ITypedHttpResource
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<HttpResourceOptions> _options;
        private readonly IServiceProvider _serviceProvider;

        public TypedHttpResourceFactory(
            IHttpClientFactory httpClientFactory,
            IOptions<HttpResourceOptions> options,
            IServiceProvider serviceProvider)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public TResource Create()
        {
            return Create(null);
        }

        public TResource Create(Action<HttpResourceOptions>? configure)
        {
            // Clone options if configuration is provided
            var options = _options.Value;
            if (configure != null)
            {
                options = options.Clone();
                configure(options);
            }

            // Create HTTP client
            var httpClient = _httpClientFactory.CreateClient(typeof(TResource).Name);

            // Get base URL from attribute or options
            var baseUrl = GetBaseUrl(options);

            // Create instance
            var resource = ActivatorUtilities.CreateInstance<TResource>(_serviceProvider);

            // Initialize if it has parameterless constructor
            if (resource is TypedHttpResource typedResource)
            {
                typedResource.Initialize(httpClient, baseUrl, options);
            }

            return resource;
        }

        private string GetBaseUrl(HttpResourceOptions options)
        {
            // Check for HttpResourceAttribute
            var attribute = typeof(TResource).GetCustomAttributes(typeof(HttpResourceAttribute), false)
                .FirstOrDefault() as HttpResourceAttribute;

            if (attribute != null)
            {
                return attribute.BaseUrl;
            }

            // Fall back to options
            return options.BaseUrl ?? string.Empty;
        }
    }
}