namespace FluentSignals.SignalBus;

/// <summary>
/// Implementation of ISignalPublisher that publishes messages through the enhanced signal bus
/// </summary>
internal sealed class SignalPublisher : ISignalPublisher
{
    private readonly ISignalBus _signalBus;

    public SignalPublisher(ISignalBus signalBus)
    {
        _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
    }

    public void Publish<T>(T message) where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        // Use synchronous publish by blocking on async method
        // This maintains backward compatibility while leveraging enhanced features
        Task.Run(async () => await _signalBus.PublishAsync(message)).GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(T message) where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        // Use the enhanced async publish method with all features
        await _signalBus.PublishAsync(message);
    }
}