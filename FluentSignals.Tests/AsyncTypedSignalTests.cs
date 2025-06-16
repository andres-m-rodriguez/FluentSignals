using FluentAssertions;
using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;
using FluentSignals.Blazor.Extensions;
using Xunit;

namespace FluentSignals.Tests;

public class AsyncTypedSignalTests
{
    [Fact]
    public async Task AsyncTypedSignal_Should_Track_Loading_State()
    {
        // Arrange
        var signal = new AsyncTypedSignal<string>("initial");
        var loadingStates = new List<bool>();
        
        using var subscription = signal.IsLoading.SubscribeDisposable(isLoading => 
            loadingStates.Add(isLoading));
        
        // Act
        await signal.LoadAsync(async () =>
        {
            await Task.Delay(50);
            return "loaded";
        });
        
        // Assert
        loadingStates.Should().Contain(true); // Was loading
        loadingStates.Should().Contain(false); // Finished loading
        signal.IsLoading.Value.Should().BeFalse();
        signal.Value.Should().Be("loaded");
    }
    
    [Fact]
    public async Task AsyncTypedSignal_Should_Track_Errors()
    {
        // Arrange
        var signal = new AsyncTypedSignal<int>(0);
        var errors = new List<Exception?>();
        
        using var subscription = signal.Error.SubscribeDisposable(error => 
            errors.Add(error));
        
        // Act
        await signal.LoadAsync(async () =>
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
    public async Task AsyncTypedSignal_Should_Clear_Error_On_Successful_Load()
    {
        // Arrange
        var signal = new AsyncTypedSignal<string>("initial");
        
        // First, cause an error
        await signal.LoadAsync(async () =>
        {
            await Task.Delay(10);
            throw new Exception("Error");
        });
        
        signal.Error.Value.Should().NotBeNull();
        
        // Act - Load successfully
        await signal.LoadAsync(async () =>
        {
            await Task.Delay(10);
            return "success";
        });
        
        // Assert
        signal.Error.Value.Should().BeNull();
        signal.Value.Should().Be("success");
    }
    
    [Fact]
    public async Task AsyncTypedSignal_Should_Preserve_Value_On_Error()
    {
        // Arrange
        var signal = new AsyncTypedSignal<string>("initial");
        
        // First successful load
        await signal.LoadAsync(async () =>
        {
            await Task.Delay(10);
            return "success value";
        });
        
        // Act - Try to load with error
        await signal.LoadAsync(async () =>
        {
            await Task.Delay(10);
            throw new Exception("Load failed");
        });
        
        // Assert
        signal.Value.Should().Be("success value"); // Value should be preserved
        signal.Error.Value.Should().NotBeNull();
    }
    
    [Fact]
    public async Task AsyncTypedSignal_Should_Support_Concurrent_Loads()
    {
        // Arrange
        var signal = new AsyncTypedSignal<int>(0);
        var loadCount = 0;
        
        // Act - Start multiple loads concurrently
        var tasks = Enumerable.Range(1, 5).Select(i => 
            signal.LoadAsync(async () =>
            {
                Interlocked.Increment(ref loadCount);
                await Task.Delay(10);
                return i * 10;
            })
        ).ToArray();
        
        await Task.WhenAll(tasks);
        
        // Assert
        loadCount.Should().Be(5); // All loads should execute
        signal.Value.Should().BeOneOf(10, 20, 30, 40, 50); // Last one wins
        signal.IsLoading.Value.Should().BeFalse();
    }
    
    [Fact]
    public async Task AsyncTypedSignal_Should_Notify_Value_Changes()
    {
        // Arrange
        var signal = new AsyncTypedSignal<int>(0);
        var values = new List<int>();
        
        using var subscription = signal.SubscribeDisposable(value => values.Add(value));
        
        // Act
        await signal.LoadAsync(async () =>
        {
            await Task.Delay(10);
            return 10;
        });
        
        await signal.LoadAsync(async () =>
        {
            await Task.Delay(10);
            return 20;
        });
        
        // Assert
        values.Should().Equal(10, 20);
    }
    
    [Fact]
    public void AsyncTypedSignal_Should_Implement_ICompositeSignal()
    {
        // Arrange
        var signal = new AsyncTypedSignal<string>("test");
        
        // Act
        var internalSignals = signal.GetInternalSignals().ToList();
        
        // Assert
        signal.Should().BeAssignableTo<ICompositeSignal>();
        internalSignals.Should().HaveCount(3);
        internalSignals.Should().Contain(signal.IsLoading);
        internalSignals.Should().Contain(signal.Error);
        internalSignals.Should().Contain(signal); // The signal itself
    }
    
    [Fact]
    public async Task AsyncTypedSignal_Should_Handle_Null_Results()
    {
        // Arrange
        var signal = new AsyncTypedSignal<string?>("initial");
        
        // Act
        await signal.LoadAsync(async () =>
        {
            await Task.Delay(10);
            return null;
        });
        
        // Assert
        signal.Value.Should().BeNull();
        signal.Error.Value.Should().BeNull();
        signal.IsLoading.Value.Should().BeFalse();
    }
    
    [Fact]
    public async Task AsyncTypedSignal_Should_Support_Complex_Types()
    {
        // Arrange
        var signal = new AsyncTypedSignal<UserData?>(null);
        var userData = new UserData 
        { 
            Id = 1, 
            Name = "John", 
            Tags = new[] { "admin", "user" } 
        };
        
        // Act
        await signal.LoadAsync(async () =>
        {
            await Task.Delay(10);
            return userData;
        });
        
        // Assert
        signal.Value.Should().NotBeNull();
        signal.Value!.Id.Should().Be(1);
        signal.Value.Name.Should().Be("John");
        signal.Value.Tags.Should().Equal("admin", "user");
    }
    
    [Fact]
    public async Task AsyncTypedSignal_LoadAsync_Should_Return_Task()
    {
        // Arrange
        var signal = new AsyncTypedSignal<int>(0);
        
        // Act
        var task = signal.LoadAsync(async () =>
        {
            await Task.Delay(10);
            return 42;
        });
        
        // Assert
        task.Should().NotBeNull();
        task.Should().BeAssignableTo<Task>();
        await task; // Should complete without throwing
        signal.Value.Should().Be(42);
    }
    
    // Test model
    private class UserData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}