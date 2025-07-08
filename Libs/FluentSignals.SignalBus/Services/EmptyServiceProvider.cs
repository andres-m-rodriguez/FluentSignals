namespace FluentSignals.SignalBus.Services;

/// <summary>
/// Empty service provider for when no DI container is available
/// </summary>
internal class EmptyServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}