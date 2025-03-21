using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using AspireAIChat.Web.Components;
using AspireAIChat.Web.Services;
using AspireAIChat.Web.Services.Ingestion;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

#if (IsOllama)
builder.AddOllamaApiClient("chat").AddChatClient()
    .UseFunctionInvocation()
    .UseLogging()
    .UseOpenTelemetry();
builder.AddOllamaApiClient("embeddings").AddEmbeddingGenerator()
    .UseLogging()
    .UseOpenTelemetry();
#endif

#if (UseAzureAISearch)
// TODO: Add support for Azure AI Search
#else
var vectorStore = new JsonVectorStore(Path.Combine(AppContext.BaseDirectory, "vector-store"));
#endif

builder.Services.AddSingleton<IVectorStore>(vectorStore);
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();

builder.AddSqliteDbContext<IngestionCacheDbContext>("ingestionCache");

var app = builder.Build();

app.MapDefaultEndpoints();
IngestionCacheDbContext.Initialize(app.Services);

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
