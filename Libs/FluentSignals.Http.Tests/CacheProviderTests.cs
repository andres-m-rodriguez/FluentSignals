using FluentAssertions;
using FluentSignals.Http.Caching;
using FluentSignals.Http.Caching.Providers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Xunit;

namespace FluentSignals.Http.Tests;

public class CacheProviderTests
{
    [Fact]
    public async Task NoCacheProvider_Should_Always_Return_Null()
    {
        // Arrange
        var provider = NoCacheProvider.Instance;
        
        // Act
        await provider.SetAsync("key", new TestData { Value = "test" });
        var result = await provider.GetAsync<TestData>("key");
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task MemoryCacheProvider_Should_Store_And_Retrieve_Data()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
        var provider = new MemoryCacheProvider(memoryCache);
        
        var data = new TestData { Value = "cached value" };
        
        // Act
        await provider.SetAsync("test-key", data);
        var retrieved = await provider.GetAsync<TestData>("test-key");
        
        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Value.Should().Be("cached value");
    }
    
    [Fact]
    public async Task MemoryCacheProvider_Should_Respect_Expiration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
        var provider = new MemoryCacheProvider(memoryCache);
        
        var data = new TestData { Value = "expiring value" };
        
        // Act
        await provider.SetAsync("expiring-key", data, TimeSpan.FromMilliseconds(50));
        var immediateRetrieve = await provider.GetAsync<TestData>("expiring-key");
        
        await Task.Delay(100);
        var afterExpiration = await provider.GetAsync<TestData>("expiring-key");
        
        // Assert
        immediateRetrieve.Should().NotBeNull();
        afterExpiration.Should().BeNull();
    }
    
    [Fact]
    public async Task MemoryCacheProvider_Should_Remove_Items()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
        var provider = new MemoryCacheProvider(memoryCache);
        
        var data = new TestData { Value = "to be removed" };
        
        // Act
        await provider.SetAsync("remove-key", data);
        var beforeRemove = await provider.GetAsync<TestData>("remove-key");
        await provider.RemoveAsync("remove-key");
        var afterRemove = await provider.GetAsync<TestData>("remove-key");
        
        // Assert
        beforeRemove.Should().NotBeNull();
        afterRemove.Should().BeNull();
    }
    
    [Fact]
    public async Task DistributedCacheProvider_Should_Serialize_And_Deserialize_Data()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();
        var provider = new DistributedCacheProvider(distributedCache);
        
        var data = new TestData { Value = "distributed value", Number = 42 };
        
        // Act
        await provider.SetAsync("dist-key", data);
        var retrieved = await provider.GetAsync<TestData>("dist-key");
        
        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Value.Should().Be("distributed value");
        retrieved.Number.Should().Be(42);
    }
    
    [Fact]
    public async Task DistributedCacheProvider_Should_Return_Null_For_Missing_Key()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();
        var provider = new DistributedCacheProvider(distributedCache);
        
        // Act
        var result = await provider.GetAsync<TestData>("non-existent");
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task DistributedCacheProvider_Should_Handle_Expiration()
    {
        // Arrange
        var distributedCache = new TestDistributedCache();
        var provider = new DistributedCacheProvider(distributedCache);
        
        var data = new TestData { Value = "expiring distributed" };
        
        // Act
        await provider.SetAsync("exp-dist-key", data, TimeSpan.FromMinutes(5));
        
        // Assert
        distributedCache.LastSetOptions.Should().NotBeNull();
        distributedCache.LastSetOptions!.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
    }
    
    // Test models
    private class TestData
    {
        public string Value { get; set; } = string.Empty;
        public int Number { get; set; }
    }
    
    // Test implementations
    private class TestDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _cache = new();
        public DistributedCacheEntryOptions? LastSetOptions { get; private set; }
        
        public byte[]? Get(string key)
        {
            return _cache.TryGetValue(key, out var value) ? value : null;
        }
        
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }
        
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _cache[key] = value;
            LastSetOptions = options;
        }
        
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
        
        public void Remove(string key)
        {
            _cache.Remove(key);
        }
        
        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
        
        public void Refresh(string key)
        {
        }
        
        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }
    }
}