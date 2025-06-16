using FluentAssertions;
using FluentSignals.Contracts;
using FluentSignals.Implementations.Core;
using FluentSignals.Implementations.Subscriptions;
using FluentSignals.Blazor.Extensions;
using Xunit;

namespace FluentSignals.Tests;

public class SignalSubscriptionTests
{
    [Fact]
    public void SignalSubscription_Should_Have_Unique_Id()
    {
        // Arrange & Act
        var sub1 = new SignalSubscription(Guid.NewGuid(), () => { });
        var sub2 = new SignalSubscription(Guid.NewGuid(), () => { });
        
        // Assert
        sub1.SubscriptionId.Should().NotBe(sub2.SubscriptionId);
    }
    
    [Fact]
    public void TypedSignalSubscription_Should_Have_Unique_Id()
    {
        // Arrange & Act
        var sub1 = new TypedSignalSubscription<int>(Guid.NewGuid(), _ => { });
        var sub2 = new TypedSignalSubscription<int>(Guid.NewGuid(), _ => { });
        
        // Assert
        sub1.SubscriptionId.Should().NotBe(sub2.SubscriptionId);
    }
    
    [Fact]
    public void Subscription_Should_Be_Removed_On_Dispose()
    {
        // Arrange
        var signal = new TypedSignal<int>(0);
        var count = 0;
        
        // Act
        using (var subscription = signal.SubscribeDisposable(_ => count++))
        {
            signal.Value = 1;
            count.Should().Be(1);
        } // Dispose happens here
        
        signal.Value = 2;
        
        // Assert
        count.Should().Be(1); // Should not be called after dispose
    }
    
    [Fact]
    public void Multiple_Disposals_Should_Be_Safe()
    {
        // Arrange
        var signal = new Signal();
        var subscription = signal.SubscribeDisposable(() => { });
        
        // Act & Assert
        var act = () =>
        {
            subscription.Dispose();
            subscription.Dispose(); // Second disposal
            subscription.Dispose(); // Third disposal
        };
        
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Subscription_Should_Support_Nested_Signals()
    {
        // Arrange
        var outerSignal = new TypedSignal<int>(0);
        var innerSignal = new TypedSignal<string>("");
        var combinedValues = new List<(int, string)>();
        
        using var outerSub = outerSignal.SubscribeDisposable(outerValue =>
        {
            using var innerSub = innerSignal.SubscribeDisposable(innerValue =>
            {
                combinedValues.Add((outerValue, innerValue));
            });
            
            innerSignal.Value = $"inner-{outerValue}";
        });
        
        // Act
        outerSignal.Value = 1;
        outerSignal.Value = 2;
        
        // Assert
        combinedValues.Should().Equal(
            (1, "inner-1"),
            (2, "inner-2")
        );
    }
    
    [Fact]
    public void Subscription_Should_Handle_Recursive_Updates()
    {
        // Arrange
        var signal = new TypedSignal<int>(0);
        var values = new List<int>();
        var maxDepth = 3;
        
        using var subscription = signal.SubscribeDisposable(value =>
        {
            values.Add(value);
            if (value < maxDepth)
            {
                signal.Value = value + 1; // Recursive update
            }
        });
        
        // Act
        signal.Value = 1;
        
        // Assert
        values.Should().Equal(1, 2, 3);
    }
    
    [Fact]
    public void Subscription_Order_Should_Be_Maintained()
    {
        // Arrange
        var signal = new Signal();
        var order = new List<int>();
        
        using var sub1 = signal.SubscribeDisposable(() => order.Add(1));
        using var sub2 = signal.SubscribeDisposable(() => order.Add(2));
        using var sub3 = signal.SubscribeDisposable(() => order.Add(3));
        
        // Act
        signal.Notify();
        
        // Assert
        order.Should().Equal(1, 2, 3);
    }
    
    [Fact]
    public void Disposing_During_Notification_Should_Be_Safe()
    {
        // Arrange
        var signal = new Signal();
        IDisposable? sub2 = null;
        var notified1 = false;
        var notified2 = false;
        var notified3 = false;
        
        using var sub1 = signal.SubscribeDisposable(() =>
        {
            notified1 = true;
            sub2?.Dispose(); // Dispose sub2 during notification
        });
        
        sub2 = signal.SubscribeDisposable(() => notified2 = true);
        
        using var sub3 = signal.SubscribeDisposable(() => notified3 = true);
        
        // Act
        signal.Notify();
        
        // Assert
        notified1.Should().BeTrue();
        notified2.Should().BeTrue(); // Should still be called this time
        notified3.Should().BeTrue();
        
        // Reset and notify again
        notified1 = notified2 = notified3 = false;
        signal.Notify();
        
        notified1.Should().BeTrue();
        notified2.Should().BeFalse(); // Should not be called after disposal
        notified3.Should().BeTrue();
    }
    
    [Fact]
    public void Subscription_Should_Work_With_Value_Types()
    {
        // Arrange
        var signal = new TypedSignal<int>(0);
        var values = new List<int>();
        
        using var subscription = signal.SubscribeDisposable(v => values.Add(v));
        
        // Act
        for (int i = 1; i <= 5; i++)
        {
            signal.Value = i;
        }
        
        // Assert
        values.Should().Equal(1, 2, 3, 4, 5);
    }
    
    [Fact]
    public void Subscription_Should_Work_With_Reference_Types()
    {
        // Arrange
        var signal = new TypedSignal<List<string>>(new List<string>());
        var snapshots = new List<List<string>>();
        
        using var subscription = signal.SubscribeDisposable(list => 
            snapshots.Add(new List<string>(list))); // Create snapshot
        
        // Act
        signal.Value = new List<string> { "a" };
        signal.Value = new List<string> { "b", "c" };
        signal.Value = new List<string> { "d", "e", "f" };
        
        // Assert
        snapshots.Should().HaveCount(3);
        snapshots[0].Should().Equal("a");
        snapshots[1].Should().Equal("b", "c");
        snapshots[2].Should().Equal("d", "e", "f");
    }
    
    [Fact]
    public void Signal_To_Signal_Subscription_Should_Work()
    {
        // Arrange
        var source = new Signal();
        var target = new Signal();
        var targetNotified = false;
        
        using var targetSub = target.SubscribeDisposable(() => targetNotified = true);
        var sourceSub = source.Subscribe(target); // Subscribe target to source
        
        // Act
        source.Notify();
        
        // Assert
        targetNotified.Should().BeTrue();
    }
    
    [Fact]
    public void TypedSignal_Should_Support_Chained_Subscriptions()
    {
        // Arrange
        var signal1 = new TypedSignal<int>(0);
        var signal2 = new TypedSignal<int>(0);
        var signal3 = new TypedSignal<int>(0);
        
        using var sub1 = signal1.SubscribeDisposable(v => signal2.Value = v * 2);
        using var sub2 = signal2.SubscribeDisposable(v => signal3.Value = v + 10);
        
        // Act
        signal1.Value = 5;
        
        // Assert
        signal2.Value.Should().Be(10); // 5 * 2
        signal3.Value.Should().Be(20); // 10 + 10
    }
}