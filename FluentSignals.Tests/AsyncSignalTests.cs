using FluentAssertions;
using FluentSignals.Implementations.Core;
using FluentSignals.Blazor.Extensions;
using Xunit;

namespace FluentSignals.Tests;

public class AsyncSignalTests
{
    [Fact]
    public async Task AsyncSignal_Should_Track_Loading_State()
    {
        // Arrange
        var signal = new AsyncSignal();
        var loadingStates = new List<bool>();
        
        using var subscription = signal.IsLoading.SubscribeDisposable(isLoading => 
            loadingStates.Add(isLoading));
        
        // Act
        await signal.RunAsync(async () =>
        {
            await Task.Delay(50);
        });
        
        // Assert
        loadingStates.Should().Contain(true); // Was loading
        loadingStates.Should().Contain(false); // Finished loading
        signal.IsLoading.Value.Should().BeFalse();
    }
    
    [Fact]
    public async Task AsyncSignal_Should_Track_Errors()
    {
        // Arrange
        var signal = new AsyncSignal();
        var errors = new List<Exception?>();
        
        using var subscription = signal.Error.SubscribeDisposable(error => 
            errors.Add(error));
        
        // Act
        await signal.RunAsync(async () =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Test error");
        });
        
        // Assert
        signal.Error.Value.Should().NotBeNull();
        signal.Error.Value!.Message.Should().Be("Test error");
        errors.Should().NotBeEmpty();
        errors.Last().Should().BeOfType<InvalidOperationException>();
    }
    
    [Fact]
    public async Task AsyncSignal_Should_Clear_Error_On_Successful_Run()
    {
        // Arrange
        var signal = new AsyncSignal();
        
        // First, cause an error
        await signal.RunAsync(async () =>
        {
            await Task.Delay(10);
            throw new Exception("Error");
        });
        
        signal.Error.Value.Should().NotBeNull();
        
        // Act - Run successfully
        await signal.RunAsync(async () =>
        {
            await Task.Delay(10);
        });
        
        // Assert
        signal.Error.Value.Should().BeNull();
    }
    
    [Fact]
    public async Task AsyncSignal_Should_Handle_Synchronous_Exceptions()
    {
        // Arrange
        var signal = new AsyncSignal();
        
        // Act
        await signal.RunAsync(() =>
        {
            throw new ArgumentException("Sync error");
        });
        
        // Assert
        signal.Error.Value.Should().NotBeNull();
        signal.Error.Value.Should().BeOfType<ArgumentException>();
        signal.Error.Value!.Message.Should().Be("Sync error");
    }
    
    [Fact]
    public async Task AsyncSignal_Should_Set_Loading_False_Even_On_Error()
    {
        // Arrange
        var signal = new AsyncSignal();
        
        // Act
        await signal.RunAsync(async () =>
        {
            await Task.Delay(10);
            throw new Exception("Error during load");
        });
        
        // Assert
        signal.IsLoading.Value.Should().BeFalse();
        signal.Error.Value.Should().NotBeNull();
    }
    
    [Fact]
    public async Task AsyncSignal_Should_Notify_On_State_Changes()
    {
        // Arrange
        var signal = new AsyncSignal();
        var notifications = 0;
        
        using var subscription = signal.SubscribeDisposable(() => notifications++);
        
        // Act
        await signal.RunAsync(async () =>
        {
            await Task.Delay(10);
            signal.Notify(); // Manual notification during async operation
        });
        
        // Assert
        notifications.Should().Be(1);
    }
    
    [Fact]
    public async Task AsyncSignal_Should_Handle_Multiple_Runs()
    {
        // Arrange
        var signal = new AsyncSignal();
        var runCount = 0;
        
        // Act
        await signal.RunAsync(async () =>
        {
            runCount++;
            await Task.Delay(10);
        });
        
        await signal.RunAsync(async () =>
        {
            runCount++;
            await Task.Delay(10);
        });
        
        await signal.RunAsync(async () =>
        {
            runCount++;
            await Task.Delay(10);
        });
        
        // Assert
        runCount.Should().Be(3);
        signal.IsLoading.Value.Should().BeFalse();
        signal.Error.Value.Should().BeNull();
    }
    
    [Fact]
    public async Task AsyncSignal_Should_Handle_Nested_Async_Operations()
    {
        // Arrange
        var signal = new AsyncSignal();
        var innerSignal = new AsyncSignal();
        var outerCompleted = false;
        var innerCompleted = false;
        
        // Act
        await signal.RunAsync(async () =>
        {
            await Task.Delay(10);
            
            await innerSignal.RunAsync(async () =>
            {
                await Task.Delay(10);
                innerCompleted = true;
            });
            
            outerCompleted = true;
        });
        
        // Assert
        outerCompleted.Should().BeTrue();
        innerCompleted.Should().BeTrue();
        signal.IsLoading.Value.Should().BeFalse();
        innerSignal.IsLoading.Value.Should().BeFalse();
    }
    
    [Fact]
    public void AsyncSignal_Dispose_Should_Dispose_Internal_Signals()
    {
        // Arrange
        var signal = new AsyncSignal();
        var loadingNotifications = 0;
        var errorNotifications = 0;
        
        signal.IsLoading.SubscribeDisposable(() => loadingNotifications++);
        signal.Error.SubscribeDisposable(() => errorNotifications++);
        
        // Act
        signal.Dispose();
        
        // Try to modify after disposal (should not notify)
        var isLoadingSignal = signal.IsLoading as TypedSignal<bool>;
        var errorSignal = signal.Error as TypedSignal<Exception?>;
        
        if (isLoadingSignal != null && errorSignal != null)
        {
            isLoadingSignal.Value = true;
            errorSignal.Value = new Exception("Should not notify");
        }
        
        // Assert
        loadingNotifications.Should().Be(0);
        errorNotifications.Should().Be(0);
    }
    
    [Fact]
    public async Task AsyncSignal_Should_Work_With_Complex_Async_Operations()
    {
        // Arrange
        var signal = new AsyncSignal();
        var results = new List<int>();
        
        // Act
        await signal.RunAsync(async () =>
        {
            var tasks = Enumerable.Range(1, 5).Select(async i =>
            {
                await Task.Delay(10 * i);
                return i * i;
            });
            
            var squares = await Task.WhenAll(tasks);
            results.AddRange(squares);
        });
        
        // Assert
        results.Should().Equal(1, 4, 9, 16, 25);
        signal.IsLoading.Value.Should().BeFalse();
        signal.Error.Value.Should().BeNull();
    }
}