# Microsoft.Extensions.AI.Abstractions

Provides abstractions representing generative AI components.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AI.Abstractions
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Examples

### `IChatClient`

The `IChatClient` interface defines a client abstraction responsible for interacting with AI services that provide chat capabilities. It defines methods for sending and receiving messages comprised of multi-modal content (text, images, audio, etc.), either as a complete set or streamed incrementally. Additionally, it provides metadata information about the client and allows for retrieving strongly-typed services that may be provided by the client or its underlying services.

#### Sample Implementation

.NET libraries that provide clients for language models and services may provide an implementation of the `IChatClient` interface. Any consumers of the interface are then able to interoperate seamlessly with these models and services via the abstractions.

Here is a sample implementation of an `IChatClient` to show the general structure. You can find other concrete implementations in the following packages:

- [Microsoft.Extensions.AI.AzureAIInference](https://aka.ms/meai-azaiinference-nuget)
- [Microsoft.Extensions.AI.OpenAI](https://aka.ms/meai-openai-nuget)
- [Microsoft.Extensions.AI.Ollama](https://aka.ms/meai-ollama-nuget)

```csharp
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

public class SampleChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; }

    public SampleChatClient(Uri endpoint, string modelId) =>
        Metadata = new("SampleChatClient", endpoint, modelId);

    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Simulate some operation.
        await Task.Delay(300, cancellationToken);

        // Return a sample chat completion response randomly.
        string[] responses =
        [
            "This is the first sample response.",
            "Here is another example of a response message.",
            "This is yet another response message."
        ];

        return new([new ChatMessage()
        {
            Role = ChatRole.Assistant,
            Text = responses[Random.Shared.Next(responses.Length)],
        }]);
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Simulate streaming by yielding messages one by one.
        string[] words = ["This ", "is ", "the ", "response ", "for ", "the ", "request."];
        foreach (string word in words)
        {
            // Simulate some operation.
            await Task.Delay(100, cancellationToken);

            // Yield the next message in the response.
            yield return new StreamingChatCompletionUpdate
            {
                Role = ChatRole.Assistant,
                Text = word,
            };
        }
    }

    public TService? GetService<TService>(object? key = null) where TService : class =>
        this as TService;

    void IDisposable.Dispose() { }
}
```

#### Requesting a Chat Completion: `CompleteAsync`

With an instance of `IChatClient`, the `CompleteAsync` method may be used to send a request. The request is composed of one or more messages, each of which is composed of one or more pieces of content. Accelerator methods exist to simplify common cases, such as constructing a request for a single piece of text content.

```csharp
using Microsoft.Extensions.AI;

IChatClient client = new SampleChatClient(new Uri("http://coolsite.ai"), "my-custom-model");

var response = await client.CompleteAsync("What is AI?");

Console.WriteLine(response.Message);
```

The core `CompleteAsync` method on the `IChatClient` interface accepts a list of messages. This list represents the history of all messages that are part of the conversation.

```csharp
using Microsoft.Extensions.AI;

IChatClient client = new SampleChatClient(new Uri("http://coolsite.ai"), "my-custom-model");

Console.WriteLine(await client.CompleteAsync(
[
    new(ChatRole.System, "You are a helpful AI assistant"),
    new(ChatRole.User, "What is AI?"),
]));
```

#### Requesting a Streaming Chat Completion: `CompleteStreamingAsync`

The inputs to `CompleteStreamingAsync` are identical to those of `CompleteAsync`. However, rather than returning the complete response as part of a `ChatCompletion` object, the method returns an `IAsyncEnumerable<StreamingChatCompletionUpdate>`, providing a stream of updates that together form the single response.

```csharp
using Microsoft.Extensions.AI;

IChatClient client = new SampleChatClient(new Uri("http://coolsite.ai"), "my-custom-model");

await foreach (var update in client.CompleteStreamingAsync("What is AI?"))
{
    Console.Write(update);
}
```

#### Tool calling

Some models and services support the notion of tool calling, where requests may include information about tools that the model may request be invoked in order to gather additional information, in particular functions. Rather than sending back a response message that represents the final response to the input, the model sends back a request to invoke a given function with a given set of arguments; the client may then find and invoke the relevant function and send back the results to the model (along with all the rest of the history). The abstractions in Microsoft.Extensions.AI include representations for various forms of content that may be included in messages, and this includes representations for these function call requests and results. While it's possible for the consumer of the `IChatClient` to interact with this content directly, `Microsoft.Extensions.AI` supports automating these interactions. It provides an `AIFunction` that represents an invocable function along with metadata for describing the function to the AI model, along with an `AIFunctionFactory` for creating `AIFunction`s to represent .NET methods. It also provides a `FunctionInvokingChatClient` that both is an `IChatClient` and also wraps an `IChatClient`, enabling layering automatic function invocation capabilities around an arbitrary `IChatClient` implementation.

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;

[Description("Gets the current weather")]
string GetCurrentWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";

IChatClient client = new ChatClientBuilder()
    .UseFunctionInvocation()
    .Use(new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1"));

var response = client.CompleteStreamingAsync(
    "Should I wear a rain coat?",
    new() { Tools = [AIFunctionFactory.Create(GetCurrentWeather)] });

await foreach (var update in response)
{
    Console.Write(update);
}
```

#### Caching

`Microsoft.Extensions.AI` provides other such delegating `IChatClient` implementations. The `DistributedCachingChatClient` is an `IChatClient` that layers caching around another arbitrary `IChatClient` instance. When a unique chat history that's not been seen before is submitted to the `DistributedCachingChatClient`, it forwards it along to the underlying client, and then caches the response prior to it being forwarded back to the consumer. The next time the same history is submitted, such that a cached response can be found in the cache, the `DistributedCachingChatClient` can return back the cached response rather than needing to forward the request along the pipeline.

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

IChatClient client = new ChatClientBuilder()
    .UseDistributedCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())))
    .Use(new SampleChatClient(new Uri("http://coolsite.ai"), "my-custom-model"));

string[] prompts = ["What is AI?", "What is .NET?", "What is AI?"];

foreach (var prompt in prompts)
{
    await foreach (var update in client.CompleteStreamingAsync(prompt))
    {
        Console.Write(update);
    }
    Console.WriteLine();
}
```

#### Telemetry

Other such delegating chat clients are provided as well. The `OpenTelemetryChatClient`, for example, provides an implementation of the [OpenTelemetry Semantic Conventions for Generative AI systems](https://opentelemetry.io/docs/specs/semconv/gen-ai/). As with the aforementioned `IChatClient` delegators, this implementation layers metrics and spans around other arbitrary `IChatClient` implementations.

```csharp
using Microsoft.Extensions.AI;
using OpenTelemetry.Trace;

// Configure OpenTelemetry exporter
var sourceName = Guid.NewGuid().ToString();
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

IChatClient client = new ChatClientBuilder()
    .UseOpenTelemetry(sourceName, c => c.EnableSensitiveData = true)
    .Use(new SampleChatClient(new Uri("http://coolsite.ai"), "my-custom-model"));

Console.WriteLine((await client.CompleteAsync("What is AI?")).Message);
```

#### Pipelines of Functionality

All of these `IChatClient`s may be layered, creating a pipeline of any number of components that all add additional functionality. Such components may come from `Microsoft.Extensions.AI`, may come from other NuGet packages, or may be your own custom implementations that augment the behavior in whatever ways you need.

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

// Configure OpenTelemetry exporter
var sourceName = Guid.NewGuid().ToString();
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

// Explore changing the order of the intermediate "Use" calls to see that impact
// that has on what gets cached, traced, etc.
IChatClient client = new ChatClientBuilder()
    .UseDistributedCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())))
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName, c => c.EnableSensitiveData = true)
    .Use(new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1"));

ChatOptions options = new()
{
    Tools = [AIFunctionFactory.Create(
        () => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining",
        name: "GetCurrentWeather", 
        description: "Gets the current weather")]
};

for (int i = 0; i < 3; i++)
{
    List<ChatMessage> history =
    [
        new ChatMessage(ChatRole.System, "You are a helpful AI assistant"),
        new ChatMessage(ChatRole.User, "Do I need an umbrella?")
    ];

    Console.WriteLine(await client.CompleteAsync(history, options));
}
```

#### Custom `IChatClient` Middleware

Anyone can layer in such additional functionality. While it's possible to implement `IChatClient` directly, the `DelegatingChatClient` class is an implementation of the `IChatClient` interface that serves as a base class for creating chat clients that delegate their operations to another `IChatClient` instance. It is designed to facilitate the chaining of multiple clients, allowing calls to be passed through to an underlying client. The class provides default implementations for methods such as `CompleteAsync`, `CompleteStreamingAsync`, and `Dispose`, simply forwarding the calls to the inner client instance. A derived type may then override just the methods it needs to in order to augment the behavior, delegating to the base implementation in order to forward the call along to the wrapped client. This setup is useful for creating flexible and modular chat clients that can be easily extended and composed.

Here is an example class derived from `DelegatingChatClient` to provide rate limiting functionality, utilizing the [System.Threading.RateLimiting](https://www.nuget.org/packages/System.Threading.RateLimiting) library:
```csharp
using Microsoft.Extensions.AI;
using System.Threading.RateLimiting;

public sealed class RateLimitingChatClient(IChatClient innerClient, RateLimiter rateLimiter) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var lease = await rateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Unable to acquire lease.");

        return await base.CompleteAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
    }

    public override async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var lease = await rateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Unable to acquire lease.");

        await foreach (var update in base.CompleteStreamingAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
            yield return update;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            rateLimiter.Dispose();

        base.Dispose(disposing);
    }
}
```

This can then be composed as with other `IChatClient` implementations.

```csharp
using Microsoft.Extensions.AI;
using System.Threading.RateLimiting;

