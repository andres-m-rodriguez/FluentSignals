using System.Diagnostics;
using FluentSignals.Http.Core;
using FluentSignals.Http.Factories;
using Playground.Console.Helpers;
using Playground.Console.Middleware;
using Playground.Console.Models;
using System.Net.Http.Json;
using FluentSignals.Contracts;

namespace Playground.Console.Services;

public class BenchmarkService
{
    private readonly HttpResourceFactory _httpResourceFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;

    public BenchmarkService(HttpResourceFactory httpResourceFactory, IServiceProvider serviceProvider, HttpClient httpClient)
    {
        _httpResourceFactory = httpResourceFactory;
        _serviceProvider = serviceProvider;
        _httpClient = httpClient;
    }

    public async Task WarmupAll()
    {
        // Warmup all scenarios
        for (int i = 0; i < 10; i++)
        {
            // HttpResource
            using var resource = _httpResourceFactory.Create<List<User>>("/api/users");
            await resource.LoadData();
            
            // HttpClient
            await _httpClient.GetFromJsonAsync<List<User>>("/api/users");
            
            // With middleware
            using var resourceWithMiddleware = _httpResourceFactory.Create<List<User>>("/api/users").Use<CacheMiddleware>();
            await resourceWithMiddleware.LoadData();
        }
    }

    public async Task RunMiddlewareBenchmark(int iterations = 1000)
    {
        ConsoleHelper.WriteHeader("Middleware Performance Benchmark");
        System.Console.WriteLine($"Iterations: {iterations}");
        System.Console.WriteLine();

        var withoutMiddleware = await BenchmarkScenario(
            "Without Middleware",
            iterations,
            async () =>
            {
                using var resource = _httpResourceFactory.Create<List<User>>("/api/users");
                await resource.LoadData();
            });

        var withMiddleware = await BenchmarkScenario(
            "With CacheMiddleware",
            iterations,
            async () =>
            {
                using var resource = _httpResourceFactory.Create<List<User>>("/api/users").Use<CacheMiddleware>();
                await resource.LoadData();
            });

        CompareResults(withoutMiddleware, withMiddleware);
    }

    public async Task RunHttpClientVsHttpResourceBenchmark(int iterations = 1000)
    {
        ConsoleHelper.WriteHeader("HttpClient vs HttpResource Benchmark");
        System.Console.WriteLine($"Iterations: {iterations}");
        System.Console.WriteLine();

        var httpClientResult = await BenchmarkScenario(
            "Raw HttpClient",
            iterations,
            async () =>
            {
                var response = await _httpClient.GetFromJsonAsync<List<User>>("/api/users");
            });

        var httpResourceResult = await BenchmarkScenario(
            "HttpResource",
            iterations,
            async () =>
            {
                using var resource = _httpResourceFactory.Create<List<User>>("/api/users");
                await resource.LoadData();
            });

        CompareResults(httpClientResult, httpResourceResult);
    }

    public async Task RunSubscriptionOverheadBenchmark(int iterations = 1000)
    {
        ConsoleHelper.WriteHeader("Subscription Overhead Benchmark");
        System.Console.WriteLine($"Iterations: {iterations}");
        System.Console.WriteLine();

        var noSubscriptions = await BenchmarkScenario(
            "No Subscriptions",
            iterations,
            async () =>
            {
                using var resource = _httpResourceFactory.Create<List<User>>("/api/users");
                await resource.LoadData();
            });

        var withSubscriptions = await BenchmarkScenario(
            "With 5 Subscriptions",
            iterations,
            async () =>
            {
                using var resource = _httpResourceFactory.Create<List<User>>("/api/users");
                
                // Add 5 subscriptions
                var subs = new List<ISignalSubscriptionContract>();
                subs.Add(resource.IsLoading.Subscribe(_ => { }));
                subs.Add(resource.IsDataAvaible.Subscribe(_ => { }));
                subs.Add(resource.Error.Subscribe(_ => { }));
                subs.Add(resource.SignalValue.Subscribe(_ => { }));
                subs.Add(resource.Subscribe(() => { }));
                
                await resource.LoadData();
                
                // Cleanup
                foreach (var sub in subs) 
                {
                    resource.Unsubscribe(sub.SubscriptionId);
                }
            });

        CompareResults(noSubscriptions, withSubscriptions);
    }

