using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FluentSignals.Options.HttpResource;
using RichardSzalay.MockHttp;
using Xunit;

namespace FluentSignals.Tests;

public class HttpResourceJsonOptionsTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    
    public HttpResourceJsonOptionsTests()
    {
        _mockHttp = new MockHttpMessageHandler();
    }
    
    public void Dispose()
    {
        _mockHttp?.Dispose();
    }
    
    [Fact]
    public async Task HttpResource_Should_Use_Custom_JsonSerializerOptions()
    {
        // Arrange
        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new CustomDateTimeConverter() }
        };
        
        var httpClient = _mockHttp.ToHttpClient();
        var resourceOptions = new HttpResourceOptions
        {
            JsonSerializerOptions = customOptions
        };
        
        var resource = new HttpResource(httpClient, resourceOptions);
        
        // Response with custom date format
        var json = @"{""id"": 1, ""createdAt"": ""2024-01-15|14:30:00""}";
        _mockHttp.When("https://api.test.com/test")
            .Respond("application/json", json);
        
        // Act
        var response = await resource.GetAsync<TestModel>("https://api.test.com/test");
        
        // Assert
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(1);
        response.Data.CreatedAt.Should().Be(new DateTime(2024, 1, 15, 14, 30, 0));
    }
    
    [Fact]
    public async Task HttpResource_Should_Use_Custom_Converter_For_Complex_Types()
    {
        // Arrange
        var customOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new StatusTypeConverter() }
        };
        
        var httpClient = _mockHttp.ToHttpClient();
        var resourceOptions = new HttpResourceOptions
        {
            JsonSerializerOptions = customOptions
        };
        
        var resource = new HttpResource(httpClient, resourceOptions);
        
        // Response with static readonly pattern
        var json = @"{""message"": ""Item processed"", ""status"": ""Success""}";
        _mockHttp.When("https://api.test.com/process")
            .Respond("application/json", json);
        
        // Act
        var response = await resource.GetAsync<ProcessResult>("https://api.test.com/process");
        
        // Assert
        response.Data.Should().NotBeNull();
        response.Data!.Message.Should().Be("Item processed");
        response.Data.Status.Should().Be(StatusType.Success);
    }
    
    [Fact]
    public async Task TypedHandlers_Should_Use_Custom_JsonOptions()
    {
        // Arrange
        var customOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false, // Strict casing
            Converters = { new StatusTypeConverter() }
        };
        
        var httpClient = _mockHttp.ToHttpClient();
        var resourceOptions = new HttpResourceOptions
        {
            JsonSerializerOptions = customOptions
        };
        
        var resource = new HttpResource(httpClient, resourceOptions);
        
        var handlerCalled = false;
        ErrorWithStatus? capturedError = null;
        
        // Response with exact casing
        var json = @"{""Message"": ""Validation failed"", ""Status"": ""Error""}";
        _mockHttp.When("https://api.test.com/validate")
            .Respond(HttpStatusCode.BadRequest, "application/json", json);
        
        resource.OnBadRequest<ErrorWithStatus>(error =>
        {
            handlerCalled = true;
            capturedError = error;
            return Task.CompletedTask;
        });
        
        // Act
        await resource.PostAsync("https://api.test.com/validate", new { });
        
        // Assert
        handlerCalled.Should().BeTrue();
        capturedError.Should().NotBeNull();
        capturedError!.Message.Should().Be("Validation failed");
        capturedError.Status.Should().Be(StatusType.Error);
    }
    
    // Test models
    private class TestModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    private class ProcessResult
    {
        public string Message { get; set; } = string.Empty;
        public StatusType Status { get; set; } = StatusType.Pending;
    }
    
    private class ErrorWithStatus
    {
        public string Message { get; set; } = string.Empty;
        public StatusType Status { get; set; } = StatusType.Error;
    }
    
    // Static readonly pattern similar to user's ConflictType
    private class StatusType
    {
        public static readonly StatusType Pending = new("Pending");
        public static readonly StatusType Success = new("Success");
        public static readonly StatusType Error = new("Error");
        
        [JsonConstructor]
        private StatusType(string value)
        {
            Value = value;
        }
        
        public string Value { get; }
        
        public override string ToString() => Value;
    }
    
    // Custom converters
    private class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (dateString != null && DateTime.TryParseExact(dateString, "yyyy-MM-dd|HH:mm:ss", 
                null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return date;
            }
            throw new JsonException($"Unable to parse date: {dateString}");
        }
        
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd|HH:mm:ss"));
        }
    }
    
    private class StatusTypeConverter : JsonConverter<StatusType>
    {
        public override StatusType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return value switch
            {
                "Pending" => StatusType.Pending,
                "Success" => StatusType.Success,
                "Error" => StatusType.Error,
                _ => throw new JsonException($"Unknown status: {value}")
            };
        }
        
        public override void Write(Utf8JsonWriter writer, StatusType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}