var client = new RateLimitingChatClient(
    new SampleChatClient(new Uri("http://localhost"), "test"),
    new ConcurrencyLimiter(new() { PermitLimit = 1, QueueLimit = int.MaxValue }));

await client.CompleteAsync("What color is the sky?");
```

#### Dependency Injection

`IChatClient` implementations will typically be provided to an application via dependency injection (DI). In this example, an `IDistributedCache` is added into the DI container, as is an `IChatClient`. The registration for the `IChatClient` employs a builder that creates a pipeline containing a caching client (which will then use an `IDistributedCache` retrieved from DI) and the sample client. Elsewhere in the app, the injected `IChatClient` may be retrieved and used.

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// App Setup
var builder = Host.CreateApplicationBuilder();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddChatClient(b => b
    .UseDistributedCache()
    .Use(new SampleChatClient(new Uri("http://coolsite.ai"), "my-custom-model")));
var host = builder.Build();

// Elsewhere in the app
var chatClient = host.Services.GetRequiredService<IChatClient>();
Console.WriteLine(await chatClient.CompleteAsync("What is AI?"));
```

What instance and configuration is injected may differ based on the current needs of the application, and multiple pipelines may be injected with different keys.

### IEmbeddingGenerator

The `IEmbeddingGenerator<TInput,TEmbeddding>` interface represents a generic generator of embeddings, where `TInput` is the type of input values being embedded and `TEmbedding` is the type of generated embedding, inheriting from `Embedding`. 

