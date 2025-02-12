# Microsoft.Extensions.AI.OpenAI

Provides an implementation of the `IChatClient` interface for the `OpenAI` package and OpenAI-compatible endpoints.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AI.OpenAI
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Examples

### Chat

```csharp
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient client =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsChatClient("gpt-4o-mini");

Console.WriteLine(await client.GetResponseAsync("What is AI?"));
```

### Chat + Conversation History

```csharp
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient client =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsChatClient("gpt-4o-mini");

Console.WriteLine(await client.GetResponseAsync(
[
    new ChatMessage(ChatRole.System, "You are a helpful AI assistant"),
    new ChatMessage(ChatRole.User, "What is AI?"),
]));
```

### Chat streaming

```csharp
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient client =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsChatClient("gpt-4o-mini");

await foreach (var update in client.GetStreamingResponseAsync("What is AI?"))
{
    Console.Write(update);
}
```

### Tool calling

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient openaiClient =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder(openaiClient)
    .UseFunctionInvocation()
    .Build();

ChatOptions chatOptions = new()
{
    Tools = [AIFunctionFactory.Create(GetWeather)]
};

await foreach (var message in client.GetStreamingResponseAsync("Do I need an umbrella?", chatOptions))
{
    Console.Write(message);
}

[Description("Gets the weather")]
static string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
```

### Caching

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenAI;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

IChatClient openaiClient =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder(openaiClient)
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
using OpenAI;
using OpenTelemetry.Trace;

// Configure OpenTelemetry exporter
var sourceName = Guid.NewGuid().ToString();
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

IChatClient openaiClient =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder(openaiClient)
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
using OpenAI;
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

IChatClient openaiClient =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder(openaiClient)
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
using OpenAI;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsEmbeddingGenerator("text-embedding-3-small");

var embeddings = await generator.GenerateAsync("What is AI?");

Console.WriteLine(string.Join(", ", embeddings[0].Vector.ToArray()));
```

### Text embedding generation with caching

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenAI;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

IEmbeddingGenerator<string, Embedding<float>> openAIGenerator =
    new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsEmbeddingGenerator("text-embedding-3-small");

IEmbeddingGenerator<string, Embedding<float>> generator = new EmbeddingGeneratorBuilder<string, Embedding<float>>(openAIGenerator)
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
using OpenAI;

// App Setup
var builder = Host.CreateApplicationBuilder();
builder.Services.AddSingleton(new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));

builder.Services.AddChatClient(services => services.GetRequiredService<OpenAIClient>().AsChatClient("gpt-4o-mini"))
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
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new OpenAIClient(builder.Configuration["OPENAI_API_KEY"]));

builder.Services.AddChatClient(services =>
    services.GetRequiredService<OpenAIClient>().AsChatClient("gpt-4o-mini"));

builder.Services.AddEmbeddingGenerator(services =>
    services.GetRequiredService<OpenAIClient>().AsEmbeddingGenerator("text-embedding-3-small"));

var app = builder.Build();

app.MapPost("/chat", async (IChatClient client, string message) =>
{
    var response = await client.GetResponseAsync(message);
    return response.Message;
});

app.MapPost("/embedding", async (IEmbeddingGenerator<string, Embedding<float>> client, string message) =>
{
    var response = await client.GenerateAsync(message);
    return response[0].Vector;
});

app.Run();
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
