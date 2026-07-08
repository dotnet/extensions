using System.ClientModel;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.AI;
using OpenAI;
using aichatweb.Components;
using aichatweb.Services;
using aichatweb.Services.Ingestion;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var chatAlias = builder.Configuration["FoundryLocal:ChatModel"] ?? "qwen3-4b";
var embeddingAlias = builder.Configuration["FoundryLocal:EmbeddingModel"] ?? "qwen3-embedding-0.6b";
var foundryServiceUrl = builder.Configuration["FoundryLocal:ServiceUrl"] ?? "http://127.0.0.1:5273";

await FoundryLocalManager.CreateAsync(new Configuration
{
    AppName = "aichatweb",
    Web = new Configuration.WebService { Urls = foundryServiceUrl }
}, NullLogger.Instance);
var foundryManager = FoundryLocalManager.Instance;
await foundryManager.StartWebServiceAsync();
var foundryCatalog = await foundryManager.GetCatalogAsync();

async Task<string> EnsureFoundryModelAsync(string modelAlias)
{
    var model = await foundryCatalog.GetModelAsync(modelAlias)
        ?? throw new InvalidOperationException(
            $"Foundry Local model '{modelAlias}' was not found in the catalog. Run 'foundry model list' to see available models.");
    if (!await model.IsCachedAsync())
    {
        Console.WriteLine($"Foundry Local: downloading model '{modelAlias}' (first run only)...");
        await model.DownloadAsync(_ => { });
    }
    if (!await model.IsLoadedAsync())
    {
        await model.LoadAsync();
    }
    return model.Id;
}

var chatModelId = await EnsureFoundryModelAsync(chatAlias);
var embeddingModelId = await EnsureFoundryModelAsync(embeddingAlias);

var foundryEndpointUrl = foundryManager.Urls?.FirstOrDefault() ?? foundryServiceUrl;
var foundryEndpoint = new Uri($"{foundryEndpointUrl.TrimEnd('/')}/v1");
var foundryClient = new OpenAIClient(
    new ApiKeyCredential("unused"),
    new OpenAIClientOptions { Endpoint = foundryEndpoint });
var chatClient = foundryClient.GetChatClient(chatModelId).AsIChatClient();
var embeddingGenerator = foundryClient.GetEmbeddingClient(embeddingModelId).AsIEmbeddingGenerator();

var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteVectorStore(_ => vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedChunk>(IngestedChunk.CollectionName, vectorStoreConnectionString);

builder.Services.AddSingleton<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddKeyedSingleton("ingestion_directory", new DirectoryInfo(Path.Combine(builder.Environment.WebRootPath, "Data")));
builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
