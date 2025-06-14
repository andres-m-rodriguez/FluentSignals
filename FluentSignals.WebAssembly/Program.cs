using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FluentSignals.Blazor.Extensions;
using FluentSignals.Options.HttpResource;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add FluentSignals Blazor services
builder.Services.AddFluentSignalsBlazor(options =>
{
    options.WithBaseUrl(builder.HostEnvironment.BaseAddress)
           .WithTimeout(TimeSpan.FromSeconds(30));
});

await builder.Build().RunAsync();
