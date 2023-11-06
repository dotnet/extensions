# Microsoft.Extensions.Http.Diagnostics

Telemetry support for HTTP Client that allows tracking latency and enriching and redacting log output.

## Install the package

From the command-line:

```dotnetcli
dotnet add package Microsoft.Extensions.Http.Diagnostics
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http.Diagnostics" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### HTTP Client Logs Enrichment and Redaction

These components enable enriching and redacting HTTP Client request logs. They remove built-it HTTP Client logging.

In order to use the redaction feature, you need to reference the `Microsoft.Extensions.Compliance.Redaction` package.

The services can be registered using the following methods:

```csharp
public static IServiceCollection AddExtendedHttpClientLogging(this IServiceCollection services);
public static IServiceCollection AddExtendedHttpClientLogging(this IServiceCollection services, IConfigurationSection section);
public static IServiceCollection AddExtendedHttpClientLogging(this IServiceCollection services, Action<LoggingOptions> configure);
public static IServiceCollection AddHttpClientLogEnricher<T>(this IServiceCollection services) where T : class, IHttpClientLogEnricher;
```

For example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Register IHttpClientFactory:
builder.Services.AddHttpClient();

// Register redaction services:
builder.Services.AddRedaction();

// Register HttpClient logging enrichment & redaction services:
builder.Services.AddExtendedHttpClientLogging();

var host = builder.Build();
```

You can also use the following extension methods to apply the logging to the specific `IHttpClientBuilder`:

```csharp
public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder);
public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder, IConfigurationSection section);
public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder, Action<LoggingOptions> configure);
```

For example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Register redaction services:
builder.Services.AddRedaction();

// Register named HttpClient:
var httpClientBuilder = builder.Services.AddHttpClient("MyNamedClient");

// Configure named HttpClient to use logging enrichment & redaction:
httpClientBuilder.AddExtendedHttpClientLogging();

var host = builder.Build();
```

### Tracking HTTP Request Client Latency

These components enable tracking and reporting the latency of HTTP Client request processing.

The services can be registered using the following methods:

```csharp
public static IServiceCollection AddHttpClientLatencyTelemetry(this IServiceCollection services);
public static IServiceCollection AddHttpClientLatencyTelemetry(this IServiceCollection services, IConfigurationSection section);
public static IServiceCollection AddHttpClientLatencyTelemetry(this IServiceCollection services, Action<HttpClientLatencyTelemetryOptions> configure);
```

For example:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Register IHttpClientFactory:
builder.Services.AddHttpClient();

// Register redaction services:
builder.Services.AddRedaction();

// Register latency context services:
builder.Services.AddLatencyContext();

// Register HttpClient logging enrichment & redaction services:
builder.Services.AddExtendedHttpClientLogging();

// Register HttpClient latency telemetry services:
builder.Services.AddHttpClientLatencyTelemetry();

var host = builder.Build();
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
