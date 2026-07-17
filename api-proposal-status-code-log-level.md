# [API Proposal]: Configurable log level for HTTP response status codes in `AddExtendedHttpClientLogging`

## Background and motivation

`HttpClientLogger` in `Microsoft.Extensions.Http.Diagnostics` hard-codes the log level based on HTTP status code:

```csharp
private static LogLevel GetLogLevel(LogRecord logRecord)
{
    const int HttpErrorsRangeStart = 400;
    const int HttpErrorsRangeEnd = 599;
    int statusCode = logRecord.StatusCode!.Value;

    if (statusCode >= HttpErrorsRangeStart && statusCode <= HttpErrorsRangeEnd)
    {
        return LogLevel.Error;
    }

    return LogLevel.Information;
}
```

Similarly, when an exception occurs (no response), it always logs at `LogLevel.Error` via `Log.OutgoingRequestError`.

This is problematic for real-world applications where certain HTTP error status codes are **expected** as part of normal business logic:

- **404 Not Found** is used for existence checks, cache lookups, or "get-or-create" patterns. The caller handles the 404 gracefully, yet the logger emits it at `Error`.
- **409 Conflict** is used for optimistic concurrency control. The application retries or merges, but the log says `Error`.
- **429 Too Many Requests** is handled by the resilience pipeline transparently, but every attempt is logged at `Error`.

### The core problem

We **still want to see these log entries**. They're useful for debugging and observability. But we need them at `Warning` or `Information`, not `Error`.

The standard .NET log filtering system (`Logging:LogLevel:CategoryName`) only supports **minimum-level thresholds**. It can suppress logs below a level, but cannot *remap* a log from one level to another. So the only options today are:

1. **Suppress all logs** from `HttpClientLogger` below `Critical`, which loses visibility entirely.
2. **Leave them at `Error`**, which triggers false alarms in monitoring (Elastic Watchers, PagerDuty, Azure Monitor alert rules, Slack channels) because the alerting systems cannot distinguish "expected 404 during a cache lookup" from "genuine 500 from a broken dependency."

Neither is acceptable. We need the logs, but at the correct severity.

### Real-world impact

In our application, HTTP clients call multiple downstream services where 404/409 responses are part of normal flows. These responses are logged at `Error`, which:
- Spams our alerting channels with non-actionable noise
- Causes on-call engineers to develop alert fatigue
- Makes it harder to spot genuine errors in the noise
- Cannot be fixed by log filtering because we'd lose the diagnostic value of seeing the calls

## API Proposal

```csharp
namespace Microsoft.Extensions.Http.Logging;

/// <summary>
/// Maps a status code or range of status codes to a specific log level.
/// </summary>
public class HttpStatusCodeLogLevelRule
{
    /// <summary>
    /// Gets or sets the minimum status code this rule applies to (inclusive).
    /// </summary>
    [Range(100, 599)]
    public int FromStatusCode { get; set; }

    /// <summary>
    /// Gets or sets the maximum status code this rule applies to (inclusive).
    /// When null, matches only <see cref="FromStatusCode"/> (exact match).
    /// </summary>
    [Range(100, 599)]
    public int? ToStatusCode { get; set; }

    /// <summary>
    /// Gets or sets the log level to use for responses matching this rule.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}

// Additions to the existing LoggingOptions class:
namespace Microsoft.Extensions.Http.Logging;

public class LoggingOptions
{
    // ... existing properties ...

    /// <summary>
    /// Gets or sets rules that map HTTP response status codes to log levels.
    /// Rules are evaluated in order; the first matching rule wins.
    /// If no rule matches, the default behavior applies
    /// (Information for 1xx-3xx, Error for 4xx-5xx).
    /// </summary>
    public IList<HttpStatusCodeLogLevelRule> StatusCodeLogLevelRules { get; set; } = [];

    /// <summary>
    /// Gets or sets the log level used when the request fails with an exception
    /// (no HTTP response available).
    /// </summary>
    /// <value>
    /// The default value is <see cref="LogLevel.Error"/>.
    /// </value>
    public LogLevel ExceptionLogLevel { get; set; } = LogLevel.Error;
}
```

