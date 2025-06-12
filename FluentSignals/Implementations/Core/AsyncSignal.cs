using FluentSignals.Contracts;

namespace FluentSignals.Implementations.Core;

public class AsyncSignal : Signal, IAsyncSignal
{
    public ISignal<bool> IsLoading { get; } = new TypedSignal<bool>(false);
    public ISignal<Exception?> Error { get; } = new TypedSignal<Exception?>(default);

    public async Task RunAsync(Func<Task> action)
    {
        IsLoading.Value = true;
        Error.Value = null;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Error.Value = ex;
        }
        finally
        {
            IsLoading.Value = false;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        IsLoading.Dispose();
        Error.Dispose();
    }
}
