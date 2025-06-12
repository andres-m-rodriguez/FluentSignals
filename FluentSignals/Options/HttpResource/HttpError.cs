namespace FluentSignals.Options.HttpResource;

public class HttpError : Exception
{
    public string Code { get; }

    public HttpError(string message, string code, Exception? innerException = null) 
        : base(message, innerException)
    {
        Code = code;
    }
}