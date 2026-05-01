# Implementation Patterns

Code patterns for common convention update change types. Use these as templates when implementing compensating changes.

> **Reuse before adding.** Before applying any of the patterns below, search the touched libraries (`Common/`, `TelemetryHelpers.cs`, `OpenTelemetryLog.cs`, and sibling OpenTelemetry* client files) for an existing helper, method, or internal type that already does the same thing. Reuse or extend it instead of adding a parallel implementation. If the same logic will be needed in two or more places, factor it into `Common/` from the start rather than duplicating it per file. The same rule applies to parallel internal types — when a sibling client already defines a type with the same shape, unify under a single shared definition. See [review-checklist.md §3](review-checklist.md#3-code-deduplication) for what reviewers look for.

## Pattern 1: Adding a New Constant

Location: `src/Libraries/Microsoft.Extensions.AI/OpenTelemetryConsts.cs`

Constants are organized into nested static classes by category. Find the appropriate section and add the new constant.

```csharp
// In the appropriate nested class (e.g., GenAI, GenAI.Client, GenAI.Request, GenAI.Usage)
public const string NewAttributeName = "gen_ai.new.attribute";
```

**Naming convention**: The C# constant name uses PascalCase, omitting the `gen_ai.` prefix where the parent class already implies it. For example:
- `gen_ai.request.stream` → in `GenAI.Request` class: `public const string Stream = "gen_ai.request.stream";`
- `gen_ai.usage.reasoning.output_tokens` → in `GenAI.Usage` class: `public const string ReasoningOutputTokens = "gen_ai.usage.reasoning.output_tokens";`

## Pattern 2: Emitting a Span Attribute

Location: Relevant OpenTelemetry* client file (e.g., `OpenTelemetryChatClient.cs`)

Keep provider-agnostic and provider-specific instrumentation separated:

- Generic `gen_ai.*` attributes belong in `src/Libraries/Microsoft.Extensions.AI/ChatCompletion/OpenTelemetryChatClient.cs` or the relevant generic OpenTelemetry* client.
- Provider-specific attributes, such as `openai.*`, belong in the provider package (`src/Libraries/Microsoft.Extensions.AI.OpenAI/`) so `OpenTelemetryChatClient` remains provider-agnostic.
- For OpenAI-specific mappings, add helper logic near the existing `openai.api.type` handling in `OpenAIClientExtensions.cs`, invoke it from the provider client that exposes the SDK value, and test it in `test/Libraries/Microsoft.Extensions.AI.OpenAI.Tests/`.
- Use `SetTag` for provider-specific response attributes that can arrive on multiple streaming updates so repeated updates do not duplicate tags.

### Request attributes (set before the call)

In the `CreateAndConfigureActivity` or equivalent method, add the attribute after the activity is created:

```csharp
activity?.SetTag(OpenTelemetryConsts.GenAI.Request.NewAttribute, value);
```

### Response attributes (set after the call)

In the `TraceResponse` or equivalent method:

```csharp
activity?.SetTag(OpenTelemetryConsts.GenAI.Response.NewAttribute, responseValue);
```

### Conditional attributes (only set when value is present)

```csharp
if (someValue is not null)
{
    activity?.SetTag(OpenTelemetryConsts.GenAI.Request.NewAttribute, someValue);
}
```

### Boolean attributes

```csharp
activity?.SetTag(OpenTelemetryConsts.GenAI.Request.Stream, true);
```

## Pattern 3: Adding a Usage Token Attribute

Location: `OpenTelemetryChatClient.cs`, in the response tracing section

Usage tokens follow a specific pattern where they're emitted as span attributes from response tracing:

```csharp
// In TraceResponse or equivalent:
if (usage?.NewTokenCount is int newTokens and > 0)
{
    activity?.SetTag(OpenTelemetryConsts.GenAI.Usage.NewTokens, newTokens);
}
```

Only update `gen_ai.client.token.usage` metric recording when the convention adds or changes a token metric type. Do not add usage span attributes as metric tags.

## Pattern 4: Adding a Metric

Location: OpenTelemetry* client constructor for instrument creation, emission site for recording.

### Instrument creation (in constructor)

```csharp
private readonly Histogram<double> _newMetric;

// In constructor:
_newMetric = meter.CreateHistogram<double>(
    OpenTelemetryConsts.GenAI.Client.NewMetricName,
    OpenTelemetryConsts.SecondsUnit,  // or TokensUnit, or null
    "Description of the metric.");
```

### Recording the metric

```csharp
_newMetric.Record(value, tags);
```

## Pattern 5: Adding an Event via ILogger

Location: `src/Libraries/Microsoft.Extensions.AI/Common/OpenTelemetryLog.cs` for the definition, emission site for the call.

**IMPORTANT**: Use `ILogger` with `[LoggerMessage]`, NOT `Activity.AddEvent`. This is the established pattern per reviewer feedback.

### Define the log message

```csharp
// In OpenTelemetryLog.cs
[LoggerMessage(
    EventName = "gen_ai.event.name",
    Level = LogLevel.Warning,
    Message = "gen_ai.event.name")]
internal static partial void EventName(ILogger logger, Exception error);
```

Note: The `Message` text should match the OTel event name. Parameters vary by event — use `Exception error` for exception events, add other parameters as needed.

### Call the log method

```csharp
if (_logger is not null)
{
    OpenTelemetryLog.EventName(_logger, exception);
}
```

## Pattern 6: Updating Version References

When bumping the convention version (e.g. v1.39 → v1.40), update the doc comment in all matched OpenTelemetry* client files:

```csharp
// Before:
/// Semantic Conventions for Generative AI systems v1.39,
// After:
/// Semantic Conventions for Generative AI systems v1.40,
```

Find all occurrences:
```bash
grep -rn "Semantic Conventions for Generative AI systems v" src/Libraries/Microsoft.Extensions.AI/
```

## Pattern 7: Modifying Message Serialization

Location: `OpenTelemetryChatClient.cs`, `SerializeChatMessages()` method and related inner classes.

### Adding a new content part type

1. Add a new inner class:
```csharp
private sealed class OtelNewPart
{
    public string? Type { get; set; }
    public string? Value { get; set; }
}
```

2. Register with the JSON serializer context:
```csharp
[JsonSerializable(typeof(OtelNewPart))]
// Add to the OtelContext partial class
```

3. Add a case in `SerializeChatMessages()`:
```csharp
case NewContentType newContent:
    writer.WriteRawValue(JsonSerializer.SerializeToUtf8Bytes(
        new OtelNewPart { Type = "new_type", Value = newContent.Value },
        OtelContext.Default.OtelNewPart));
    break;
```

## Pattern 8: Sensitive Data Gating

Any attribute that could contain user-generated content must be gated:

```csharp
if (EnableSensitiveData)
{
    activity?.SetTag(OpenTelemetryConsts.GenAI.SensitiveAttribute, sensitiveValue);
}
```

Check the `EnableSensitiveData` property (set directly or from environment variable `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT`).

## Pattern 9: Span Naming for Tool Execution

Location: `FunctionInvokingChatClient.cs`

The span name format for tool execution follows the pattern:
```csharp
string spanName = $"execute_tool {toolCall.Name}";
```

For `invoke_agent` or `invoke_workflow` operations, detect based on the function metadata and adjust the operation name accordingly.

## Fluent API Style

Always use fluent chains for Activity API calls:

```csharp
// ✅ Correct — fluent chain
activity?
    .SetStatus(ActivityStatusCode.Error, errorMessage)
    .SetTag(OpenTelemetryConsts.Error.Type, errorType);

// ❌ Incorrect — separate statements
activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
activity?.SetTag(OpenTelemetryConsts.Error.Type, errorType);
```
