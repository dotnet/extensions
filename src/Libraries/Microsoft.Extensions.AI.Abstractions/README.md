# Microsoft.Extensions.AI.Abstractions

Provides abstractions representing generative AI components.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AI.Abstractions
```

or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

To also have access to higher-level utilities for working with such components, instead reference the [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI)
package. Libraries providing implementations of the abstractions will typically only reference `Microsoft.Extensions.AI.Abstractions`, whereas most consuming applications and services
will reference the `Microsoft.Extensions.AI` package (which itself references `Microsoft.Extensions.AI.Abstractions`) along with one or more libraries that provide concrete implementations
of the abstractions.

## Usage Examples

### `IChatClient`

The `IChatClient` interface defines a client abstraction responsible for interacting with AI services that provide "chat" capabilities. It defines methods for sending and receiving messages comprised of multi-modal content (text, images, audio, etc.), with responses providing either a complete result or one streamed incrementally. Additionally, it allows for retrieving strongly-typed services that may be provided by the client or its underlying services.

.NET libraries that provide clients for language models and services may provide an implementation of the `IChatClient` interface. Any consumers of the interface are then able to interoperate seamlessly with these models and services via the abstractions.

#### Requesting a Chat Response: `GetResponseAsync`

With an instance of `IChatClient`, the `GetResponseAsync` method may be used to send a request and get a response. The request is composed of one or more messages, each of which is composed of one or more pieces of content. Accelerator methods exist to simplify common cases, such as constructing a request for a single piece of text content.

```csharp
IChatClient client = ...;

Console.WriteLine(await client.GetResponseAsync("What is AI?"));
```

The core `GetResponseAsync` method on the `IChatClient` interface accepts a list of messages. This list often represents the history of all messages that are part of the conversation.

```csharp
IChatClient client = ...;

Console.WriteLine(await client.GetResponseAsync(
[
    new(ChatRole.System, "You are a helpful AI assistant"),
    new(ChatRole.User, "What is AI?"),
]));
```

The `ChatResponse` that's returned from `GetResponseAsync` exposes a list of `ChatMessage` instances representing one or more messages generated as part of the operation.
In common cases, there is only one response message, but a variety of situations can result in their being multiple; the list is ordered, such that the last message in
the list represents the final message to the request. In order to provide all of those response messages back to the service in a subsequent request, the messages from
the response may be added back into the messages list.

```csharp
List<ChatMessage> history = [];
while (true)
{
    Console.Write("Q: ");
    history.Add(new(ChatRole.User, Console.ReadLine()));

    var response = await client.GetResponseAsync(history);
    Console.WriteLine(response);

    history.AddMessages(response);
}
```

#### Requesting a Streaming Chat Response: `GetStreamingResponseAsync`

The inputs to `GetStreamingResponseAsync` are identical to those of `GetResponseAsync`. However, rather than returning the complete response as part of a
`ChatResponse` object, the method returns an `IAsyncEnumerable<ChatResponseUpdate>`, providing a stream of updates that together form the single response.

```csharp
IChatClient client = ...;

