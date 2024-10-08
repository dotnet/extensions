# Microsoft.Extensions.AI.AzureAIInference

Provides an implementation of the `IChatClient` interface for the `Azure.AI.Inference` package.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AI.AzureAIInference
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI.AzureAIInference" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Examples

### Chat

```csharp
using Azure;
using Microsoft.Extensions.AI;

IChatClient client =
    new Azure.AI.Inference.ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")!))
    .AsChatClient("gpt-4o-mini");

Console.WriteLine(await client.CompleteAsync("What is AI?"));
```

### Chat + Conversation History

```csharp
using Azure;
using Microsoft.Extensions.AI;

IChatClient client =
    new Azure.AI.Inference.ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")!))
    .AsChatClient("gpt-4o-mini");

Console.WriteLine(await client.CompleteAsync(
[
    new ChatMessage(ChatRole.System, "You are a helpful AI assistant"),
    new ChatMessage(ChatRole.User, "What is AI?"),
]));
```

### Chat streaming

```csharp
using Azure;
using Microsoft.Extensions.AI;

IChatClient client =
    new Azure.AI.Inference.ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")!))
    .AsChatClient("gpt-4o-mini");

await foreach (var update in client.CompleteStreamingAsync("What is AI?"))
{
    Console.Write(update);
}
```

### Tool calling

```csharp
using System.ComponentModel;
using Azure;
using Microsoft.Extensions.AI;

IChatClient azureClient =
    new Azure.AI.Inference.ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")!))
    .AsChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder()
    .UseFunctionInvocation()
    .Use(azureClient);

ChatOptions chatOptions = new()
{
    Tools = [AIFunctionFactory.Create(GetWeather)]
};

await foreach (var message in client.CompleteStreamingAsync("Do I need an umbrella?", chatOptions))
{
    Console.Write(message);
}

[Description("Gets the weather")]
static string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
```

### Caching

```csharp
using Azure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

IChatClient azureClient =
    new Azure.AI.Inference.ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")!))
    .AsChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder()
    .UseDistributedCache(cache)
    .Use(azureClient);

for (int i = 0; i < 3; i++)
{
    await foreach (var message in client.CompleteStreamingAsync("In less than 100 words, what is AI?"))
    {
        Console.Write(message);
    }

    Console.WriteLine();
    Console.WriteLine();
}
```

### Telemetry

```csharp
using Azure;
using Microsoft.Extensions.AI;
using OpenTelemetry.Trace;

// Configure OpenTelemetry exporter
var sourceName = Guid.NewGuid().ToString();
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

IChatClient azureClient =
    new Azure.AI.Inference.ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")!))
    .AsChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder()
    .UseOpenTelemetry(sourceName, c => c.EnableSensitiveData = true)
    .Use(azureClient);

Console.WriteLine(await client.CompleteAsync("What is AI?"));
```

### Telemetry, Caching, and Tool Calling

```csharp
using System.ComponentModel;
using Azure;
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

IChatClient azureClient =
    new Azure.AI.Inference.ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")!))
    .AsChatClient("gpt-4o-mini");

IChatClient client = new ChatClientBuilder()
    .UseDistributedCache(cache)
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName, c => c.EnableSensitiveData = true)
    .Use(azureClient);

for (int i = 0; i < 3; i++)
{
    Console.WriteLine(await client.CompleteAsync("How much older is Alice than Bob?", chatOptions));
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

### Dependency Injection

```csharp
using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// App Setup
var builder = Host.CreateApplicationBuilder();
builder.Services.AddSingleton(
    new ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")!)));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));

builder.Services.AddChatClient(b => b
    .UseDistributedCache()
    .UseLogging()
    .Use(b.Services.GetRequiredService<ChatCompletionsClient>().AsChatClient("gpt-4o-mini")));

var app = builder.Build();

// Elsewhere in the app
var chatClient = app.Services.GetRequiredService<IChatClient>();
Console.WriteLine(await chatClient.CompleteAsync("What is AI?"));
```

### Minimal Web API

```csharp
using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new ChatCompletionsClient(
        new("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(builder.Configuration["GH_TOKEN"]!)));

builder.Services.AddChatClient(b =>
    b.Use(b.Services.GetRequiredService<ChatCompletionsClient>().AsChatClient("gpt-4o-mini")));

var app = builder.Build();

app.MapPost("/chat", async (IChatClient client, string message) =>
{
    var response = await client.CompleteAsync(message);
    return response.Message;
});

app.Run();
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
