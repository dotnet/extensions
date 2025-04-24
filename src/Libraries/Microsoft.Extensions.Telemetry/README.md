# Microsoft.Extensions.Telemetry

This library provides advanced logging and telemetry enrichment capabilities for .NET applications. It allows for detailed and configurable enrichment of log entries, along with enhanced latency monitoring and logging features. It is built for applications needing sophisticated telemetry and logging insights.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.Telemetry
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Telemetry" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage

### Logging Sampling

The library provides two types of log sampling mechanisms: **Random Probabilistic Sampling** and **Trace-based Sampling**.

#### Random Probabilistic Sampling

Provides configurable probability-based sampling with flexible rules:

```csharp
// Simple configuration with probability
builder.Logging.AddRandomProbabilisticSampler(0.1); // Sample 10% of all logs, meaning - 90% of logs will be dropped
builder.Logging.AddRandomProbabilisticSampler(0.1, LogLevel.Warning); // Sample 10% of Warning and lower level logs

// Configuration using options
builder.Logging.AddRandomProbabilisticSampler(options =>
{
    options.Rules.Add(new RandomProbabilisticSamplerFilterRule(0.1, logLevel: LogLevel.Information)); // Sample 10% of Information and lower level logs
    options.Rules.Add(new RandomProbabilisticSamplerFilterRule(1.0, logLevel: LogLevel.Error)); // Sample all Error logs
});

// Configuration using IConfiguration
builder.Logging.AddRandomProbabilisticSampler(configuration.GetSection("Logging:Sampling"));
```

The Random Probabilistic Sampler supports the `IOptionsMonitor<T>` pattern, allowing for dynamic configuration updates. This means you can change the sampling rules at runtime without needing to restart your application.

#### Trace-Based Sampling

Matches logging sampling decisions with the underlying [Distributed Tracing sampling decisions](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-concepts#sampling):

```csharp
// Add trace-based sampler
builder.Logging.AddTraceBasedSampler();
```
This comes in handy when you already use OpenTelemetry .NET Tracing and would like to see sampling decisions being consistent across both logs and their underlying [`Activity`](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-concepts#sampling).

### Service Log Enrichment

Enriches logs with application-specific information based on `ApplicationMetadata` information. The bellow calls will add the service log enricher to the service collection.

```csharp
// Add service log enricher with default settings
builder.Services.AddServiceLogEnricher();

// Or configure with options
builder.Services.AddServiceLogEnricher(options =>
{
    options.ApplicationName = true;
    options.BuildVersion = true;
    options.DeploymentRing = true;
    options.EnvironmentName = true;
});
```

### Latency Monitoring

Provides tools for latency data collection and export. The bellow example uses the built-in Console exporter, but custom exporters can be created by implementing the `ILatencyDataExporter` interface.

```csharp
// Add latency console data exporter with configuration
builder.Services.AddConsoleLatencyDataExporter(options =>
{
    options.OutputCheckpoints = true;
    options.OutputMeasures = true;
    options.OutputTags = true;
});
```

In order for the latency data to be exported, a call to `ILatencyDataExporter.ExportAsync()` is required. This can either be called manually, or by using the Request Latency Middleware inside the `Microsoft.AspNetCore.Diagnostics.Middleware` package by adding:

```csharp
// Add Latency Context
builder.Services.AddLatencyContext();

// Add Checkpoints, Measures, Tags
builder.Services.RegisterCheckpointNames("databaseQuery", "externalApiCall");
builder.Services.RegisterMeasureNames("responseTime", "processingTime");
builder.Services.RegisterTagNames("userId", "transactionId");

// Add Console Latency exporter.
builder.Services.AddConsoleLatencyDataExporter();
// Optionally add custom exporters.
builder.Services.AddSingleton<ILatencyDataExporter, MyCustomExporter>();

// Add Request latency telemetry.
builder.Services.AddRequestLatencyTelemetry();

// ...

// Add Request Latency Middleware which will automatically call ExportAsync on all registered latency exporters.
app.UseRequestLatencyTelemetry();
```

### Logging Enhancements

Offers additional logging capabilities like stack trace capturing, exception message inclusion, and log redaction.

```csharp
// Enable log enrichment.
builder.Logging.EnableEnrichment(options =>
{
    options.CaptureStackTraces = true;
    options.IncludeExceptionMessage = true;
    options.MaxStackTraceLength = 500;
    options.UseFileInfoForStackTraces = true;
});

builder.Services.AddServiceLogEnricher(); // <- This call is required in order for the enricher to be added into the service collection.

// Enable log redaction
builder.Logging.EnableRedaction(options =>
{
    options.ApplyDiscriminator = true;
});

builder.Services.AddRedaction(); // <- This call is required in order for the redactor provider to be added into the service collection.

```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
