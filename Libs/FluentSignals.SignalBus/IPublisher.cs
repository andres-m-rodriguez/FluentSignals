using System.Threading;
using System.Threading.Tasks;

namespace FluentSignals.SignalBus
{
    /// <summary>
    /// Public interface for publishing messages to the SignalBus
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Publishes a message synchronously
        /// </summary>
        /// <typeparam name="T">The type of message to publish</typeparam>
        /// <param name="message">The message to publish</param>
        void Publish<T>(T message) where T : class;

        /// <summary>
        /// Publishes a message asynchronously
        /// </summary>
        /// <typeparam name="T">The type of message to publish</typeparam>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    }
}