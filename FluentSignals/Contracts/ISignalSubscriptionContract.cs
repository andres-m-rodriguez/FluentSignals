namespace FluentSignals.Contracts;

public interface ISignalSubscriptionContract : IDisposable
{
    public Guid SubscriptionId { get; }
};