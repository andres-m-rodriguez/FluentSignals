using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FluentSignals.Options.HttpResource;

public class HttpResponse
{
    public HttpStatusCode StatusCode { get; }
    public HttpResponseHeaders Headers { get; }
    public string Content { get; }
    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;

    public HttpResponse(HttpStatusCode statusCode, HttpResponseHeaders headers, string content)
    {
        StatusCode = statusCode;
        Headers = headers;
        Content = content;
    }
    
    public T? GetData<T>()
    {
        if (string.IsNullOrEmpty(Content))
            return default;
            
        try
        {
            return JsonSerializer.Deserialize<T>(Content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return default;
        }
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