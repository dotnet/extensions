# Microsoft.Extensions.AI.Testing

Deterministic test doubles for applications built on `Microsoft.Extensions.AI`.

> [!IMPORTANT]
> This **preview** package is for **mocking and deterministic testing** of chat and embedding behavior. Its APIs are
> experimental (`MEAI001`) and may change or be removed in future releases.

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
- `MockEmbeddingGenerator<TInput>` (`IEmbeddingGenerator<TInput, Embedding<float>>`) for configurable, deterministic embeddings.

## MockChatClient behavior

| Behavior | Details |
| --- | --- |
| Starts empty | No responses are predefined. |
| Match order | Last response added wins. Add the most specific matches last. |
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

### Response dictionary

Use `AddResponses` when a response dictionary is more convenient than individual response factories. The default
predicate is `string.Equals(request.LastUserText, key, StringComparison.OrdinalIgnoreCase)`. Entries are seeded in
dictionary order, so the last matching entry wins.

```csharp
var client = new MockChatClient().AddResponses(new()
{
    ["hello"] = "Hello from a deterministic mock.",
    ["goodbye"] = "Goodbye from a deterministic mock.",
});
```

A dictionary deserialized from JSON works the same way:

```csharp
using System.Text.Json;

var client = new MockChatClient().AddResponses(
    JsonSerializer.Deserialize<Dictionary<string, string>>(
    """
    {
      "hello": "Hello from a deterministic mock.",
      "goodbye": "Goodbye from a deterministic mock."
    }
    """)!);
```

Use an `IEnumerable<KeyValuePair<string, string>>` only when repeated keys are needed. Entries are seeded in
enumeration order, so the last matching entry wins:

```csharp
client.AddResponses(
    new KeyValuePair<string, string>[]
    {
        new("hello", "Hello again. Nice to see you."),
        new("hello", "Hello. Nice to meet you."),
    },
    singleUse: true);
```

Use `AddResponse` to return fully populated `ChatResponse` instances, including non-text content such as tool calls:

```csharp
var client = new MockChatClient()
    .AddResponse(
        static request => request.LastUserText == "get-weather",
        static (_, _) => Task.FromResult(
            new ChatResponse(
                new ChatMessage(
                    ChatRole.Assistant,
                    [new FunctionCallContent("weather-call", "GetWeather")]))));
```

Pass a predicate when the dictionary key needs a different matching rule:

```csharp
client.AddResponses(new()
    {
        ["hello"] = "Hello from a deterministic mock.",
        ["goodbye"] = "Goodbye from a deterministic mock.",
    },
    static (request, key) => request.LastUserText?.StartsWith(key, StringComparison.OrdinalIgnoreCase) is true);
```

`singleUse` applies to every dictionary response. A custom predicate can map distinct dictionary keys to the same prompt:

```csharp
var client = new MockChatClient()
    .AddResponses(
        new()
        {
            ["hello:2"] = "Hello again. Nice to see you.",
            ["hello:1"] = "Hello. Nice to meet you.",
        },
        static (request, key) =>
        {
            string promptKey = key.Split(new[] { ':' }, 2)[0];
            return string.Equals(request.LastUserText, promptKey, StringComparison.OrdinalIgnoreCase);
        },
        singleUse: true);
```

Chat requests can include images as `DataContent`. A predicate can match image MIME types:

```csharp
var client = new MockChatClient()
    .AddResponses(
        new()
        {
            ["image/jpeg"] = "I see you shared a JPEG.",
            ["image/png"] = "I see you shared a PNG.",
        },
        static (request, mediaType) => request.Messages
            .SelectMany(static message => message.Contents)
            .OfType<DataContent>()
            .Any(content => string.Equals(content.MediaType, mediaType, StringComparison.OrdinalIgnoreCase)));
```

Pass `getResponse` to apply common asynchronous behavior to every selected response. String values are converted to
`ChatResponse` instances before `getResponse` is invoked:

```csharp
var client = new MockChatClient()
    .AddResponses(
        new()
        {
            ["hello"] = "Hello from a deterministic mock.",
            ["goodbye"] = "Goodbye from a deterministic mock.",
        },
        getResponse: async (response, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            return response;
        });
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

## Mock embeddings

Configure `MockEmbeddingGenerator<TInput>` with the embeddings a test needs. Its callback receives the original input enumerable and cancellation token, and `CallCount` records generation requests.

```csharp
using var embeddings = new MockEmbeddingGenerator<string>
{
    GenerateAsyncCallback = static (_, _, _) =>
        Task.FromResult<GeneratedEmbeddings<Embedding<float>>>(
            [new(new float[] { 0.1f, 0.2f, 0.3f })]),
};

GeneratedEmbeddings<Embedding<float>> result = await embeddings.GenerateAsync(["trail"]);
Console.WriteLine(embeddings.CallCount);
```

Use the input type required by the test, such as `MockEmbeddingGenerator<string>` or `MockEmbeddingGenerator<DataContent>`.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
