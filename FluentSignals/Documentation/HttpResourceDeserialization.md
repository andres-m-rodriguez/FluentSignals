# HTTP Resource JSON Deserialization Guide

This guide explains how to handle JSON deserialization issues with custom types when using HttpResource handlers.

## The Problem

When using typed error handlers with HttpResource, you may encounter deserialization errors if your types don't support System.Text.Json's default deserialization. This commonly occurs with:

- Types using static readonly patterns
- Types without parameterless constructors
- Immutable types without proper JSON constructor attributes

### Example Problem

```csharp
// This type will cause deserialization errors
public class ConflictType
{
    public static readonly ConflictType Username = new("Username already exists");
    public static readonly ConflictType Email = new("Email already exists");
    
    private ConflictType(string message)
    {
        Message = message;
    }
    
    public string Message { get; }
}

// Error response that uses ConflictType
public class UserAlreadyExistsError
{
    public ConflictType ConflictType { get; set; }
}
```

## Solutions

### Solution 1: Add JSON Constructor Support

Modify your type to support JSON deserialization by adding a JsonConstructor:

```csharp
public class ConflictType
{
    public static readonly ConflictType Username = new("Username already exists");
    public static readonly ConflictType Email = new("Email already exists");
    
    [JsonConstructor]
    private ConflictType(string message)
    {
        Message = message;
    }
    
    public string Message { get; }
    
    public override string ToString() => Message;
}
```

Note: The `[JsonConstructor]` attribute on the private constructor tells System.Text.Json to use it for deserialization. This preserves the encapsulation while allowing JSON deserialization to work.

### Solution 2: Use a DTO for API Responses

Create separate DTOs specifically for API communication:

```csharp
// DTO for API responses
public class UserAlreadyExistsErrorDto
{
    public string ConflictType { get; set; }
    public string Message { get; set; }
}

// Usage in HttpResource
_registrationResource
    .OnConflict<UserAlreadyExistsErrorDto>(async error =>
    {
        var conflictMessage = error.ConflictType switch
        {
            "Username" => "Username already exists",
            "Email" => "Email already exists",
            _ => error.Message
        };
        
        // Handle the error
    });
```

### Solution 3: Custom JsonConverter

Implement a custom JsonConverter for complex types:

```csharp
public class ConflictTypeConverter : JsonConverter<ConflictType>
{
    public override ConflictType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("message", out var messageProp) || 
                root.TryGetProperty("Message", out messageProp))
            {
                var message = messageProp.GetString();
                
                if (message == ConflictType.Username.Message)
                    return ConflictType.Username;
                if (message == ConflictType.Email.Message)
                    return ConflictType.Email;
                
                // Return a default or throw
                return ConflictType.Username;
            }
        }
        
        throw new JsonException("Unable to deserialize ConflictType");
    }
    
    public override void Write(Utf8JsonWriter writer, ConflictType value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("message", value.Message);
        writer.WriteEndObject();
    }
}

// Register the converter in Program.cs
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new ConflictTypeConverter() }
};

// Configure HttpResource to use custom JSON options
services.AddFluentSignalsBlazor(options =>
{
    options.JsonSerializerOptions = jsonOptions;
    options.BaseUrl = "https://api.example.com/";
});
```

### Solution 4: Simplify Error Response Structure

The simplest solution is often to use primitive types in your API responses:

```csharp
// API Controller
[HttpPost("register")]
public async Task<IActionResult> Register(RegisterRequest request)
{
    var result = await _authService.RegisterAsync(request);
    
    if (!result.Success)
    {
        // Return simple error structure
        return Conflict(new 
        { 
            error = "UserAlreadyExists",
            field = result.ConflictField, // "username" or "email"
            message = result.ErrorMessage
        });
    }
    
    return Ok(result.User);
}

// Client-side handling
_registrationResource
    .OnConflict<dynamic>(async error =>
    {
        var errorObj = error as JsonElement?;
        if (errorObj?.TryGetProperty("field", out var field) == true)
        {
            var fieldValue = field.GetString();
            if (fieldValue == "username")
                _usernameError.Value = "Username already taken";
            else if (fieldValue == "email")
                _emailError.Value = "Email already registered";
        }
    });
```

## Best Practices

1. **Keep API DTOs Simple**: Use primitive types and simple objects for API communication
2. **Separate Domain Models from DTOs**: Don't expose complex domain models directly through APIs
3. **Document Error Formats**: Clearly document the structure of error responses
4. **Use Consistent Error Structures**: Maintain a consistent error response format across your API

### Example: Complete Registration Flow

```csharp
// Shared error response DTO
public class ApiError
{
    public string Code { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Details { get; set; }
}

// Registration component
@code {
    private HttpResource _registrationResource;
    
    protected override void OnInitialized()
    {
        _registrationResource = ResourceFactory.Create();
        
        _registrationResource
            .OnSuccess<AuthResponse>(async response =>
            {
                // Store token, navigate to dashboard
                await _authService.LoginAsync(response.Token);
                Navigation.NavigateTo("/dashboard");
            })
            .OnBadRequest<ApiError>(async error =>
            {
                // Handle validation errors
                if (error.Details?.ContainsKey("Username") == true)
                    _usernameError.Value = error.Details["Username"];
                if (error.Details?.ContainsKey("Email") == true)
                    _emailError.Value = error.Details["Email"];
            })
            .OnConflict<ApiError>(async error =>
            {
                // Handle conflicts
                if (error.Code == "USERNAME_EXISTS")
                    _usernameError.Value = error.Message;
                else if (error.Code == "EMAIL_EXISTS")
                    _emailError.Value = error.Message;
            });
    }
    
    private async Task RegisterAsync()
    {
        var request = new RegisterRequest
        {
            Username = _username.Value,
            Email = _email.Value,
            Password = _password.Value
        };
        
        await _registrationResource.PostAsync("auth/register", request);
    }
}
```

## Troubleshooting

### Common Issues

1. **"Deserialization of types without a parameterless constructor..."**
   - Add a parameterless constructor or use JsonConstructor
   - Use DTOs instead of complex domain objects

2. **"The JSON value could not be converted to..."**
   - Ensure JSON structure matches your type
   - Check property name casing (use JsonPropertyName if needed)
   - Verify nullable reference types match JSON

3. **Handler not being called**
   - Verify the response content type is "application/json"
   - Check that the JSON structure matches your expected type
   - Use dynamic or JsonElement for debugging

### Debugging Tips

```csharp
// Debug handler to inspect raw response
_resource.OnConflict(async response =>
{
    var content = response.Content;
    Console.WriteLine($"Raw response: {content}");
    
    // Try to parse as JsonElement for inspection
    try
    {
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Console.WriteLine($"Parsed JSON: {json}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Parse error: {ex.Message}");
    }
});
```