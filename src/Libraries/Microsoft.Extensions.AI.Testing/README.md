# Microsoft.Extensions.AI.Testing

Deterministic test doubles for applications built on `Microsoft.Extensions.AI`.

> [!IMPORTANT]
> This package is for **mocking and deterministic testing** of chat and embedding behavior.

## Install

```console
dotnet add package Microsoft.Extensions.AI.Testing
```

Or in a project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI.Testing" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## What you get

- `MockChatClient` (`IChatClient`) for deterministic, request-aware chat responses.
- `MockChatClientRequest` for request matching and request-history inspection.

## MockChatClient behavior

| Behavior | Details |
| --- | --- |
| Starts empty | No responses are predefined. |
| Match order | Responses are checked in reverse insertion order; the most recently added matching response wins. |
| Consumption | Seeds are reusable by default. |
| One-time seed | Set `singleUse: true` to remove a seed after its first match. |
| Unmatched request | Throws `InvalidOperationException` with the last user message text. |
| History | Every request is captured in `client.Requests` (`Messages`, `Options`, `IsStreaming`). |
| Streaming/non-streaming | Supports both `GetResponseAsync` and `GetStreamingResponseAsync`. |

## Quick start

`MockChatClient` accepts a required predicate plus a cancellable asynchronous response factory. The factory can inspect the captured request and receives the cancellation token from the matching chat call.

```csharp
using Microsoft.Extensions.AI;

var client = new MockChatClient()
    .AddResponse(
        static _ => true,
        static (_, _) => Task.FromResult(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "Fallback response."))),
        singleUse: true)
    .AddResponse(
        static request => request.LastUserText?.Contains("hello", StringComparison.OrdinalIgnoreCase) is true,
        static (_, _) => Task.FromResult(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello from a deterministic mock."))));

ChatResponse response = await client.GetResponseAsync([new(ChatRole.User, "hello")]);
Console.WriteLine(response.Text);
```

## Seeding patterns

### Non-streaming response

```csharp
client.AddResponse(
    static request => request.LastUserText == "explain",
    static (_, _) => Task.FromResult(
        new ChatResponse(new ChatMessage(ChatRole.Assistant, "Complete answer"))));
```

### Streaming response

```csharp
using System.Runtime.CompilerServices;

client.AddStreamingResponse(
    static request => request.LastUserText == "stream",
    static (_, cancellationToken) => GetUpdatesAsync(cancellationToken));

static async IAsyncEnumerable<ChatResponseUpdate> GetUpdatesAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    yield return new ChatResponseUpdate(ChatRole.Assistant, "Part 1 ");
    yield return new ChatResponseUpdate(ChatRole.Assistant, "Part 2");
}
```

### Distinct streaming and non-streaming responses

```csharp
client.AddResponse(
    static request => request.LastUserText == "both",
    static (_, _) => Task.FromResult(
        new ChatResponse(new ChatMessage(ChatRole.Assistant, "Complete answer"))),
    static (_, cancellationToken) => GetUpdatesAsync(cancellationToken));
```

## Exercise rich chat features

`MockChatClient` does not synthesize model output; factories return the payloads you choose. Seed fully populated `ChatResponse` and `ChatResponseUpdate` payloads to exercise:

- citations and other annotations
- reasoning content
- tool calls and tool results
- usage fields
- additional metadata

For example, a response can include citations, reasoning, and usage:

```csharp
client.AddResponse(
    static request => request.LastUserText == "sources",
    static (_, _) => Task.FromResult(
        new ChatResponse(
        [
            new ChatMessage(
                ChatRole.Assistant,
                [
                    new TextContent("TrailMaster tracks your progress.")
                    {
                        Annotations =
                        [
                            new CitationAnnotation
                            {
                                Title = "Example_GPS_Watch.md",
                                Snippet = "track your progress",
                            }
                        ]
                    },
                    new TextReasoningContent("The document describes tracking features.")
                ])
        ])
        {
            Usage = new UsageDetails { InputTokenCount = 3, OutputTokenCount = 7 }
        }));
```

## Assert what your app sent

```csharp
MockChatClientRequest first = client.Requests[0];
string? lastUserText = first.LastUserText;
bool wasStreaming = first.IsStreaming;
```

This is useful for validating prompts, routing logic, and chat options.

## Clear and reseed

`ClearResponses()` removes all response and exception seeds while preserving captured request history:

```csharp
client.ClearResponses()
    .AddResponse(
        static _ => true,
        static (_, _) => Task.FromResult(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "New test scenario."))));
```

## Mock errors

Use `AddException` to model a failing provider. The factory creates a new exception for each matching request:

```csharp
client.AddException(
    static request => request.LastUserText == "UNAVAILABLE",
    static () => new HttpRequestException("The provider is temporarily unavailable."));
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
