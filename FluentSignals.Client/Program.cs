using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FluentSignals.Blazor.Extensions;
using FluentSignals.Client;
using FluentSignals.Options.HttpResource;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client with named client
builder.Services.AddHttpClient("FluentSignals", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// Add default HttpClient with BaseAddress for DI
builder.Services.AddScoped(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return httpClientFactory.CreateClient("FluentSignals");
});

// Configure FluentSignals
builder.Services.AddFluentSignalsBlazor(options =>
{
    options.UseDependencyInjection("FluentSignals")
           .WithBaseUrl(builder.HostEnvironment.BaseAddress)
           .WithTimeout(TimeSpan.FromSeconds(30));
});

await builder.Build().RunAsync();