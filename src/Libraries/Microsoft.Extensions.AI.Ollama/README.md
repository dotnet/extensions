# Microsoft.Extensions.AI.Ollama

Provides an implementation of the `IChatClient` interface for Ollama.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AI.Ollama
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI.Ollama" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Examples

### Chat

```csharp
using Microsoft.Extensions.AI;

IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1");

Console.WriteLine(await client.GetResponseAsync("What is AI?"));
```

### Chat + Conversation History

```csharp
using Microsoft.Extensions.AI;

IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1");

Console.WriteLine(await client.GetResponseAsync(
[
    new ChatMessage(ChatRole.System, "You are a helpful AI assistant"),
    new ChatMessage(ChatRole.User, "What is AI?"),
]));
```

### Chat Streaming

```csharp
using Microsoft.Extensions.AI;

IChatClient client = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1");

await foreach (var update in client.GetStreamingResponseAsync("What is AI?"))
{
    Console.Write(update);
}
```

### Tool Calling

Known limitations:

- Only a subset of models provided by Ollama support tool calling.
- Tool calling is currently not supported with streaming requests.

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;

IChatClient ollamaClient = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1");

IChatClient client = new ChatClientBuilder(ollamaClient)
    .UseFunctionInvocation()
    .Build();

ChatOptions chatOptions = new()
{
    Tools = [AIFunctionFactory.Create(GetWeather)]
};

Console.WriteLine(await client.GetResponseAsync("Do I need an umbrella?", chatOptions));

[Description("Gets the weather")]
static string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
```

### Caching

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

IChatClient ollamaClient = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1");

IChatClient client = new ChatClientBuilder(ollamaClient)
    .UseDistributedCache(cache)
    .Build();

for (int i = 0; i < 3; i++)
{
    await foreach (var message in client.GetStreamingResponseAsync("In less than 100 words, what is AI?"))
    {
        Console.Write(message);
    }

    Console.WriteLine();
    Console.WriteLine();
}
```

### Telemetry

```csharp
using Microsoft.Extensions.AI;
using OpenTelemetry.Trace;

// Configure OpenTelemetry exporter
var sourceName = Guid.NewGuid().ToString();
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

IChatClient ollamaClient = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1");

IChatClient client = new ChatClientBuilder(ollamaClient)
    .UseOpenTelemetry(sourceName, c => c.EnableSensitiveData = true)
    .Build();

Console.WriteLine(await client.GetResponseAsync("What is AI?"));
```

### Telemetry, Caching, and Tool Calling

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

// Configure telemetry
var sourceName = Guid.NewGuid().ToString();
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

// Configure caching
IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

// Configure tool calling
var chatOptions = new ChatOptions
{
    Tools = [AIFunctionFactory.Create(GetPersonAge)]
};

IChatClient ollamaClient = new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1");

IChatClient client = new ChatClientBuilder(ollamaClient)
    .UseDistributedCache(cache)
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName, c => c.EnableSensitiveData = true)
    .Build();

for (int i = 0; i < 3; i++)
{
    Console.WriteLine(await client.GetResponseAsync("How much older is Alice than Bob?", chatOptions));
}

[Description("Gets the age of a person specified by name.")]
static int GetPersonAge(string personName) =>
    personName switch
    {
        "Alice" => 42,
        "Bob" => 35,
        _ => 26,
    };
```

### Text embedding generation

```csharp
using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm");

var embeddings = await generator.GenerateAsync("What is AI?");

Console.WriteLine(string.Join(", ", embeddings[0].Vector.ToArray()));
```

### Text embedding generation with caching

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

IEmbeddingGenerator<string, Embedding<float>> ollamaGenerator =
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm");

IEmbeddingGenerator<string, Embedding<float>> generator = new EmbeddingGeneratorBuilder<string, Embedding<float>>(ollamaGenerator)
    .UseDistributedCache(cache)
    .Build();

foreach (var prompt in new[] { "What is AI?", "What is .NET?", "What is AI?" })
{
    var embeddings = await generator.GenerateAsync(prompt);

    Console.WriteLine(string.Join(", ", embeddings[0].Vector.ToArray()));
}
```

### Dependency Injection

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// App Setup
var builder = Host.CreateApplicationBuilder();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));

builder.Services.AddChatClient(new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1"))
    .UseDistributedCache()
    .UseLogging();

var app = builder.Build();

// Elsewhere in the app
var chatClient = app.Services.GetRequiredService<IChatClient>();
Console.WriteLine(await chatClient.GetResponseAsync("What is AI?"));
```

### Minimal Web API

```csharp
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChatClient(
    new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.1"));

builder.Services.AddEmbeddingGenerator(new OllamaEmbeddingGenerator(endpoint, "all-minilm"));

var app = builder.Build();

app.MapPost("/chat", async (IChatClient client, string message) =>
{
    var response = await client.GetResponseAsync(message, cancellationToken: default);
    return response.Message;
});

app.MapPost("/embedding", async (IEmbeddingGenerator<string,Embedding<float>> client, string message) =>
{
    var response = await client.GenerateAsync(message);
    return response[0].Vector;
});

app.Run();
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