The `Embedding` class provides a base class for embeddings generated by an `IEmbeddingGenerator`. This class is designed to store and manage the metadata and data associated with embeddings. Types derived from `Embedding`, like `Embedding<T>`, then provide the concrete embedding vector data. For example, an `Embedding<float>` exposes a `ReadOnlyMemory<float> Vector { get; }` property for access to its embedding data.

`IEmbeddingGenerator` defines a method to asynchronously generate embeddings for a collection of input values with optional configuration and cancellation support. Additionally, it provides metadata describing the generator and allows for the retrieval of strongly-typed services that may be provided by the generator or its underlying services. 

#### Sample Implementation

Here is a sample implementation of an `IEmbeddingGenerator` to show the general structure but that just generates random embedding vectors. You can find actual concrete implementations in the following packages:

- [Microsoft.Extensions.AI.OpenAI](https://aka.ms/meai-openai-nuget)
- [Microsoft.Extensions.AI.Ollama](https://aka.ms/meai-ollama-nuget)

```csharp
using Microsoft.Extensions.AI;

public class SampleEmbeddingGenerator(Uri endpoint, string modelId) : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata { get; } = new("SampleEmbeddingGenerator", endpoint, modelId);

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Simulate some async operation
        await Task.Delay(100, cancellationToken);

        // Create random embeddings
        return new GeneratedEmbeddings<Embedding<float>>(
            from value in values
            select new Embedding<float>(
                Enumerable.Range(0, 384).Select(_ => Random.Shared.NextSingle()).ToArray()));
    }

    public TService? GetService<TService>(object? key = null) where TService : class =>
        this as TService;

    void IDisposable.Dispose() { }
}
```

#### Creating an embedding: `GenerateAsync`

The primary operation performed with an `IEmbeddingGenerator` is generating embeddings, which is accomplished with its `GenerateAsync` method.

```csharp
using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new SampleEmbeddingGenerator(new Uri("http://coolsite.ai"), "my-custom-model");

foreach (var embedding in await generator.GenerateAsync(["What is AI?", "What is .NET?"]))
{
    Console.WriteLine(string.Join(", ", embedding.Vector.ToArray()));
}
```

#### Middleware

As with `IChatClient`, `IEmbeddingGenerator` implementations may be layered. Just as `Microsoft.Extensions.AI` provides delegating implementations of `IChatClient` for caching and telemetry, it does so for `IEmbeddingGenerator` as well.

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

// Configure OpenTelemetry exporter
var sourceName = Guid.NewGuid().ToString();
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

// Explore changing the order of the intermediate "Use" calls to see that impact
// that has on what gets cached, traced, etc.
IEmbeddingGenerator<string, Embedding<float>> generator = new EmbeddingGeneratorBuilder<string, Embedding<float>>()
    .UseDistributedCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())))
    .UseOpenTelemetry(sourceName)
    .Use(new SampleEmbeddingGenerator(new Uri("http://coolsite.ai"), "my-custom-model"));

