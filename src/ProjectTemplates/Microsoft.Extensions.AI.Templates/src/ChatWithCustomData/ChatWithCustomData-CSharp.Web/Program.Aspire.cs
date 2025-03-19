using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using ChatWithCustomData_CSharp.Web.Components;
using ChatWithCustomData_CSharp.Web.Services;
using ChatWithCustomData_CSharp.Web.Services.Ingestion;
#if (IsOllama)
#elif (IsGHModels)
using OpenAI;
using System.ClientModel;
#else // IsAzureOpenAI || IsOpenAI
using OpenAI;
#endif
#if (UseAzureAISearch)
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
#elif (UseQdrant)
using Microsoft.SemanticKernel.Connectors.Qdrant;
#endif

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

#if (IsGHModels)
var credential = new ApiKeyCredential(builder.Configuration["GITHUB_MODELS_TOKEN"] ?? throw new InvalidOperationException("Missing configuration: GITHUB_MODELS_TOKEN. See the README for details."));
var openAIOptions = new OpenAIClientOptions()
{
    Endpoint = new Uri("https://models.inference.ai.azure.com")
};

var ghModelsClient = new OpenAIClient(credential, openAIOptions);
var chatClient = ghModelsClient.AsChatClient("gpt-4o-mini");
var embeddingGenerator = ghModelsClient.AsEmbeddingGenerator("text-embedding-3-small");
#elif (IsOllama)
builder.AddOllamaApiClient("chat")
    .AddChatClient()
    .UseFunctionInvocation()
    .UseLogging();
builder.AddOllamaApiClient("embeddings")
    .AddEmbeddingGenerator();
#elif (IsAzureAiFoundry)

#else // IsAzureOpenAI || IsOpenAI
builder.AddOpenAIClientFromConfiguration("openai");
#endif

#if (UseAzureAISearch)
builder.AddAzureSearchClient("azureAISearch");

builder.Services.AddSingleton<IVectorStore, AzureAISearchVectorStore>();
#elif (UseQdrant)
builder.AddQdrantClient("vectordb");

builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
#else // UseLocalVectorStore
var vectorStore = new JsonVectorStore(Path.Combine(AppContext.BaseDirectory, "vector-store"));
builder.Services.AddSingleton<IVectorStore>(vectorStore);
#endif
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
#if (IsOllama)
#elif (IsGHModels)
builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
builder.Services.AddEmbeddingGenerator(embeddingGenerator);
#else // IsAzureOpenAI || IsOpenAI
builder.Services.AddChatClient(sp => sp.GetRequiredService<OpenAIClient>().AsChatClient("gpt-4o-mini"))
    .UseFunctionInvocation()
    .UseLogging();
builder.Services.AddEmbeddingGenerator(sp => sp.GetRequiredService<OpenAIClient>().AsEmbeddingGenerator("text-embedding-3-small"));
#endif

builder.AddSqliteDbContext<IngestionCacheDbContext>("ingestionCache");

var app = builder.Build();
IngestionCacheDbContext.Initialize(app.Services);

app.MapDefaultEndpoints();

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

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
await DataIngestor.IngestDataAsync(
    app.Services,
    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.Run();
