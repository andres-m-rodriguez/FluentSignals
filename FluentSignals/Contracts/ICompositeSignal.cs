using System.Collections.Generic;

namespace FluentSignals.Contracts
{
    /// <summary>
    /// Defines a contract for signals that contain other internal signals.
    /// This allows composite signals to expose their internal signals for subscription.
    /// </summary>
    public interface ICompositeSignal
    {
        /// <summary>
        /// Gets all internal signals that should be subscribed to when this composite signal is used.
        /// This enables automatic subscription management for complex signal types.
        /// </summary>
        /// <returns>A collection of internal signals that make up this composite signal.</returns>
        IEnumerable<ISignal> GetInternalSignals();
    }
}