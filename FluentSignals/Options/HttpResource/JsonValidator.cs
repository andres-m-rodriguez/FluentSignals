using System.Text.Json;

namespace FluentSignals.Options.HttpResource;

internal static class JsonValidator
{
    /// <summary>
    /// Validates if the JSON structure matches the expected type by checking if any properties were actually deserialized.
    /// </summary>
    public static bool IsValidDeserialization(string json, Type targetType, object? deserializedObject)
    {
        if (deserializedObject == null || string.IsNullOrEmpty(json))
            return false;
            
        try
        {
            // Parse the JSON to get property names
            using var doc = JsonDocument.Parse(json);
            var jsonProperties = doc.RootElement.EnumerateObject()
                .Select(p => p.Name.ToLowerInvariant())
                .ToHashSet();
                
            if (jsonProperties.Count == 0)
                return false;
                
            // Get target type properties
            var typeProperties = targetType.GetProperties()
                .Select(p => p.Name.ToLowerInvariant())
                .ToHashSet();
                
            // Check if at least one JSON property matches a type property
            return jsonProperties.Any(jp => typeProperties.Contains(jp));
        }
        catch
        {
            return false;
        }
    }
}