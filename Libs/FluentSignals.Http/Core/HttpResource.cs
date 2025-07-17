using System;
using System.Net.Http;
using System.Net.Http.Json;
using FluentSignals.Contracts;
using FluentSignals.Http.Contracts;
using FluentSignals.Http.Options;
using FluentSignals.Implementations.Core;
using FluentSignals.Implementations.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace FluentSignals.Http.Core;

public class HttpResource<T>(
    HttpClient client,
    Func<HttpRequestMessage> requestBuilder,
    IServiceProvider serviceProvider
) : IAsyncSignal<T>
{
    private readonly Queue<Func<HttpResourceHandler, HttpResourceHandler>> _middleware = [];
    private readonly List<ISignalSubscriptionContract> _externalSubscriptions = [];
    private Func<HttpRequestMessage> _requestBuilder = requestBuilder;

    public ISignal<T> SignalValue { get; } = new TypedSignal<T>(default!);
    public ISignal<bool> IsLoading { get; } = new TypedSignal<bool>(false);
    public ISignal<Exception?> Error { get; } = new TypedSignal<Exception?>(null);
    public ISignal<bool> IsDataAvaible { get; } = new TypedSignal<bool>(false);
    public List<ISignalSubscriptionContract> Subscribers { get; } = [];

    public T Value 
    { 
        get => SignalValue.Value; 
        set => SignalValue.Value = value; 
    }

    public HttpResource<T> Use(Func<HttpResourceHandler, HttpResourceHandler> middleware)
    {
        _middleware.Enqueue(middleware);
        return this;
    }

    public HttpResource<T> Use<TMiddleware>()
        where TMiddleware : IHttpResourceMiddleware
    {
        var middleware = serviceProvider.GetRequiredService<TMiddleware>();
        return Use(next =>
            (request, cancellationToken) => middleware.InvokeAsync(request, next, cancellationToken)
        );
    }

    public async Task LoadData(CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoading.Value = true;
            var request = _requestBuilder();
            var handler = BuildPipeline();
            var response = await handler(request, cancellationToken);

            // Special handling for HttpResponseMessage type
            if (typeof(T) == typeof(HttpResponseMessage))
            {
                SignalValue.Value = (T)(object)response;
            }
            else
            {
                var parsed = await response.Content.ReadFromJsonAsync<T>(
                    cancellationToken: cancellationToken
                );
                if (parsed is null)
                    throw new InvalidOperationException("The response was null.");

                SignalValue.Value = parsed;
            }
            
            IsDataAvaible.Value = true;
            Notify();
        }
        catch (Exception ex)
        {
            Error.Value = ex;
        }
        finally
        {
            IsLoading.Value = false;
        }
    }

    public ISignalSubscriptionContract Subscribe(ISignal subscriber)
    {
        var subscription = new SignalSubscription(Guid.NewGuid(), subscriber.Notify);
        Subscribers.Add(subscription);
        return subscription;
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        var subscriber = Subscribers.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
        if (subscriber is not null)
        {
            Subscribers.Remove(subscriber);
        }
    }

    public void Notify()
    {
        // Clean up dead weak references first
        var deadSubscribers = Subscribers
            .OfType<WeakSignalSubscription>()
            .Where(w => !w.IsAlive)
            .Cast<ISignalSubscriptionContract>()
            .ToList();
            
        foreach (var dead in deadSubscribers)
        {
            Subscribers.Remove(dead);
        }
        
        // Create a copy to avoid concurrent modification exceptions
        var subscribersCopy = Subscribers.ToList();
        
        foreach (var sub in subscribersCopy)
        {
            switch (sub)
            {
                case ConditionalSignalSubscription conditional:
                    if (conditional.Condition?.Invoke() == true)
                    {
                        conditional.Action?.Invoke();
                    }
                    break;
                case WeakSignalSubscription weak:
                    weak.Invoke();
                    break;
                case SignalSubscription regular:
                    regular.Action?.Invoke();
                    break;
            }
        }
    }

    public void Dispose()
    {
        foreach (var subscription in _externalSubscriptions)
        {
            subscription?.Dispose();
        }
        _externalSubscriptions.Clear();
        
        Subscribers.Clear();
        SignalValue.Dispose();
        IsLoading.Dispose();
        Error.Dispose();
        IsDataAvaible.Dispose();
    }

    public ISignalSubscriptionContract Subscribe(Action action)
    {
        var subscription = new SignalSubscription(Guid.NewGuid(), action);
        Subscribers.Add(subscription);
        return subscription;
    }
    
    public ISignalSubscriptionContract Subscribe(Action action, Func<bool> condition)
    {
        var subscription = new ConditionalSignalSubscription(Guid.NewGuid(), action, condition);
        Subscribers.Add(subscription);
        return subscription;
    }
    
    public ISignalSubscriptionContract SubscribeWeak(Action action)
    {
        var subscription = new WeakSignalSubscription(Guid.NewGuid(), action);
        Subscribers.Add(subscription);
        return subscription;
    }

    public ISignalSubscriptionContract Subscribe(Action<T> action)
    {
        var subscription = new TypedSignalSubscription<T>(Guid.NewGuid(), action);
        Subscribers.Add(subscription);
        SignalValue.Subscribe(action);
        return subscription;
    }

    public ISignalSubscriptionContract Subscribe(Action<T> action, Func<T, bool> condition)
    {
        var subscription = new ConditionalTypedSignalSubscription<T>(Guid.NewGuid(), action, condition);
        Subscribers.Add(subscription);
        SignalValue.Subscribe(action, condition);
        return subscription;
    }

    public ISignalSubscriptionContract SubscribeWeak(Action<T> action)
    {
        var subscription = new WeakTypedSignalSubscription<T>(Guid.NewGuid(), action);
        Subscribers.Add(subscription);
        SignalValue.SubscribeWeak(action);
        return subscription;
    }

    public HttpResource<T> SubscribeTo(ISignal signal)
    {
        var subscription = signal.Subscribe(() => _ = LoadData());
        _externalSubscriptions.Add(subscription);
        return this;
    }

    public HttpResource<T> SubscribeTo(params ISignal[] signals)
    {
        foreach (var signal in signals)
        {
            SubscribeTo(signal);
        }
        return this;
    }

    public HttpResource<T> WithRequestBuilder(Func<HttpRequestMessage> requestBuilder)
    {
        _requestBuilder = requestBuilder;
        return this;
    }

    public HttpResource<T> SubscribeWithDynamicRequest(Func<HttpRequestMessage> dynamicRequestBuilder, params ISignal[] signals)
    {
        _requestBuilder = dynamicRequestBuilder;
        
        foreach (var signal in signals)
        {
            var subscription = signal.Subscribe(() => _ = LoadData());
            _externalSubscriptions.Add(subscription);
        }
        
        return this;
    }

    private HttpResourceHandler BuildPipeline()
    {
        HttpResourceHandler final = (req, ct) => client.SendAsync(req, ct);

        foreach (var component in _middleware)
        {
            final = component(final);
        }

        return final;
    }
}
