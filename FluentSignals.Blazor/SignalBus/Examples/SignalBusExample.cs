using FluentSignals.Blazor.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSignals.Blazor.SignalBus.Examples;

// Example message types
public class UserLoggedInMessage
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime LoggedInAt { get; set; }
}

public class NotificationMessage
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, error, success
}

// Example usage in a Blazor component
public class SignalBusExampleUsage
{
    private readonly ISignalPublisher _publisher;
    private readonly ISignalConsumer<UserLoggedInMessage> _userLoginConsumer;
    private readonly ISignalConsumer<NotificationMessage> _notificationConsumer;

    public SignalBusExampleUsage(
        ISignalPublisher publisher,
        ISignalConsumer<UserLoggedInMessage> userLoginConsumer,
        ISignalConsumer<NotificationMessage> notificationConsumer)
    {
        _publisher = publisher;
        _userLoginConsumer = userLoginConsumer;
        _notificationConsumer = notificationConsumer;
    }

    public void SetupSubscriptions()
    {
        // Subscribe to user login messages
        _userLoginConsumer.Subscribe(message =>
        {
            Console.WriteLine($"User {message.UserName} logged in at {message.LoggedInAt}");
        });

        // Subscribe to notification messages with async handler
        _notificationConsumer.Subscribe(async message =>
        {
            await Task.Delay(100); // Simulate async work
            Console.WriteLine($"[{message.Type.ToUpper()}] {message.Title}: {message.Content}");
        });
    }

    public void PublishUserLogin(string userId, string userName)
    {
        var message = new UserLoggedInMessage
        {
            UserId = userId,
            UserName = userName,
            LoggedInAt = DateTime.UtcNow
        };

        _publisher.Publish(message);
    }

    public async Task PublishNotificationAsync(string title, string content, string type = "info")
    {
        var message = new NotificationMessage
        {
            Title = title,
            Content = content,
            Type = type
        };

        await _publisher.PublishAsync(message);
    }
}

// Example registration in Program.cs
public static class SignalBusExampleRegistration
{
    public static void RegisterExample(IServiceCollection services)
    {
        // Add FluentSignals Blazor with SignalBus
        services.AddFluentSignalsBlazorWithSignalBus();
        
        // Register consumers for specific message types
        services.AddSignalConsumer<UserLoggedInMessage>();
        services.AddSignalConsumer<NotificationMessage>();
        
        // Or use the factory approach
        services.AddScoped(provider =>
        {
            var factory = provider.GetRequiredService<ISignalConsumerFactory>();
            return factory.CreateConsumer<UserLoggedInMessage>();
        });
    }
}