using Microsoft.Extensions.Logging;

namespace Playground.Console.Helpers;

using Console = System.Console;

public class ConsoleLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        
        switch (logLevel)
        {
            case LogLevel.Error:
            case LogLevel.Critical:
                ConsoleHelper.WriteError($"[{typeof(T).Name}] {message}");
                break;
            case LogLevel.Warning:
                ConsoleHelper.WriteWarning($"[{typeof(T).Name}] {message}");
                break;
            case LogLevel.Information:
                ConsoleHelper.WriteInfo($"[{typeof(T).Name}] {message}");
                break;
            default:
                Console.WriteLine($"[{typeof(T).Name}] {message}");
                break;
        }
        
        if (exception != null)
        {
            ConsoleHelper.WriteError($"Exception: {exception}");
        }
    }
}