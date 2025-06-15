# SignalBus Comprehensive Feature List

## Core Features

### 1. **Basic Publish-Subscribe**
- Type-safe message publishing and consumption
- Async and sync message handling
- IDisposable subscriptions for easy cleanup

### 2. **Message Lifecycle Hooks**
- `OnBeforePublish` - Modify messages before sending
- `OnAfterPublish` - Track successful sends
- `OnBeforeReceive` - Pre-process incoming messages
- `OnAfterReceive` - Post-process handled messages
- `OnError` - Handle processing errors
- `OnMessageDropped` - Track dropped messages

### 3. **Auto-Registration**
- Automatically discover and register handlers via reflection
- Assembly scanning with configurable patterns
- Convention-based handler discovery
- Configurable handler lifetimes (Singleton, Scoped, Transient)

### 4. **Message Transformation Pipeline**
- Transform messages before delivery
- Type-specific and global transformers
- Transformation caching for performance
- Context-aware transformations

### 5. **Subscription Strategies**
- Weak reference support to prevent memory leaks
- Subscription limits per message type
- Priority-based message delivery
- Automatic inactive subscription cleanup

### 6. **Message Deduplication**
- Prevent duplicate message processing
- Configurable deduplication window
- Custom key extraction strategies
- Automatic cleanup of expired entries

### 7. **Backpressure & Flow Control**
- Rate limiting with configurable windows
- Concurrent message processing limits
- Queue depth management
- Drop strategies (DropOldest, DropNewest, Block)
- Adaptive throttling based on system load

### 8. **Message Serialization & Compression**
- Multiple serializer support (JSON, MessagePack, Protobuf)
- Automatic compression for large messages
- Encryption support for sensitive data
- Custom serializer plugins

### 9. **Multi-Tenancy Support**
- Tenant isolation for messages
- Per-tenant quotas and limits
- Cross-tenant broadcast capabilities
- Custom tenant resolution strategies

### 10. **Event Sourcing Integration**
- Automatic event generation for state changes
- Event store integration
- Snapshot support
- Event replay capabilities
- Projection support

### 11. **Smart Batching & Coalescing**
- Automatic message batching by time/size
- Message coalescing to reduce duplicates
- Type-aware batching strategies
- Custom batch processors

### 12. **Distributed SignalBus**
- Multi-node message distribution
- Redis backplane support
- SignalR integration
- Partition strategies
- Node discovery and health checking

### 13. **Middleware Pipeline**
- Pluggable middleware components
- Built-in middleware (Logging, Validation, Authorization)
- Custom middleware support
- Conditional middleware execution

### 14. **Request-Response Pattern**
- Send requests and await responses
- Timeout support
- Correlation tracking
- Type-safe request/response matching

### 15. **Message Scheduling**
- Schedule messages for future delivery
- Recurring message support (cron expressions)
- Timezone-aware scheduling
- Schedule persistence

### 16. **Metrics & Monitoring**
- Comprehensive metrics collection
- Per-message-type statistics
- Handler performance tracking
- Health check endpoints
- Prometheus/OpenTelemetry integration

### 17. **Development & Debugging**
- Built-in diagnostics dashboard
- Message flow visualization
- Performance profiling
- Message replay for debugging
- Chaos engineering support

## Configuration Examples

### High-Performance Configuration
```csharp
services.AddSignalBus(options =>
{
    options.UsePreset(SignalBusPreset.HighThroughput);
    options.Batching.Enabled = true;
    options.Batching.MaxBatchSize = 1000;
    options.BackpressureOptions.MaxConcurrentMessages = 200;
});
```

### Reliable Messaging Configuration
```csharp
services.AddSignalBus(options =>
{
    options.Deduplication.Enabled = true;
    options.LifecycleHooks.OnError = async (msg, ex) => 
        await errorLogger.LogError(ex);
    options.EventSourcing.Enabled = true;
});
```

### Development Configuration
```csharp
services.AddSignalBus(options =>
{
    options.UsePreset(SignalBusPreset.Development);
    options.Debugging.LogAllMessages = true;
    options.Debugging.EnableDiagnosticsDashboard = true;
});
```

## Advanced Usage Patterns

### 1. **Saga Pattern**
```csharp
// Long-running business processes
public class OrderSaga : ISaga<OrderPlaced, OrderCompleted>
{
    public async Task Handle(OrderPlaced message)
    {
        // Process order steps
    }
}
```

### 2. **Event Aggregation**
```csharp
// Aggregate multiple events into summaries
consumer.SubscribeAggregated<PriceUpdate>(
    window: TimeSpan.FromSeconds(5),
    aggregator: updates => updates.Average(u => u.Price)
);
```

### 3. **Circuit Breaker**
```csharp
// Protect against cascading failures
options.CircuitBreaker = new CircuitBreakerOptions
{
    FailureThreshold = 5,
    ResetTimeout = TimeSpan.FromMinutes(1)
};
```

### 4. **Message Replay**
```csharp
// Replay historical messages
await signalBus.ReplayMessages<OrderEvent>(
    from: DateTime.UtcNow.AddDays(-1),
    filter: e => e.Status == "Failed"
);
```

## Performance Characteristics

- **Throughput**: 100,000+ messages/second (in-memory)
- **Latency**: Sub-millisecond for local delivery
- **Memory**: Efficient memory usage with pooling
- **Scalability**: Horizontal scaling with distributed mode

## Best Practices

1. **Use batching for high-frequency messages**
2. **Enable deduplication for idempotent operations**
3. **Configure appropriate backpressure limits**
4. **Use middleware for cross-cutting concerns**
5. **Monitor metrics in production**
6. **Use request-response for command patterns**
7. **Enable event sourcing for audit trails**

## Integration Points

- **Blazor Components**: Seamless integration with Blazor lifecycle
- **Dependency Injection**: Full DI container support
- **Logging**: Integrated with ASP.NET Core logging
- **Health Checks**: Standard health check integration
- **OpenTelemetry**: Distributed tracing support
- **Authentication**: Built-in auth middleware