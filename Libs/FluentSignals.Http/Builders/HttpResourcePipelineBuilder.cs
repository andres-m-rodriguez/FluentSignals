using FluentSignals.Http.Contracts;
using FluentSignals.Http.Core;

namespace FluentSignals.Http.Builders;

public class HttpResourcePipelineBuilder
{
    private readonly List<Func<HttpResourceHandler, HttpResourceHandler>> _components = [];

    public HttpResourcePipelineBuilder Use(
        Func<HttpResourceHandler, HttpResourceHandler> middleware
    )
    {
        _components.Add(middleware);
        return this;
    }

    public HttpResourcePipelineBuilder UseMiddleware<T>()
        where T : IHttpResourceMiddleware, new()
    {
        return Use(next =>
        {
            var instance = new T();
            return (req, ct) => instance.InvokeAsync(req, next, ct);
        });
    }

    public HttpResourceHandler Build(HttpClient client)
    {
        HttpResourceHandler final = (req, ct) => client.SendAsync(req, ct);

        foreach (var component in _components.AsEnumerable().Reverse())
        {
            final = component(final);
        }

        return final;
    }
}
