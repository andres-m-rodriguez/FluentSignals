# SignalBus Configuration Options

This directory contains all the configuration options for the FluentSignals SignalBus. The SignalBus provides a comprehensive messaging infrastructure with support for advanced features.

## Configuration Structure

### Core Options (SignalBusOptions.cs)
- **Auto-Registration**: Automatic discovery and registration of message handlers
- **Global Settings**: Timeout, concurrency, and performance monitoring
- **Feature Toggles**: Enable/disable specific features

### Feature-Specific Options

#### 1. Message Lifecycle (MessageLifecycleOptions.cs)
- Pre/Post send hooks
- Pre/Post receive hooks
- Error and completion hooks
- Hook execution order and timeout configuration

#### 2. Transformation Pipeline (TransformationPipelineOptions.cs)
- Message transformation rules
- Caching and validation
- Parallel execution support
- Custom transformation ordering

#### 3. Subscription Strategies (SubscriptionStrategyOptions.cs)
- Topic, Type, Pattern, and Channel-based subscriptions
- Wildcard support
- Priority-based routing
- Subscription groups and filtering

#### 4. Message Deduplication (DeduplicationOptions.cs)
- Multiple deduplication strategies
- Content-based and ID-based deduplication
- Sliding window support
- Configurable duplicate handling

#### 5. Backpressure & Flow Control (BackpressureOptions.cs)
- Queue management strategies
- Rate limiting with multiple algorithms
- Circuit breaker pattern
- Adaptive backpressure

#### 6. Serialization & Compression (SerializationOptions.cs)
- Multiple serialization formats (JSON, MessagePack, Protobuf, etc.)
- Compression algorithms (Gzip, Brotli, LZ4, Zstd)
- Encryption support
- Schema validation

#### 7. Multi-Tenancy (MultiTenancyOptions.cs)
- Tenant isolation strategies
- Per-tenant configuration
- Quota management
- Cross-tenant messaging control

#### 8. Event Sourcing (EventSourcingOptions.cs)
- Event store integration
- Snapshots and projections
- Event replay capabilities
- Retention policies

#### 9. Batching & Coalescing (BatchingOptions.cs)
- Smart batching by type/destination
- Message coalescing strategies
- Adaptive batching
- Batch compression

#### 10. Distributed Mode (DistributedOptions.cs)
- Multiple transport options (Redis, RabbitMQ, Kafka, etc.)
- Node discovery and clustering
- Failover and leader election
- Distributed caching

#### 11. Development & Debugging (DebuggingOptions.cs)
- Message tracing and inspection
- Performance profiling
- Diagnostic endpoints
- Test mode with fault injection
- Visualization tools

## Usage Examples

### Basic Configuration
```csharp
services.AddSignalBus(options => options
    .Configure(o => o.GlobalTimeout = TimeSpan.FromSeconds(60))
    .WithAutoRegistration("MyApp.Handlers")
    .WithBackpressure(bp => bp.MaxQueueSize = 5000));
```

### High-Throughput Configuration
```csharp
services.AddSignalBusHighThroughput(options => options
    .WithBatching(b => b.MaxBatchSize = 1000)
    .WithSerialization(s => s.EnableBinaryOptimization = true));
```

### Development Configuration
```csharp
services.AddSignalBusDevelopment(options => options
    .WithDebugging(d => {
        d.EnableInspector = true;
        d.Inspector.UiPort = 5001;
    }));
```

### Distributed Configuration
```csharp
services.AddSignalBus(options => options
    .WithDistributed(d => {
        d.Enabled = true;
        d.Transport = TransportType.Redis;
        d.Connection.ConnectionString = "localhost:6379";
    })
    .WithEventSourcing(es => {
        es.Enabled = true;
        es.StoreType = EventStoreType.PostgreSql;
    }));
```

## Configuration Builder

The `SignalBusOptionsBuilder` provides a fluent API for configuration with:
- Presets for common scenarios (High Throughput, Low Latency, Development, Production)
- Method chaining for easy configuration
- Integration with dependency injection

## Best Practices

1. **Start with Presets**: Use the built-in presets and customize as needed
2. **Enable Features Gradually**: Start with core features and add advanced features as needed
3. **Monitor Performance**: Use debugging features in development, disable in production
4. **Configure Limits**: Set appropriate limits for queues, batches, and concurrency
5. **Test Configuration**: Use test mode to validate configuration before deployment