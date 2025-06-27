using FluentSignals.Http.Factories;
using FluentSignals.Http.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FluentSignals.Http.Extensions;

public static class HttpResourceFactoryExtensions
{
    public static IServiceCollection AddHttpResourceFactory(this IServiceCollection services)
    {
        return services.AddHttpResourceFactory(_ => { });
    }

    public static IServiceCollection AddHttpResourceFactory(
        this IServiceCollection services,
        Action<HttpResourceOptions> configure
    )
    {
        services.Configure(configure);
        services.AddSingleton<HttpResourceFactory>();

        return services;
    }
}
