namespace FluentSignals.SignalR;

/// <summary>
/// Attribute to specify the SignalR hub method name for a type
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class SignalRMethodAttribute : Attribute
{
    /// <summary>
    /// The SignalR hub method name
    /// </summary>
    public string MethodName { get; }

    public SignalRMethodAttribute(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        }
        
        MethodName = methodName;
    }
}