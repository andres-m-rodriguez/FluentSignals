using FluentSignals.Http.Extensions;
using FluentSignals.Http.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Playground.Console;
using Playground.Console.Helpers;
using Playground.Console.Middleware;
using Playground.Console.Services;

Console.Clear();
ConsoleHelper.WriteHeader("FluentSignals HTTP Benchmarks", ConsoleColor.Magenta);
ConsoleHelper.WriteInfo("Starting local API server...");

var localHostPath = "http://localhost:5123";

// Start API with spinner
using (var spinner = new ConsoleSpinner("Starting test API"))
{
    var runningTask = Task.Run(async () => TestApi.StartAsync(localHostPath));
    await Task.Delay(2000); // Give API time to start
    spinner.Stop("API server started successfully!", true);
}

ConsoleHelper.WriteInfo($"API running at {localHostPath}");
ConsoleHelper.WriteSeparator();

// Setup DI
IServiceCollection services = new ServiceCollection();
services.AddMemoryCache();
services.AddTransient<CacheMiddleware>();
services.AddHttpClient();
services.ConfigureHttpClientDefaults(options =>
{
    options.ConfigureHttpClient(httpClientOptions =>
    {
        httpClientOptions.BaseAddress = new Uri(localHostPath);
    });
});
services.AddHttpResourceFactory(options =>
{
    options.HttpClientPreference = new HttpClientPreference
    {
        Value = HttpClientPreference.Preference.HttpClient,
        ThrowExceptionIfPreferedClientNotFound = false,
    };
});
services.AddScoped<UserHttpService>();
services.AddScoped<BenchmarkService>();
services.AddSingleton<ILogger<CacheMiddleware>, ConsoleLogger<CacheMiddleware>>();
services.AddLogging();

var app = services.BuildServiceProvider();
var benchmarkService = app.GetRequiredService<BenchmarkService>();

// Create menu
var menu = new ConsoleMenu("FluentSignals HTTP Benchmarks")
    .AddItem("Run All Benchmarks", async () => await RunAllBenchmarks(benchmarkService))
    .AddItem("Middleware Performance Benchmark", async () => await RunSingleBenchmark(benchmarkService.RunMiddlewareBenchmark))
    .AddItem("HttpClient vs HttpResource Benchmark", async () => await RunSingleBenchmark(benchmarkService.RunHttpClientVsHttpResourceBenchmark))
    .AddItem("Subscription Overhead Benchmark", async () => await RunSingleBenchmark(benchmarkService.RunSubscriptionOverheadBenchmark))
    .AddItem("Concurrency Benchmark", async () => await RunConcurrencyBenchmark(benchmarkService))
    .AddSeparator()
    .AddItem("Exit", () => Environment.Exit(0));

menu.Show();

async Task RunAllBenchmarks(BenchmarkService benchmarkService)
{
    ConsoleHelper.WriteHeader("Running All Benchmarks", ConsoleColor.Cyan);
    
    // Warm up
    ConsoleHelper.WriteInfo("Warming up...");
    await benchmarkService.WarmupAll();
    Console.WriteLine();
    
    // Run benchmarks
    await benchmarkService.RunMiddlewareBenchmark(1000);
    Console.WriteLine();
    
    await benchmarkService.RunHttpClientVsHttpResourceBenchmark(1000);
    Console.WriteLine();
    
    await benchmarkService.RunSubscriptionOverheadBenchmark(1000);
    Console.WriteLine();
    
    await benchmarkService.RunConcurrencyBenchmark(100, 10);
}

async Task RunSingleBenchmark(Func<int, Task> benchmarkFunc)
{
    Console.Clear();
    ConsoleHelper.WriteInfo("Enter number of iterations (default: 1000): ");
    var input = Console.ReadLine();
    var iterations = string.IsNullOrWhiteSpace(input) ? 1000 : int.Parse(input);
    
    ConsoleHelper.WriteInfo("Warming up...");
    await benchmarkService.WarmupAll();
    Console.WriteLine();
    
    await benchmarkFunc(iterations);
}

async Task RunConcurrencyBenchmark(BenchmarkService benchmarkService)
{
    Console.Clear();
    ConsoleHelper.WriteInfo("Enter total requests (default: 100): ");
    var totalInput = Console.ReadLine();
    var totalRequests = string.IsNullOrWhiteSpace(totalInput) ? 100 : int.Parse(totalInput);
    
    ConsoleHelper.WriteInfo("Enter concurrent requests (default: 10): ");
    var concurrentInput = Console.ReadLine();
    var concurrentRequests = string.IsNullOrWhiteSpace(concurrentInput) ? 10 : int.Parse(concurrentInput);
    
    ConsoleHelper.WriteInfo("Warming up...");
    await benchmarkService.WarmupAll();
    Console.WriteLine();
    
    await benchmarkService.RunConcurrencyBenchmark(totalRequests, concurrentRequests);
}