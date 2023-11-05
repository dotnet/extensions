# Microsoft.Extensions.Telemetry

This library provides advanced logging and telemetry enrichment capabilities for .NET applications. It allows for detailed and configurable enrichment of log entries, along with enhanced latency monitoring and logging features. It is built for applications needing sophisticated telemetry and logging insights.

## Install the package

From the command-line:

```dotnetcli
dotnet add package Microsoft.Extensions.Telemetry
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Telemetry" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage

### Service Log Enrichment

Enriches logs with application-specific information based on `ApplicationMetadata information. The bellow calls will add the service log enricher to the service collection.

```csharp
// Add service log enricher with default settings
builder.services.AddServiceLogEnricher();

// Or configure with options
builder.services.AddServiceLogEnricher(options =>
{
    options.ApplicationName = true;
    options.BuildVersion = true;
    options.DeploymentRing = true;
    options.EnvironmentName = true;
});
```

### Latency Monitoring

Provides tools for latency data collection and export, particularly to console outputs.

```csharp
// Add latency console data exporter with configuration
builder.services.AddConsoleLatencyDataExporter(options =>
{
    options.OutputCheckpoints = true;
    options.OutputMeasures = true;
    options.OutputTags = true;
});
```

### Logging Enhancements

Offers additional logging capabilities like stack trace capturing, exception message inclusion, and log redaction.

```csharp
// Enable log enrichment.
builder.EnableEnrichment(options =>
{
    options.CaptureStackTraces = true;
    options.IncludeExceptionMessage = true;
    options.MaxStackTraceLength = 500;
    options.UseFileInfoForStackTraces = true;
});

builder.Services.AddServiceLogEnricher(); // <- This call is required in order for the enricher to be added into the service collection.

// Enable log redaction
builder.EnableRedaction(options =>
{
    options.ApplyDiscriminator = true;
});

builder.Services.AddRedaction(); // <- This call is required in order for the redactor provider to be added into the service collection.

```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
