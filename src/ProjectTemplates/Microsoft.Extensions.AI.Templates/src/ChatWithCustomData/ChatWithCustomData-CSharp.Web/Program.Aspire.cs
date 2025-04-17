using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using ChatWithCustomData_CSharp.Web.Components;
using ChatWithCustomData_CSharp.Web.Services;
using ChatWithCustomData_CSharp.Web.Services.Ingestion;
#if (IsOllama)
#else // IsAzureOpenAI || IsOpenAI || IsGHModels
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

#if (IsOllama)
builder.AddOllamaApiClient("chat")
    .AddChatClient()
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());
builder.AddOllamaApiClient("embeddings")
    .AddEmbeddingGenerator();
#elif (IsAzureAiFoundry)
#else // (IsOpenAI || IsAzureOpenAI || IsGHModels)
#if (IsOpenAI)
var openai = builder.AddOpenAIClient("openai");
#else
var openai = builder.AddAzureOpenAIClient("openai");
#endif
openai.AddChatClient("gpt-4o-mini")
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());
openai.AddEmbeddingGenerator("text-embedding-3-small");
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
