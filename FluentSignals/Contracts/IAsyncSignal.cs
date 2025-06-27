namespace FluentSignals.Contracts;

public interface IAsyncSignal : ISignal
{
    ISignal<bool> IsLoading { get; }
    ISignal<Exception?> Error { get; }
}

public interface IAsyncSignal<T> : ISignal
{
    ISignal<bool> IsLoading { get; }
    ISignal<Exception?> Error { get; }
    ISignal<T> SignalValue { get; } 
}
