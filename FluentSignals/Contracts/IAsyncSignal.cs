namespace FluentSignals.Contracts;

public interface IAsyncSignal : ISignal
{
    ISignal<bool> IsLoading { get; }
    ISignal<Exception?> Error { get; }

    Task RunAsync(Func<Task> action);
}

public interface IAsyncSignal<T> : ISignal<T>
{
    ISignal<bool> IsLoading { get; }
    ISignal<Exception?> Error { get; }

    Task LoadAsync(Func<Task<T>> loader);
}
