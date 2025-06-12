namespace FluentSignals.Contracts;

public interface IResource : IAsyncSignal
{
    Task RefreshAsync();
}

public interface IResource<T> : IAsyncSignal<T>, IResource
{
}