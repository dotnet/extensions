# File Inventory

Files that are typically inspected and/or modified when updating OpenTelemetry gen-ai semantic conventions.

## Constants

| File | Purpose |
|------|---------|
| `src/Libraries/Microsoft.Extensions.AI/OpenTelemetryConsts.cs` | All OTel attribute and metric name constants. New attributes/metrics always get constants added here first. |

## Instrumentation Clients

These files contain the actual telemetry emission logic. Each wraps a different AI capability with OTel spans, metrics, and logs.

| File | Capability | Key Sections |
|------|-----------|--------------|
| `src/Libraries/Microsoft.Extensions.AI/ChatCompletion/OpenTelemetryChatClient.cs` | Chat completion | Activity creation, attribute emission, message serialization, streaming support, metrics |
| `src/Libraries/Microsoft.Extensions.AI/ChatCompletion/OpenTelemetryImageGenerator.cs` | Image generation | Activity creation, attribute emission |
| `src/Libraries/Microsoft.Extensions.AI/Embeddings/OpenTelemetryEmbeddingGenerator.cs` | Embeddings | Activity creation, attribute emission |
| `src/Libraries/Microsoft.Extensions.AI/SpeechToText/OpenTelemetrySpeechToTextClient.cs` | Speech-to-text | Activity creation, attribute emission |
| `src/Libraries/Microsoft.Extensions.AI/TextToSpeech/OpenTelemetryTextToSpeechClient.cs` | Text-to-speech | Activity creation, attribute emission |
| `src/Libraries/Microsoft.Extensions.AI/Realtime/OpenTelemetryRealtimeClientSession.cs` | Realtime sessions | Activity creation, attribute emission |
| `src/Libraries/Microsoft.Extensions.AI/Realtime/OpenTelemetryRealtimeClient.cs` | Realtime client wrapper | Delegates to session |
| `src/Libraries/Microsoft.Extensions.AI/Files/OpenTelemetryHostedFileClient.cs` | Hosted file management | Activity creation, attribute emission |

## Function Invocation / Tool Orchestration

These files handle `execute_tool`, `invoke_agent`, and `invoke_workflow` spans:

| File | Purpose |
|------|---------|
| `src/Libraries/Microsoft.Extensions.AI/ChatCompletion/FunctionInvokingChatClient.cs` | Chat-based tool orchestration |
| `src/Libraries/Microsoft.Extensions.AI/Realtime/FunctionInvokingRealtimeClientSession.cs` | Realtime tool orchestration |
| `src/Libraries/Microsoft.Extensions.AI/Common/FunctionInvocationProcessor.cs` | Shared function invocation logic (used by both chat and realtime) |
| `src/Libraries/Microsoft.Extensions.AI/Common/FunctionInvocationHelpers.cs` | Shared function invocation helpers |
| `src/Libraries/Microsoft.Extensions.AI/Common/FunctionInvocationLogger.cs` | Shared function invocation logging |

## Shared Code

| File | Purpose |
|------|---------|
| `src/Libraries/Microsoft.Extensions.AI/Common/OpenTelemetryLog.cs` | Shared `[LoggerMessage]` definitions for OTel events (e.g. exception recording) |
| `src/Libraries/Microsoft.Extensions.AI/TelemetryHelpers.cs` | Shared telemetry helper methods (at library root, not Common/) |

## Tests

| File | Tests For |
|------|----------|
| `test/Libraries/Microsoft.Extensions.AI.Tests/ChatCompletion/OpenTelemetryChatClientTests.cs` | Chat client telemetry |
| `test/Libraries/Microsoft.Extensions.AI.Tests/Image/OpenTelemetryImageGeneratorTests.cs` | Image generator telemetry |
| `test/Libraries/Microsoft.Extensions.AI.Tests/Embeddings/OpenTelemetryEmbeddingGeneratorTests.cs` | Embedding generator telemetry |
| `test/Libraries/Microsoft.Extensions.AI.Tests/SpeechToText/OpenTelemetrySpeechToTextClientTests.cs` | Speech-to-text telemetry |
| `test/Libraries/Microsoft.Extensions.AI.Tests/TextToSpeech/OpenTelemetryTextToSpeechClientTests.cs` | Text-to-speech telemetry |
| `test/Libraries/Microsoft.Extensions.AI.Tests/Realtime/OpenTelemetryRealtimeClientTests.cs` | Realtime session telemetry |
| `test/Libraries/Microsoft.Extensions.AI.Tests/Files/OpenTelemetryHostedFileClientTests.cs` | Hosted file client telemetry |

To discover any additional test files: `dir test\Libraries\Microsoft.Extensions.AI.Tests\ -Recurse -Filter "OpenTelemetry*Tests.cs"`

## Version References

The semantic conventions version is referenced in a doc comment in specific OpenTelemetry* instrumentation client files. When bumping the version, update all files that match the grep below — not all OpenTelemetry* files contain the version reference.

The reference looks like:

```csharp
/// This class provides an implementation of the Semantic Conventions for Generative AI systems v1.XX,
/// defined at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
```

Find all occurrences with:
```
grep -rn "Semantic Conventions for Generative AI systems v" src/Libraries/Microsoft.Extensions.AI/
```