    public async Task RunConcurrencyBenchmark(int totalRequests = 100, int concurrentRequests = 10)
    {
        ConsoleHelper.WriteHeader("Concurrency Benchmark");
        System.Console.WriteLine($"Total Requests: {totalRequests}");
        System.Console.WriteLine($"Concurrent Requests: {concurrentRequests}");
        System.Console.WriteLine();

        // Sequential HttpClient
        var sequentialHttpClient = await BenchmarkScenario(
            "Sequential HttpClient",
            1,
            async () =>
            {
                for (int i = 0; i < totalRequests; i++)
                {
                    await _httpClient.GetFromJsonAsync<List<User>>("/api/users");
                }
            });

        // Concurrent HttpClient
        var concurrentHttpClient = await BenchmarkScenario(
            "Concurrent HttpClient",
            1,
            async () =>
            {
                var semaphore = new SemaphoreSlim(concurrentRequests);
                var tasks = new List<Task>();
                
                for (int i = 0; i < totalRequests; i++)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await _httpClient.GetFromJsonAsync<List<User>>("/api/users");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
                
                await Task.WhenAll(tasks);
            });

        // Sequential HttpResource
        var sequentialHttpResource = await BenchmarkScenario(
            "Sequential HttpResource",
            1,
            async () =>
            {
                for (int i = 0; i < totalRequests; i++)
                {
                    using var resource = _httpResourceFactory.Create<List<User>>("/api/users");
                    await resource.LoadData();
                }
            });

        // Concurrent HttpResource
        var concurrentHttpResource = await BenchmarkScenario(
            "Concurrent HttpResource",
            1,
            async () =>
            {
                var semaphore = new SemaphoreSlim(concurrentRequests);
                var tasks = new List<Task>();
                
                for (int i = 0; i < totalRequests; i++)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using var resource = _httpResourceFactory.Create<List<User>>("/api/users");
                            await resource.LoadData();
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
                
                await Task.WhenAll(tasks);
            });

        // Display results
        var results = new[] { sequentialHttpClient, concurrentHttpClient, sequentialHttpResource, concurrentHttpResource };
        DisplayMultipleResults(results);
    }

    private async Task<BenchmarkResult> BenchmarkScenario(string name, int iterations, Func<Task> action)
    {
        var times = new List<double>();
        var stopwatch = new Stopwatch();
        
        // Force GC before benchmark
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        long memoryBefore = GC.GetTotalMemory(false);

        for (int i = 0; i < iterations; i++)
        {
            stopwatch.Restart();
            await action();
            stopwatch.Stop();
            
            times.Add(stopwatch.Elapsed.TotalMilliseconds);
            
            if (iterations >= 100 && (i + 1) % (iterations / 10) == 0)
            {
                ConsoleHelper.WriteProgress(name, i + 1, iterations);
            }
        }

        long memoryAfter = GC.GetTotalMemory(false);
        
        return new BenchmarkResult
        {
            Name = name,
            Times = times,
            MemoryUsed = memoryAfter - memoryBefore,
            TotalTime = times.Sum()
        };
    }