var embeddings = await generator.GenerateAsync(
[
    "What is AI?",
    "What is .NET?",
    "What is AI?"
]);

foreach (var embedding in embeddings)
{
    Console.WriteLine(string.Join(", ", embedding.Vector.ToArray()));
}
```

Also as with `IChatClient`, `IEmbeddingGenerator` enables building custom middleware that extends the functionality of an `IEmbeddingGenerator`. The `DelegatingEmbeddingGenerator<TInput, TEmbedding>` class is an implementation of the `IEmbeddingGenerator<TInput, TEmbedding>` interface that serves as a base class for creating embedding generators which delegate their operations to another `IEmbeddingGenerator<TInput, TEmbedding>` instance. It allows for chaining multiple generators in any order, passing calls through to an underlying generator. The class provides default implementations for methods such as `GenerateAsync` and `Dispose`, which simply forward the calls to the inner generator instance, enabling flexible and modular embedding generation.

Here is an example implementation of such a delegating embedding generator that rate limits embedding generation requests:
```csharp
using Microsoft.Extensions.AI;
using System.Threading.RateLimiting;

public class RateLimitingEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator, RateLimiter rateLimiter) :
    DelegatingEmbeddingGenerator<string, Embedding<float>>(innerGenerator)
{
    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var lease = await rateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Unable to acquire lease.");

        return await base.GenerateAsync(values, options, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            rateLimiter.Dispose();

        base.Dispose(disposing);
    }
}
```

This can then be layered around an arbitrary `IEmbeddingGenerator<string, Embedding<float>>` to rate limit all embedding generation operations performed.

```csharp
using Microsoft.Extensions.AI;
using System.Threading.RateLimiting;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new RateLimitingEmbeddingGenerator(
        new SampleEmbeddingGenerator(new Uri("http://coolsite.ai"), "my-custom-model"),
        new ConcurrencyLimiter(new() { PermitLimit = 1, QueueLimit = int.MaxValue }));

foreach (var embedding in await generator.GenerateAsync(["What is AI?", "What is .NET?"]))
{
    Console.WriteLine(string.Join(", ", embedding.Vector.ToArray()));
}
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
