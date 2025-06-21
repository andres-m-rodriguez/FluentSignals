using System.Net;

namespace FluentSignals.Http.Options;

public class RetryOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialRetryDelay { get; set; } = 1000; // milliseconds
    public bool UseExponentialBackoff { get; set; } = true;
    public List<HttpStatusCode> RetryableStatusCodes { get; set; } = [];
    public Func<int, TimeSpan, Task>? OnRetry { get; set; }

    /// <summary>
    /// Creates a deep copy of the current options
    /// </summary>
    public RetryOptions Clone()
    {
        return new RetryOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            InitialRetryDelay = InitialRetryDelay,
            UseExponentialBackoff = UseExponentialBackoff,
            RetryableStatusCodes = [.. RetryableStatusCodes],
            OnRetry = OnRetry
        };
    }
}