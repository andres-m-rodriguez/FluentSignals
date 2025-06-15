using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FluentSignals.Blazor.Extensions;
using FluentSignals.Blazor.SignalBus;
using FluentSignals.Options.HttpResource;
using FluentSignals.WebAssembly.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add FluentSignals Blazor services with SignalBus
builder.Services.AddFluentSignalsBlazorWithSignalBus(options =>
{
    options.WithBaseUrl(builder.HostEnvironment.BaseAddress)
           .WithTimeout(TimeSpan.FromSeconds(30));
});

// Register SignalBus consumers
builder.Services.AddSignalConsumer<PersonForAddDto>();

await builder.Build().RunAsync();
