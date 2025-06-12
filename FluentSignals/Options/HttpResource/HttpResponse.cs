using System.Net;
using System.Net.Http.Headers;

namespace FluentSignals.Options.HttpResource;

public class HttpResponse
{
    public HttpStatusCode StatusCode { get; }
    public HttpResponseHeaders Headers { get; }
    public string Content { get; }

    public HttpResponse(HttpStatusCode statusCode, HttpResponseHeaders headers, string content)
    {
        StatusCode = statusCode;
        Headers = headers;
        Content = content;
    }
}

public class HttpResponse<T> : HttpResponse
{
    public T? Data { get; }

    public HttpResponse(HttpStatusCode statusCode, HttpResponseHeaders headers, string content, T? data)
        : base(statusCode, headers, content)
    {
        Data = data;
    }
}