await foreach (var update in client.GetStreamingResponseAsync("What is AI?"))
{
    Console.Write(update);
}
```

As with `GetResponseAsync`, the updates from `IChatClient.GetStreamingResponseAsync` can be added back into the messages list. As the updates provided
are individual pieces of a response, helpers like `ToChatResponse` can be used to compose one or more updates back into a single `ChatResponse` instance.
Further helpers like `AddMessages` perform that same operation and then extract the composed messages from the response and add them into a list.

```csharp
List<ChatMessage> history = [];
while (true)
{
    Console.Write("Q: ");
    history.Add(new(ChatRole.User, Console.ReadLine()));

    List<ChatResponseUpdate> updates = [];
    await foreach (var update in client.GetStreamingResponseAsync(history))
    {
        Console.Write(update);
    }
    Console.WriteLine();

    history.AddMessages(updates);
}
```

#### Tool Calling

Some models and services support the notion of tool calling, where requests may include information about tools (in particular .NET methods) that the model may request be invoked in order to gather additional information. Rather than sending back a response message that represents the final response to the input, the model sends back a request to invoke a given function with a given set of arguments; the client may then find and invoke the relevant function and send back the results to the model (along with all the rest of the history). The abstractions in `Microsoft.Extensions.AI` include representations for various forms of content that may be included in messages, and this includes representations for these function call requests and results. While it's possible for the consumer of the `IChatClient` to interact with this content directly, `Microsoft.Extensions.AI` supports automating these interactions. It provides an `AIFunction` that represents an invocable function along with metadata for describing the function to the AI model, as well as an `AIFunctionFactory` for creating `AIFunction`s to represent .NET methods. It also provides a `FunctionInvokingChatClient` that both is an `IChatClient` and also wraps an `IChatClient`, enabling layering automatic function invocation capabilities around an arbitrary `IChatClient` implementation.

```csharp
using Microsoft.Extensions.AI;

string GetCurrentWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";

IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1")
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

ChatOptions options = new() { Tools = [AIFunctionFactory.Create(GetCurrentWeather)] };

var response = client.GetStreamingResponseAsync("Should I wear a rain coat?", options);
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

IChatClient client = new ChatClientBuilder(new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1"))
    .UseDistributedCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())))
    .Build();

string[] prompts = ["What is AI?", "What is .NET?", "What is AI?"];

