using Microsoft.Extensions.DependencyInjection;

namespace FluentSignals.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds FluentSignals core services
        /// </summary>
        public static IServiceCollection AddFluentSignals(this IServiceCollection services)
        {
            // Core signal services can be registered here if needed
            // Currently, signals are created directly and don't require DI registration
            
            return services;
        }
    }
}