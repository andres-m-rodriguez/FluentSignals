using FluentSignals.Http.Contracts;

namespace FluentSignals.Http.Core;

public interface IHttpResourceMiddleware
{
    Task<HttpResponseMessage> InvokeAsync(
        HttpRequestMessage request,
        HttpResourceHandler next,
        CancellationToken cancellationToken
    );
}
