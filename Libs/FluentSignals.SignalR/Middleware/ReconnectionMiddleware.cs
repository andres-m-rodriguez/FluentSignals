using FluentSignals.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Client;

namespace FluentSignals.SignalR.Middleware;

/// <summary>
/// Middleware that handles reconnection logic
/// </summary>
public class ReconnectionMiddleware : ISignalRMiddleware
{
    private DateTime? _lastMessageTime;
    private readonly TimeSpan _messageTimeout;

    public ReconnectionMiddleware(TimeSpan? messageTimeout = null)
    {
        _messageTimeout = messageTimeout ?? TimeSpan.FromMinutes(5);
    }

    public async Task InvokeAsync(
        object message,
        SignalRContext context,
        SignalRMessageHandler next,
        CancellationToken cancellationToken)
    {
        // Update last message time
        _lastMessageTime = DateTime.UtcNow;
        
        // Store in context for other middleware to use
        context.Items["LastMessageTime"] = _lastMessageTime;
        context.Items["IsConnectionHealthy"] = true;

        await next(message, context, cancellationToken);
    }

    /// <summary>
    /// Checks if the connection is healthy based on last message time
    /// </summary>
    public bool IsConnectionHealthy()
    {
        if (!_lastMessageTime.HasValue)
            return false;

        return DateTime.UtcNow - _lastMessageTime.Value < _messageTimeout;
    }
}