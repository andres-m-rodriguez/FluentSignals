namespace FluentSignals.SignalBus.Middleware.BuiltIn;

/// <summary>
/// Middleware that validates messages before publishing
/// </summary>
public class ValidationMiddleware : ISignalBusMiddleware
{
    private readonly Dictionary<Type, Func<object, bool>> _validators = new();

    public ValidationMiddleware()
    {
    }

    /// <summary>
    /// Register a validator for a message type
    /// </summary>
    public ValidationMiddleware RegisterValidator<T>(Func<T, bool> validator) where T : class
    {
        _validators[typeof(T)] = obj => validator((T)obj);
        return this;
    }

    public async Task InvokeAsync(SignalBusContext context, SignalBusDelegate next)
    {
        if (_validators.TryGetValue(context.MessageType, out var validator))
        {
            if (!validator(context.Message))
            {
                context.IsCancelled = true;
                context.Items["ValidationFailed"] = true;
                return;
            }
        }

        await next(context);
    }
}