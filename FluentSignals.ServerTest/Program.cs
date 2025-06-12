using FluentSignals.ServerTest.Components;
using FluentSignals.Blazor.Extensions;
using FluentSignals.Options.HttpResource;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add controllers for API endpoints
builder.Services.AddControllers();

// Get the current application URL
var urls = builder.Configuration["urls"] ?? "https://localhost:5001";
var baseUrl = urls.Split(';')[0]; // Take the first URL if multiple are configured

// Add HttpClient for HTTP resources with BaseAddress
builder.Services.AddHttpClient("FluentSignals", client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

// Add default HttpClient with BaseAddress for DI
builder.Services.AddScoped(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return httpClientFactory.CreateClient("FluentSignals");
});

// Add FluentSignals Blazor services
builder.Services.AddFluentSignalsBlazor(options =>
{
    options.UseDependencyInjection("FluentSignals")
           .WithBaseUrl(baseUrl)
           .WithTimeout(TimeSpan.FromSeconds(30));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map controller endpoints
app.MapControllers();

app.Run();
