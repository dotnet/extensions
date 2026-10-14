# Microsoft.Extensions.Telemetry.Abstractions

This package contains common abstractions for high-level telemetry primitives. Here are the main features it provides:

- Enhanced Logging Capabilities
- Log Enrichment
- Latency Measurement
- HTTP Request Metadata Handling

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.Telemetry.Abstractions
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Telemetry.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage

### Enhanced Logging Capabilities

The package includes a custom logging generator that enhances the default .NET logging capabilities by replacing the default generator. This generator automatically logs the contents of collections and offers advanced logging features, significantly improving the debugging and monitoring process.

```csharp
[LoggerMessage(1, LogLevel.Information, "These are the contents of my dictionary: {temperature}")]
internal static partial void LogMyDictionary(ILogger<Program> logger, Dictionary<int, string> temperature);
```

It also adds the `LogProperties` attribute which can be applied to an object parameter of a `LoggerMessage` method. It introspects the passed-in object and automatically adds tags for all its properties. This leads to more informative logs without the need for manual tagging of each property.

```csharp
[LoggerMessage(1, LogLevel.Information, "Detected a new temperature: {temperature}")]
internal static partial void LogNewTemperature(ILogger<Program> logger, [LogProperties] Temperature temperature);

internal record Temperature(double value, TemperatureUnit unit);
```

The `TagName` attribute can be used to specify a custom tag name for each parameter or property. This is useful when you want to have more control over the naming of tags in your logs.

```csharp
[LoggerMessage(1, LogLevel.Information, "My custom tag name: {my.custom.tagname}")]
public static partial void LogMyCustomTagName(
    this ILogger logger,
    [TagName("my.custom.tagname")] string name);
```

The experimental `LogRateLimit` attribute limits how many log records a logging method emits within a time window. This protects the logging pipeline when a method is called very often, for example when a database is down and every request logs the same error. It works with any `ILogger` and needs no configuration in the application.

```csharp
[LoggerMessage(0, LogLevel.Error, "Error querying the database {hostName}")]
[LogRateLimit(5)] // At most 5 log records per second.
internal static partial void DatabaseError(ILogger logger, Exception exception, string hostName);

[LoggerMessage(1, LogLevel.Warning, "Cache miss for {key}")]
[LogRateLimit(100, WindowMilliseconds = 60_000, SegmentsPerWindow = 6)] // At most 100 per minute; the window moves every 10 seconds.
internal static partial void CacheMiss(ILogger logger, string key);
```

The limit behaves like a sliding window rate limiter without a queue. The window is split into `SegmentsPerWindow` equal segments, and calls above `PermitLimit` within the last window are dropped. With the default of one segment, the count resets once per window. The limit is shared by all callers of the method in the process, and it's checked right after `ILogger.IsEnabled`, before any other work is done for the log record.

To count the dropped log records, add a `Meter` parameter to the logging method. Each dropped call increments the `logging.rate_limiting.dropped` counter on that meter, tagged with the method's event name in `logging.rate_limiting.event_name`:

```csharp
[LoggerMessage(0, LogLevel.Error, "Error querying the database {hostName}")]
[LogRateLimit(5)]
internal static partial void DatabaseError(ILogger logger, Meter? meter, Exception exception, string hostName);
```

### Log Enrichment

Logging data can be enriched by adding custom log enrichers to the service collection. This can be done using specific implementations or generic types.

```csharp
// Using a specific implementation
builder.Services.AddLogEnricher(new CustomLogEnricher());

// Using a generic type
builder.Services.AddLogEnricher<AnotherLogEnricher>();
```

Create custom log enrichers by implementing the `ILogEnricher` interface.

```csharp
public class CustomLogEnricher : ILogEnricher
{
    public void Enrich(IEnrichmentTagCollector collector)
    {
        // Add custom logic to enrich log data
        collector.Add("CustomTag", "CustomValue");
    }
}
```

### Latency Measurement

To track latency in an application it is possible to register checkpoint, measure, and tag names using the following methods:

```csharp
builder.Services.RegisterCheckpointNames("databaseQuery", "externalApiCall");
builder.Services.RegisterMeasureNames("responseTime", "processingTime");
builder.Services.RegisterTagNames("userId", "transactionId");
```

Implement the `ILatencyDataExporter` to export latency data. This can be integrated with external systems or logging frameworks.

```csharp
public class CustomLatencyDataExporter : ILatencyDataExporter
{
    public async Task ExportAsync(LatencyData data, CancellationToken cancellationToken)
    {
        // Export logic here
    }
}
```

Use the latency context to track performance metrics in your application.

```csharp
public void YourMethod(ILatencyContextProvider contextProvider)
{
    var context = contextProvider.CreateContext();
    var checkpointToken = context.GetCheckpointToken("databaseQuery");

    // Start measuring
    context.AddCheckpoint(checkpointToken);

    // Perform operations...

    // End measuring
    context.AddCheckpoint(checkpointToken);

    // Optionally, record measures and tags
    context.RecordMeasure(context.GetMeasureToken("responseTime"), measureValue);
    context.SetTag(context.GetTagToken("userId"), "User123");
}
```

### Http Request Metadata Handling

The `IDownstreamDependencyMetadata` interface is designed to capture and store metadata about the downstream dependencies of an HTTP request. This is particularly useful for understanding external service dependencies and their impact on your application's performance and reliability.

The `IOutgoingRequestContext` interface provides a mechanism for associating metadata with outgoing HTTP requests. This allows you to enrich outbound requests with additional information that can be used for logging, telemetry, and analysis.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
