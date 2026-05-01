# Testing Guide

How to add and update tests when making convention changes. Tests for OpenTelemetry gen-ai instrumentation follow consistent patterns.

## Test File Locations

| Instrumentation Client | Test File |
|----------------------|-----------|
| `OpenTelemetryChatClient` | `test/Libraries/Microsoft.Extensions.AI.Tests/ChatCompletion/OpenTelemetryChatClientTests.cs` |
| `OpenTelemetryImageGenerator` | `test/Libraries/Microsoft.Extensions.AI.Tests/Image/OpenTelemetryImageGeneratorTests.cs` |
| `OpenTelemetryEmbeddingGenerator` | `test/Libraries/Microsoft.Extensions.AI.Tests/Embeddings/OpenTelemetryEmbeddingGeneratorTests.cs` |
| `OpenTelemetrySpeechToTextClient` | `test/Libraries/Microsoft.Extensions.AI.Tests/SpeechToText/OpenTelemetrySpeechToTextClientTests.cs` |
| `OpenTelemetryTextToSpeechClient` | `test/Libraries/Microsoft.Extensions.AI.Tests/TextToSpeech/OpenTelemetryTextToSpeechClientTests.cs` |
| `OpenTelemetryRealtimeClientSession` | `test/Libraries/Microsoft.Extensions.AI.Tests/Realtime/OpenTelemetryRealtimeClientTests.cs` |
| `OpenTelemetryHostedFileClient` | `test/Libraries/Microsoft.Extensions.AI.Tests/Files/OpenTelemetryHostedFileClientTests.cs` |

## Test Infrastructure

### In-Memory Exporters

Tests use in-memory OTel exporters to capture and assert on telemetry:

```csharp
var activities = new List<Activity>();
using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddInMemoryExporter(activities)
    .Build();
```

### Metric Collection

```csharp
using var meterCollector = new MetricCollector<double>(
    null, // meter provider
    OpenTelemetryConsts.DefaultSourceName,
    OpenTelemetryConsts.GenAI.Client.MetricName);
```

### Test Chat Client

A `TestChatClient` is used to provide controlled responses:

```csharp
var testClient = new TestChatClient
{
    GetResponseAsync = (messages, options, ct) =>
    {
        return Task.FromResult(new ChatResponse(/* configured response */));
    }
};
```

## Assertion Patterns

### Asserting Span Attributes

```csharp
var activity = Assert.Single(activities);
Assert.Equal("expected_value", activity.GetTagItem(OpenTelemetryConsts.GenAI.Request.AttributeName));
```

### Asserting Optional Attributes (null when not present)

```csharp
Assert.Null(activity.GetTagItem(OpenTelemetryConsts.GenAI.Request.OptionalAttribute));
```

### Asserting Boolean Attributes

```csharp
Assert.True(activity.GetTagItem(OpenTelemetryConsts.GenAI.Request.BoolAttribute) is true);
```

### Asserting Numeric Attributes

```csharp
Assert.Equal(42L, activity.GetTagItem(OpenTelemetryConsts.GenAI.Usage.TokenCount));
```

### Asserting Metric Values

```csharp
var measurements = meterCollector.GetMeasurementSnapshot();
var measurement = Assert.Single(measurements);
Assert.Equal(expectedValue, measurement.Value);
Assert.Equal("expected_tag_value", measurement.Tags[OpenTelemetryConsts.GenAI.Request.TagName]);
```

### JSON Content Assertions

For serialized message content, tests use whitespace-normalized JSON comparison:

```csharp
var events = activity.Events.ToList();
var eventPayload = events[0].Tags.First(t => t.Key == "gen_ai.content").Value as string;
Assert.Equal(
    NormalizeWhitespace(expectedJson),
    NormalizeWhitespace(eventPayload));
```

## Key Testing Principles

### 1. Augment Existing Tests First

Before creating new test methods, check if existing tests already exercise the scenario. Add new assertions to existing test methods when possible. This was explicit reviewer feedback on past PRs.

For example, if adding a new response attribute, find the existing test that validates response attributes and add the new assertion there.

### 2. Test Both Streaming and Non-Streaming

The `OpenTelemetryChatClient` has two code paths: `GetResponseAsync` and `GetStreamingResponseAsync`. Both must be tested. Existing tests often use `[InlineData]` or `[Theory]` to parameterize across both paths.

### 3. Test Sensitive Data Gating

If an attribute is gated behind `EnableSensitiveData`, test both:
- **With sensitive data enabled**: attribute should be present
- **With sensitive data disabled**: attribute should be absent (null)

```csharp
[Theory]
[InlineData(true)]
[InlineData(false)]
public async Task SensitiveAttribute_RespectsSetting(bool enableSensitiveData)
{
    // ... setup with enableSensitiveData
    if (enableSensitiveData)
    {
        Assert.Equal(expected, activity.GetTagItem(...));
    }
    else
    {
        Assert.Null(activity.GetTagItem(...));
    }
}
```

### 4. Test Default Values and Missing Values

Test that attributes are omitted (not set to empty/default) when the source data doesn't include the relevant field.

### 5. Verify Metric Tags Match Span Attributes

When an attribute appears on both spans and metrics, ensure tests verify both emission points.

## Build and Test Commands

See [build-commands.md](build-commands.md) for the canonical Windows and Linux/macOS forms, including the faster `dotnet test --filter` invocation for inner-loop iteration.
