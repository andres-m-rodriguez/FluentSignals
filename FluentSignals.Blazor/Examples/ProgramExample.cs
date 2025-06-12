using FluentSignals.Blazor.Extensions;
using FluentSignals.Blazor.Http;
using FluentSignals.Options.HttpResource;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;

namespace FluentSignals.Blazor.Examples;

public class ProgramExample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Example 1: Basic setup with default configuration
        services.AddFluentSignalsBlazor();

        // Example 2: Setup with custom HttpResource configuration
        services.AddFluentSignalsBlazor(options => options
            .WithBaseUrl("https://api.example.com")
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithHeader("X-API-Version", "1.0")
            .WithRetry(3, HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout));

        // Example 3: Setup with custom factory
        services.AddFluentSignalsBlazorWithFactory(sp => 
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var options = new HttpResourceOptions()
                .UseHttpClientFactory(httpClientFactory, "MyApiClient")
                .WithRetry(config =>
                {
                    config.MaxRetryAttempts = 5;
                    config.UseExponentialBackoff = true;
                });
            
            return new HttpResourceFactory(sp, options);
        });
    }
}