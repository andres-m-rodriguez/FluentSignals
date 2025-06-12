using System.Net;

namespace FluentSignals.Options.HttpResource;

public class RetryOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialRetryDelay { get; set; } = 1000; // milliseconds
    public bool UseExponentialBackoff { get; set; } = true;
    public List<HttpStatusCode> RetryableStatusCodes { get; set; } = [];
    public Func<int, TimeSpan, Task>? OnRetry { get; set; }
}