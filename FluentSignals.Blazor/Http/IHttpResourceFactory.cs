using FluentSignals.Options.HttpResource;
using System.Net.Http;

namespace FluentSignals.Blazor.Http;

public interface IHttpResourceFactory
{
    HttpResource Create();
    HttpResource Create(HttpClient httpClient);
    HttpResource CreateWithBaseUrl(string baseUrl);
    HttpResource CreateWithOptions(Action<HttpResourceOptions> configure);
}