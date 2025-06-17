# Changelog

All notable changes to FluentSignals will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.2] - 2024-12-16

### Added
- Custom JSON serialization support for HttpResource via `JsonSerializerOptions` configuration
- Ability to configure JSON serialization globally through `HttpResourceOptions`
- Support for custom JsonConverters for complex types
- Comprehensive documentation for handling JSON deserialization issues
- Version history page in the demo application

### Fixed
- Fixed issue where typed HTTP status handlers were being called twice
- Resolved duplicate handler invocation in `InvokeHandlers<T>` method

### Changed
- HttpResource now uses configured JsonSerializerOptions throughout all deserialization operations
- Updated HttpResponse.GetData<T>() to support custom JsonSerializerOptions

## [1.1.1] - 2024-11-15

### Added
- SignalR resource integration with `ResourceSignalR<T>` class
- Real-time data synchronization using SignalR connections
- Automatic reconnection support for SignalR resources
- Filtered subscriptions for SignalR messages based on custom predicates
- SignalR demo pages showcasing real-time stock price updates

### Changed
- Enhanced resource management capabilities with SignalR support
- Improved demo application with real-time examples

## [1.1.0] - 2024-10-20

### Added
- Resource type system with `IResource<T>` interface
- `HttpResource` class for reactive HTTP operations
- Automatic state management for loading, error, and success states
- Retry policies with exponential backoff for HTTP requests
- HTTP status code handlers (OnSuccess, OnNotFound, OnBadRequest, etc.)
- Resource-specific Blazor components (`HttpResourceView`, `ResourceView`)
- Comprehensive HTTP resource documentation

### Changed
- Expanded FluentSignals to support external data sources
- Enhanced Blazor integration with resource-aware components

## [1.0.0] - 2024-09-15

### Added
- Core signal types: `Signal<T>` and `TypedSignal<T>`
- Async signal types: `AsyncSignal` and `AsyncTypedSignal<T>`
- Computed signals for derived state
- Automatic UI updates in Blazor components
- Subscription management with proper disposal
- SignalBus for decoupled communication
- Comprehensive documentation and examples
- Full test coverage for core functionality

### Features
- Reactive state management for .NET applications
- Type-safe signal subscriptions
- Memory-efficient subscription handling
- Thread-safe signal updates
- Seamless Blazor integration

## [0.9.0-beta] - 2024-08-01

### Added
- Initial beta release
- Basic signal implementation
- Proof of concept for reactive state management

---

## Upgrade Guide

### From 1.1.1 to 1.1.2

To use custom JSON serialization options:

```csharp
services.AddFluentSignalsBlazor(options =>
{
    options.JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new MyCustomConverter() }
    };
});
```

### From 1.1.0 to 1.1.1

No breaking changes. SignalR resources are opt-in:

```csharp
var signalR = new ResourceSignalR<T>(hubUrl, eventName);
await signalR.StartAsync();
```

### From 1.0.0 to 1.1.0

No breaking changes. HTTP resources are additive:

```csharp
services.AddFluentSignalsBlazor();
var resource = resourceFactory.Create();
```