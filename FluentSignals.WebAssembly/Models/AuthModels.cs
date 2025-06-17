using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentSignals.WebAssembly.Models;

// Original ConflictType with JSON support added
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

// Solution 2: Use simpler DTOs for API communication
public class RegistrationErrorDto
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// Solution 3: Custom converter for ConflictType
public class ConflictTypeConverter : JsonConverter<ConflictType>
{
    public override ConflictType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            
            // Try to get message property (case-insensitive)
            if (root.TryGetProperty("message", out var messageProp) || 
                root.TryGetProperty("Message", out messageProp))
            {
                var message = messageProp.GetString();
                
                if (message == ConflictType.Username.Message)
                    return ConflictType.Username;
                if (message == ConflictType.Email.Message)
                    return ConflictType.Email;
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            // Handle if ConflictType is serialized as just a string
            var message = reader.GetString();
            if (message == ConflictType.Username.Message)
                return ConflictType.Username;
            if (message == ConflictType.Email.Message)
                return ConflictType.Email;
        }
        
        return null;
    }
    
    public override void Write(Utf8JsonWriter writer, ConflictType value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("message", value.Message);
        writer.WriteEndObject();
    }
}

// Example usage models
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// Error response that works with System.Text.Json
public class UserAlreadyExistsError
{
    // Use string instead of ConflictType for simpler deserialization
    public string ConflictType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    // Helper method to check conflict type
    public bool IsUsernameConflict => ConflictType == "Username";
    public bool IsEmailConflict => ConflictType == "Email";
}