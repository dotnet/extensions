# Microsoft.AspNetCore.Diagnostics.Middleware

HTTP request diagnostics middleware for tracking latency and enriching and redacting log output.

## Install the package

From the command-line:

```dotnetcli
dotnet add package Microsoft.AspNetCore.Diagnostics.Middleware
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Diagnostics.Middleware" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Tracking HTTP Request Latency

These components enable tracking and reporting the latency of HTTP request processing.

The services can be registered using the following methods:

```csharp
public static IServiceCollection AddRequestCheckpoint(this IServiceCollection services)
public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services)
public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, Action<RequestLatencyTelemetryOptions> configure)
public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, IConfigurationSection section)
```

The middleware can be registered using the following methods:

```csharp
public static IApplicationBuilder UseRequestCheckpoint(this IApplicationBuilder builder)
public static IApplicationBuilder UseRequestLatencyTelemetry(this IApplicationBuilder builder)
```

For example:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRequestLatencyTelemetry();
builder.Services.AddRequestCheckpoint(options => { });

var app = builder.Build();

app.UseRequestCheckpoint();
app.UseRequestLatencyTelemetry();
```

## HTTP Request Logs Enrichment and Redaction

These components enable enriching and redacting ASP.NET Core's [HTTP request logs](https://learn.microsoft.com/aspnet/core/fundamentals/http-logging/).

These APIs are only available for ASP.NET Core 8+.

The services can be registered using the following methods:

```csharp
public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, Action<HeaderParsingOptions>? configure = null);
public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, IConfigurationSection section);
public static IServiceCollection AddHttpLogEnricher<T>(this IServiceCollection services)
```

The middleware can be registered using the following method:

```csharp
public static IApplicationBuilder UseHttpLogging(this IApplicationBuilder builder);
```

For example:

```csharp
var builder = WebApplication.CreateBuilder(args);

// General logging options
builder.Services.AddHttpLogging(options => { });
// Redaction options
builder.Services.AddHttpLoggingRedaction(options => { });

var app = builder.Build();

app.UseHttpLogging();
```

## Feedback & Contributing

For any feedback or contributions, please visit us in [our GitHub repo](https://github.com/dotnet/extensions).
