namespace FluentSignals.SignalBus;

/// <summary>
/// Adapter that implements the public IPublisher interface by delegating to ISignalPublisher
/// </summary>
internal sealed class PublisherAdapter : IPublisher
{
    private readonly ISignalPublisher _signalPublisher;

    public PublisherAdapter(ISignalPublisher signalPublisher)
    {
        _signalPublisher = signalPublisher ?? throw new ArgumentNullException(nameof(signalPublisher));
    }

    public void Publish<T>(T message) where T : class
    {
        _signalPublisher.Publish(message);
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        // Note: ISignalPublisher.PublishAsync doesn't support cancellation token
        // but the enhanced SignalBus.PublishAsync does
        return _signalPublisher.PublishAsync(message);
    }
}