foreach (var prompt in prompts)
{
    await foreach (var update in client.GetStreamingResponseAsync(prompt))
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

IChatClient client = new ChatClientBuilder(new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1"))
    .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = true)
    .Build();

Console.WriteLine(await client.GetResponseAsync("What is AI?"));
```

Alternatively, the `LoggingChatClient` and corresponding `UseLogging` method provide a simple way to write log entries to an `ILogger` for every request and response.

#### Options

Every call to `GetResponseAsync` or `GetStreamingResponseAsync` may optionally supply a `ChatOptions` instance containing additional parameters for the operation. The most common parameters that are common amongst AI models and services show up as strongly-typed properties on the type, such as `ChatOptions.Temperature`. Other parameters may be supplied by name in a weakly-typed manner via the `ChatOptions.AdditionalProperties` dictionary.

Options may also be baked into an `IChatClient` via the `ConfigureOptions` extension method on `ChatClientBuilder`. This delegating client wraps another client and invokes the supplied delegate to populate a `ChatOptions` instance for every call. For example, to ensure that the `ChatOptions.ModelId` property defaults to a particular model name, code like the following may be used:
```csharp
using Microsoft.Extensions.AI;

IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434"))
    .AsBuilder()
    .ConfigureOptions(options => options.ModelId ??= "phi3")
    .Build();

Console.WriteLine(await client.GetResponseAsync("What is AI?")); // will request "phi3"
Console.WriteLine(await client.GetResponseAsync("What is AI?", new() { ModelId = "llama3.1" })); // will request "llama3.1"
```

#### Pipelines of Chat Functionality

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

// Explore changing the order of the intermediate "Use" calls to see the impact
// that has on what gets cached, traced, etc.
IChatClient client = new ChatClientBuilder(new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1"))
    .UseDistributedCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())))
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = true)
    .Build();

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

    Console.WriteLine(await client.GetResponseAsync(history, options));
}
```

#### Custom `IChatClient` Middleware

Anyone can layer in such additional functionality. While it's possible to implement `IChatClient` directly, the `DelegatingChatClient` class is an implementation of the `IChatClient` interface that serves as a base class for creating chat clients that delegate their operations to another `IChatClient` instance. It is designed to facilitate the chaining of multiple clients, allowing calls to be passed through to an underlying client. The class provides default implementations for methods such as `GetResponseAsync`, `GetStreamingResponseAsync`, and `Dispose`, simply forwarding the calls to the inner client instance. A derived type may then override just the methods it needs to in order to augment the behavior, delegating to the base implementation in order to forward the call along to the wrapped client. This setup is useful for creating flexible and modular chat clients that can be easily extended and composed.

Here is an example class derived from `DelegatingChatClient` to provide rate limiting functionality, utilizing the [System.Threading.RateLimiting](https://www.nuget.org/packages/System.Threading.RateLimiting) library:
```csharp
using Microsoft.Extensions.AI;
using System.Threading.RateLimiting;

public sealed class RateLimitingChatClient(IChatClient innerClient, RateLimiter rateLimiter) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var lease = await rateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Unable to acquire lease.");

        return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var lease = await rateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Unable to acquire lease.");

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
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
    new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1"),
    new ConcurrencyLimiter(new() { PermitLimit = 1, QueueLimit = int.MaxValue }));

Console.WriteLine(await client.GetResponseAsync("What color is the sky?"));
```

To make it easier to compose such components with others, the author of the component is recommended to create a "Use" extension method for registering this component into a pipeline, e.g.
```csharp
public static class RateLimitingChatClientExtensions
{
    public static ChatClientBuilder UseRateLimiting(this ChatClientBuilder builder, RateLimiter rateLimiter) =>
        builder.Use(innerClient => new RateLimitingChatClient(innerClient, rateLimiter));
}
```

Such extensions may also query for relevant services from the DI container; the `IServiceProvider` used by the pipeline is passed in as an optional parameter:
```csharp
public static class RateLimitingChatClientExtensions
{
    public static ChatClientBuilder UseRateLimiting(this ChatClientBuilder builder, RateLimiter? rateLimiter = null) =>
        builder.Use((innerClient, services) => new RateLimitingChatClient(innerClient, services.GetRequiredService<RateLimiter>()));
}
```

The consumer can then easily use this in their pipeline, e.g.
```csharp
var client = new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1")
    .AsBuilder()
    .UseDistributedCache()
    .UseRateLimiting()
    .UseOpenTelemetry()
    .Build(services);
```

The above extension methods demonstrate using a `Use` method on `ChatClientBuilder`. `ChatClientBuilder` also provides `Use` overloads that make it easier to
write such delegating handlers. For example, in the earlier `RateLimitingChatClient` example, the overrides of `GetResponseAsync` and `GetStreamingResponseAsync` only
need to do work before and after delegating to the next client in the pipeline. To achieve the same thing without writing a custom class, an overload of `Use` may
be used that accepts a delegate which is used for both `GetResponseAsync` and `GetStreamingResponseAsync`, reducing the boilderplate required:
```csharp
RateLimiter rateLimiter = ...;
var client = new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1")
    .AsBuilder()
    .UseDistributedCache()
    .Use(async (messages, options, nextAsync, cancellationToken) =>
    {
        using var lease = await rateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Unable to acquire lease.");

        await nextAsync(messages, options, cancellationToken);
    })
    .UseOpenTelemetry()
    .Build();
```

For scenarios where the developer would like to specify delegating implementations of `GetResponseAsync` and `GetStreamingResponseAsync` inline,
and where it's important to be able to write a different implementation for each in order to handle their unique return types specially,
another overload of `Use` exists that accepts a delegate for each.

#### Dependency Injection

While not required, `IChatClient` implementations will often be provided to an application via dependency injection (DI). In this example, an `IDistributedCache` is added into the DI container, as is an `IChatClient`. The registration for the `IChatClient` employs a builder that creates a pipeline containing a caching client (which will then use an `IDistributedCache` retrieved from DI) and the sample client. Elsewhere in the app, the injected `IChatClient` may be retrieved and used.

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// App Setup
var builder = Host.CreateApplicationBuilder();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddChatClient(new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.1"))
    .UseDistributedCache();
var host = builder.Build();

// Elsewhere in the app
var chatClient = host.Services.GetRequiredService<IChatClient>();
Console.WriteLine(await chatClient.GetResponseAsync("What is AI?"));
```

What instance and configuration is injected may differ based on the current needs of the application, and multiple pipelines may be injected with different keys.

#### Stateless vs Stateful Clients

"Stateless" services require all relevant conversation history to sent back on every request, while "stateful" services keep track of the history and instead
require only additional messages be sent with a request. The `IChatClient` interface is designed to handle both stateless and stateful AI services.

When working with a stateless service, callers maintain a list of all messages, adding in all received response messages, and providing the list
back on subsequent interactions.
```csharp
List<ChatMessage> history = [];
while (true)
{
    Console.Write("Q: ");
    history.Add(new(ChatRole.User, Console.ReadLine()));

    var response = await client.GetResponseAsync(history);
    Console.WriteLine(response);

    history.AddMessages(response);
}
```

For stateful services, you may know ahead of time an identifier used for the relevant conversation. That identifier can be put into `ChatOptions.ConversationId`.
Usage then follows the same pattern, except there's no need to maintain a history manually.
```csharp
ChatOptions options = new() { ConversationId = "my-conversation-id" };
while (true)
{
    Console.Write("Q: ");
    ChatMessage message = new(ChatRole.User, Console.ReadLine());

    Console.WriteLine(await client.GetResponseAsync(message, options));
}
```

Some services may support automatically creating a thread ID for a request that doesn't have one. In such cases, you can transfer the `ChatResponse.ConversationId` over
to the `ChatOptions.ConversationId` for subsequent requests, e.g.
```csharp
ChatOptions options = new();
while (true)
{
    Console.Write("Q: ");
    ChatMessage message = new(ChatRole.User, Console.ReadLine());

    ChatResponse response = await client.GetResponseAsync(message, options);
    Console.WriteLine(response);

    options.ConversationId = response.ConversationId;
}
```

If you don't know ahead of time whether the service is stateless or stateful, both can be accomodated by checking the response `ConversationId`
and acting based on its value. Here, if the response `ConversationId` is set, then that value is propagated to the options and the history
cleared so as to not resend the same history again. If, however, the `ConversationId` is not set, then the response message is added to the
history so that it's sent back to the service on the next turn.
```csharp
List<ChatMessage> history = [];
ChatOptions options = new();
while (true)
{
    Console.Write("Q: ");
    history.Add(new(ChatRole.User, Console.ReadLine()));

    ChatResponse response = await client.GetResponseAsync(history);
    Console.WriteLine(response);

    options.ConversationId = response.ConversationId;
    if (response.ConversationId is not null)
    {
        history.Clear();
    }
    else
    {
        history.AddMessages(response);
    }
}
```

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
    private readonly EmbeddingGeneratorMetadata _metadata = new("SampleEmbeddingGenerator", endpoint, modelId);

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

    object? IChatClient.GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is not null ? null :
        serviceType == typeof(EmbeddingGeneratorMetadata) ? _metadata :
        serviceType?.IsInstanceOfType(this) is true ? this :
        null;

    void IDisposable.Dispose() { }
}
```

#### Creating an Embedding: `GenerateAsync`

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

Accelerator extension methods also exist to simplify common cases, such as generating an embedding vector from a single input, e.g.
```csharp
using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new SampleEmbeddingGenerator(new Uri("http://coolsite.ai"), "my-custom-model");

ReadOnlyMemory<float> vector = generator.GenerateVectorAsync("What is AI?");
```

#### Pipelines of Functionality

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
var generator = new EmbeddingGeneratorBuilder<string, Embedding<float>>(
        new SampleEmbeddingGenerator(new Uri("http://coolsite.ai"), "my-custom-model"))
    .UseDistributedCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())))
    .UseOpenTelemetry(sourceName: sourceName)
    .Build();

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
