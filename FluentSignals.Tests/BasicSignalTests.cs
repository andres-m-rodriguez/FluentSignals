using FluentAssertions;
using FluentSignals.Implementations.Core;
using FluentSignals.Blazor.Extensions;
using Xunit;

namespace FluentSignals.Tests;

public class BasicSignalTests
{
    [Fact]
    public void Signal_Should_Notify_Subscribers_On_Change()
    {
        // Arrange
        var signal = new Signal();
        var notificationCount = 0;
        
        using var subscription = signal.SubscribeDisposable(() => notificationCount++);
        
        // Act
        signal.Notify();
        signal.Notify();
        
        // Assert
        notificationCount.Should().Be(2);
    }
    
    [Fact]
    public void Signal_Should_Support_Multiple_Subscribers()
    {
        // Arrange
        var signal = new Signal();
        var subscriber1Count = 0;
        var subscriber2Count = 0;
        var subscriber3Count = 0;
        
        using var sub1 = signal.SubscribeDisposable(() => subscriber1Count++);
        using var sub2 = signal.SubscribeDisposable(() => subscriber2Count++);
        using var sub3 = signal.SubscribeDisposable(() => subscriber3Count++);
        
        // Act
        signal.Notify();
        
        // Assert
        subscriber1Count.Should().Be(1);
        subscriber2Count.Should().Be(1);
        subscriber3Count.Should().Be(1);
    }
    
    [Fact]
    public void Signal_Should_Not_Notify_Disposed_Subscribers()
    {
        // Arrange
        var signal = new Signal();
        var count1 = 0;
        var count2 = 0;
        
        var sub1 = signal.SubscribeDisposable(() => count1++);
        using var sub2 = signal.SubscribeDisposable(() => count2++);
        
        // Act
        signal.Notify(); // Both should be notified
        sub1.Dispose();
        signal.Notify(); // Only sub2 should be notified
        
        // Assert
        count1.Should().Be(1);
        count2.Should().Be(2);
    }
    
    [Fact]
    public void Signal_Unsubscribe_Should_Remove_Subscription()
    {
        // Arrange
        var signal = new Signal();
        var count = 0;
        
        var subscription = signal.Subscribe(() => count++);
        
        // Act
        signal.Notify();
        signal.Unsubscribe(subscription.SubscriptionId);
        signal.Notify();
        
        // Assert
        count.Should().Be(1);
    }
    
    [Fact]
    public void Signal_Should_Propagate_Exception_From_Subscriber()
    {
        // Arrange
        var signal = new Signal();
        var goodSubscriberCount = 0;
        
        using var badSub = signal.SubscribeDisposable(() => throw new Exception("Bad subscriber"));
        using var goodSub = signal.SubscribeDisposable(() => goodSubscriberCount++);
        
        // Act & Assert
        var act = () => signal.Notify();
        act.Should().Throw<Exception>().WithMessage("Bad subscriber");
        
        // The good subscriber might not be called if exception stops propagation
        // This depends on the implementation order
    }
    
    [Fact]
    public void Signal_Dispose_Should_Clear_All_Subscribers()
    {
        // Arrange
        var signal = new Signal();
        var count = 0;
        
        signal.Subscribe(() => count++);
        signal.Subscribe(() => count++);
        
        // Act
        signal.Dispose();
        signal.Notify();
        
        // Assert
        count.Should().Be(0);
        signal.Subscribers.Should().BeEmpty();
    }
}