## API Usage

### Code configuration

```csharp
builder.Services.AddHttpClient<MyApiClient>()
    .AddExtendedHttpClientLogging(options =>
    {
        options.StatusCodeLogLevelRules =
        [
            // Expected "not found" responses during cache lookups
            new() { FromStatusCode = 404, LogLevel = LogLevel.Information },
            // Rate limiting is a warning, not an error
            new() { FromStatusCode = 429, LogLevel = LogLevel.Warning },
            // All other 4xx are warnings
            new() { FromStatusCode = 400, ToStatusCode = 499, LogLevel = LogLevel.Warning },
            // 5xx remain errors (explicit, same as default)
            new() { FromStatusCode = 500, ToStatusCode = 599, LogLevel = LogLevel.Error },
        ];

        // Exceptions (e.g. timeouts, DNS failures) log at Warning instead of Error
        options.ExceptionLogLevel = LogLevel.Warning;
    });
```

### Configuration from appsettings.json

```json
{
  "HttpClientLogging": {
    "StatusCodeLogLevelRules": [
      { "FromStatusCode": 404, "LogLevel": "Information" },
      { "FromStatusCode": 429, "LogLevel": "Warning" },
      { "FromStatusCode": 400, "ToStatusCode": 499, "LogLevel": "Warning" },
      { "FromStatusCode": 500, "ToStatusCode": 599, "LogLevel": "Error" }
    ],
    "ExceptionLogLevel": "Warning"
  }
}
```

### Per-client configuration (via keyed options)

The existing keyed options mechanism means different clients can have different rules:

```csharp
builder.Services.AddHttpClient<PaymentGatewayClient>()
    .AddExtendedHttpClientLogging(options =>
    {
        // Payment gateway: all errors are critical
        options.StatusCodeLogLevelRules = [];  // Use defaults (Error for 4xx/5xx)
    });

builder.Services.AddHttpClient<CacheLookupClient>()
    .AddExtendedHttpClientLogging(options =>
    {
        // Cache: 404 is normal, 5xx is warning (cache is non-critical)
        options.StatusCodeLogLevelRules =
        [
            new() { FromStatusCode = 404, LogLevel = LogLevel.Information },
            new() { FromStatusCode = 500, ToStatusCode = 599, LogLevel = LogLevel.Warning },
        ];
    });
```

## Alternative Designs

### 1. Delegate-based approach

```csharp
public Func<int, LogLevel>? LogLevelSelector { get; set; }
```

Pros: Maximum flexibility.
Cons: Not JSON-serializable, not bindable from configuration, can't be validated, opaque to tooling.

### 2. Fixed properties per status code class

```csharp
public LogLevel ClientErrorLogLevel { get; set; } = LogLevel.Error;   // 4xx
public LogLevel ServerErrorLogLevel { get; set; } = LogLevel.Error;   // 5xx
public IDictionary<int, LogLevel> StatusCodeOverrides { get; set; }   // exact codes
```

Pros: Simpler for the common case.
Cons: Two different configuration shapes for the same concern. Cannot express arbitrary ranges (e.g. 500-502 = Warning, 503-599 = Error).

### 3. Dictionary-only approach

```csharp
public IDictionary<int, LogLevel> StatusCodeLogLevels { get; set; }
```

Pros: Very simple.
Cons: Cannot express ranges without listing every code. A 404+429 override needs two entries but "all 4xx = Warning" requires 100 entries.

## Risks

- **Backward compatibility:** Fully backward-compatible. Default behavior is preserved when `StatusCodeLogLevelRules` is empty (which is the default).
- **Performance:** Rule evaluation is a linear scan over a small list (typically 2-5 rules). Given that this runs once per HTTP request at logging time, the overhead is negligible.
- **Validation:** `LoggingOptionsValidator` needs a new rule to enforce `FromStatusCode <= ToStatusCode` when both are set. Straightforward addition.
- **Source-generated logging:** `Log.OutgoingRequestError` currently hard-codes `LogLevel.Error` via `[LoggerMessage]`. The exception path would need to call the dynamic `OutgoingRequest(logger, level, ...)` method instead, which already exists for the non-exception path.