    private void CompareResults(BenchmarkResult baseline, BenchmarkResult comparison)
    {
        System.Console.WriteLine();
        ConsoleHelper.WriteHeader("Results", ConsoleColor.Green);

        var baselineStats = CalculateStats(baseline.Times);
        var comparisonStats = CalculateStats(comparison.Times);

        var results = new[]
        {
            new 
            { 
                Scenario = baseline.Name,
                Min = $"{baselineStats.Min:F2} ms",
                Max = $"{baselineStats.Max:F2} ms",
                Avg = $"{baselineStats.Average:F2} ms",
                P50 = $"{baselineStats.Percentile50:F2} ms",
                P95 = $"{baselineStats.Percentile95:F2} ms",
                P99 = $"{baselineStats.Percentile99:F2} ms",
                Total = $"{baseline.TotalTime:F2} ms",
                Memory = FormatBytes(baseline.MemoryUsed)
            },
            new 
            { 
                Scenario = comparison.Name,
                Min = $"{comparisonStats.Min:F2} ms",
                Max = $"{comparisonStats.Max:F2} ms",
                Avg = $"{comparisonStats.Average:F2} ms",
                P50 = $"{comparisonStats.Percentile50:F2} ms",
                P95 = $"{comparisonStats.Percentile95:F2} ms",
                P99 = $"{comparisonStats.Percentile99:F2} ms",
                Total = $"{comparison.TotalTime:F2} ms",
                Memory = FormatBytes(comparison.MemoryUsed)
            }
        };

        ConsoleHelper.WriteTable(results,
            ("Scenario", r => r.Scenario),
            ("Min", r => r.Min),
            ("Avg", r => r.Avg),
            ("P50", r => r.P50),
            ("P95", r => r.P95),
            ("P99", r => r.P99),
            ("Total", r => r.Total),
            ("Memory", r => r.Memory)
        );

        // Performance comparison
        System.Console.WriteLine();
        ConsoleHelper.WriteInfo("Performance Comparison:");
        
        var avgDiff = comparisonStats.Average - baselineStats.Average;
        var percentDiff = (avgDiff / baselineStats.Average) * 100;
        
        if (avgDiff > 0)
        {
            ConsoleHelper.WriteWarning($"{comparison.Name} is {Math.Abs(percentDiff):F1}% slower ({avgDiff:F2}ms per request)");
        }
        else
        {
            ConsoleHelper.WriteSuccess($"{comparison.Name} is {Math.Abs(percentDiff):F1}% faster ({Math.Abs(avgDiff):F2}ms per request)");
        }
    }

    private void DisplayMultipleResults(BenchmarkResult[] results)
    {
        System.Console.WriteLine();
        ConsoleHelper.WriteHeader("Results", ConsoleColor.Green);

        var tableData = results.Select(r =>
        {
            var stats = CalculateStats(r.Times);
            return new
            {
                Scenario = r.Name,
                Min = $"{stats.Min:F2} ms",
                Avg = $"{stats.Average:F2} ms",
                P95 = $"{stats.Percentile95:F2} ms",
                Total = $"{r.TotalTime:F2} ms",
                Throughput = $"{(r.Times.Count / (r.TotalTime / 1000)):F2} req/s"
            };
        }).ToArray();

        ConsoleHelper.WriteTable(tableData,
            ("Scenario", r => r.Scenario),
            ("Min", r => r.Min),
            ("Avg", r => r.Avg),
            ("P95", r => r.P95),
            ("Total", r => r.Total),
            ("Throughput", r => r.Throughput)
        );
    }

    private Statistics CalculateStats(List<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var count = sorted.Count;
        
        return new Statistics
        {
            Min = sorted.First(),
            Max = sorted.Last(),
            Average = sorted.Average(),
            Percentile50 = GetPercentile(sorted, 0.50),
            Percentile95 = GetPercentile(sorted, 0.95),
            Percentile99 = GetPercentile(sorted, 0.99),
            StdDev = Math.Sqrt(sorted.Average(v => Math.Pow(v - sorted.Average(), 2)))
        };
    }

    private double GetPercentile(List<double> sortedValues, double percentile)
    {
        int index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = Math.Abs(bytes);
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{(bytes < 0 ? "-" : "")}{len:F2} {sizes[order]}";
    }

    private class BenchmarkResult
    {
        public string Name { get; set; } = "";
        public List<double> Times { get; set; } = new();
        public long MemoryUsed { get; set; }
        public double TotalTime { get; set; }
    }

    private class Statistics
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public double Percentile50 { get; set; }
        public double Percentile95 { get; set; }
        public double Percentile99 { get; set; }
        public double StdDev { get; set; }
    }
}