namespace Playground.Console.Helpers;

using Console = System.Console;

public class ConsoleSpinner : IDisposable
{
    private readonly string[] _frames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
    private readonly string _message;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _spinnerTask;
    private int _currentFrame = 0;

    public ConsoleSpinner(string message = "Loading")
    {
        _message = message;
        _spinnerTask = Task.Run(async () => await SpinAsync(_cts.Token));
    }

    public async Task SpinAsync(CancellationToken cancellationToken)
    {
        Console.CursorVisible = false;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write($"\r{_frames[_currentFrame]} {_message}...");
            _currentFrame = (_currentFrame + 1) % _frames.Length;
            
            try
            {
                await Task.Delay(100, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    public void Stop(string completionMessage = "", bool success = true)
    {
        _cts.Cancel();
        _spinnerTask.Wait();
        
        ConsoleHelper.ClearLine();
        
        if (!string.IsNullOrEmpty(completionMessage))
        {
            if (success)
                ConsoleHelper.WriteSuccess(completionMessage);
            else
                ConsoleHelper.WriteError(completionMessage);
        }
        
        Console.CursorVisible = true;
    }

    public void Dispose()
    {
        if (!_cts.IsCancellationRequested)
        {
            Stop();
        }
        _cts.Dispose();
    }
}