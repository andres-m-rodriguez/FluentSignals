using FluentAssertions;
using FluentSignals.Implementations.Core;
using FluentSignals.Blazor.Extensions;
using Xunit;

namespace FluentSignals.Tests;

public class TypedSignalTests
{
    [Fact]
    public void TypedSignal_Should_Hold_Initial_Value()
    {
        // Arrange & Act
        var signal = new TypedSignal<int>(42);
        
        // Assert
        signal.Value.Should().Be(42);
    }
    
    [Fact]
    public void TypedSignal_Should_Notify_On_Value_Change()
    {
        // Arrange
        var signal = new TypedSignal<string>("initial");
        var notifications = new List<string>();
        
        using var subscription = signal.SubscribeDisposable(value => notifications.Add(value));
        
        // Act
        signal.Value = "changed";
        signal.Value = "changed again";
        
        // Assert
        notifications.Should().Equal("changed", "changed again");
    }
    
    [Fact]
    public void TypedSignal_Should_Not_Notify_When_Value_Does_Not_Change()
    {
        // Arrange
        var signal = new TypedSignal<int>(10);
        var notificationCount = 0;
        
        using var subscription = signal.SubscribeDisposable(_ => notificationCount++);
        
        // Act
        signal.Value = 10; // Same value
        signal.Value = 10; // Same value again
        
        // Assert
        notificationCount.Should().Be(0);
    }
    
    [Fact]
    public void TypedSignal_Should_Work_With_Reference_Types()
    {
        // Arrange
        var person1 = new Person { Name = "John", Age = 30 };
        var person2 = new Person { Name = "Jane", Age = 25 };
        var signal = new TypedSignal<Person?>(person1);
        var notifications = new List<Person?>();
        
        using var subscription = signal.SubscribeDisposable(p => notifications.Add(p));
        
        // Act
        signal.Value = person2;
        signal.Value = null;
        signal.Value = person1;
        
        // Assert
        notifications.Should().HaveCount(3);
        notifications[0].Should().Be(person2);
        notifications[1].Should().BeNull();
        notifications[2].Should().Be(person1);
    }
    
    [Fact]
    public void TypedSignal_Should_Support_Value_Types()
    {
        // Arrange
        var signal = new TypedSignal<DateTime>(DateTime.MinValue);
        var notifications = new List<DateTime>();
        
        using var subscription = signal.SubscribeDisposable(dt => notifications.Add(dt));
        
        // Act
        var date1 = new DateTime(2024, 1, 1);
        var date2 = new DateTime(2024, 12, 31);
        signal.Value = date1;
        signal.Value = date2;
        
        // Assert
        notifications.Should().Equal(date1, date2);
    }
    
    [Fact]
    public void TypedSignal_Should_Handle_Null_Values()
    {
        // Arrange
        var signal = new TypedSignal<string?>("initial");
        var notifications = new List<string?>();
        
        using var subscription = signal.SubscribeDisposable(value => notifications.Add(value));
        
        // Act
        signal.Value = null;
        signal.Value = "not null";
        signal.Value = null;
        
        // Assert
        notifications.Should().Equal(null, "not null", null);
    }
    
    [Fact]
    public void TypedSignal_Should_Support_Collections()
    {
        // Arrange
        var signal = new TypedSignal<List<int>>(new List<int> { 1, 2, 3 });
        var notificationCount = 0;
        
        using var subscription = signal.SubscribeDisposable(_ => notificationCount++);
        
        // Act
        signal.Value = new List<int> { 4, 5, 6 };
        signal.Value.Add(7); // This won't trigger notification
        signal.Value = new List<int> { 8, 9 }; // This will
        
        // Assert
        notificationCount.Should().Be(2);
        signal.Value.Should().Equal(8, 9);
    }
    
    [Fact]
    public void TypedSignal_Multiple_Subscribers_Should_Receive_Same_Value()
    {
        // Arrange
        var signal = new TypedSignal<int>(0);
        var values1 = new List<int>();
        var values2 = new List<int>();
        var values3 = new List<int>();
        
        using var sub1 = signal.SubscribeDisposable(v => values1.Add(v));
        using var sub2 = signal.SubscribeDisposable(v => values2.Add(v));
        using var sub3 = signal.SubscribeDisposable(v => values3.Add(v));
        
        // Act
        signal.Value = 10;
        signal.Value = 20;
        signal.Value = 30;
        
        // Assert
        values1.Should().Equal(10, 20, 30);
        values2.Should().Equal(10, 20, 30);
        values3.Should().Equal(10, 20, 30);
    }
    
    [Fact]
    public void TypedSignal_Should_Work_With_Custom_Equality()
    {
        // Arrange
        var signal = new TypedSignal<PersonWithEquality>(new PersonWithEquality { Name = "John", Age = 30 });
        var notificationCount = 0;
        
        using var subscription = signal.SubscribeDisposable(_ => notificationCount++);
        
        // Act
        // Same values, should not notify due to custom equality
        signal.Value = new PersonWithEquality { Name = "John", Age = 30 };
        // Different values, should notify
        signal.Value = new PersonWithEquality { Name = "John", Age = 31 };
        
        // Assert
        notificationCount.Should().Be(1);
    }
    
    // Test models
    private class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
    
    private class PersonWithEquality : IEquatable<PersonWithEquality>
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        
        public bool Equals(PersonWithEquality? other)
        {
            if (other is null) return false;
            return Name == other.Name && Age == other.Age;
        }
        
        public override bool Equals(object? obj) => Equals(obj as PersonWithEquality);
        public override int GetHashCode() => HashCode.Combine(Name, Age);
    }
}