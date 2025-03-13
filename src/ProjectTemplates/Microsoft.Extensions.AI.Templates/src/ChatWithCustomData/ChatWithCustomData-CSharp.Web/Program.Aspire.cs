using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;
using ChatWithCustomData_CSharp.Web.Components;
using ChatWithCustomData_CSharp.Web.Services;
using ChatWithCustomData_CSharp.Web.Services.Ingestion;
#if (UseAzureAISearch) // TODO
#else // UseQdrant
using Microsoft.SemanticKernel.Connectors.Qdrant;
#endif

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

#if (IsOllama)
builder.AddOllamaApiClient("chat").AddChatClient()
    .UseFunctionInvocation()
    .UseLogging()
    .UseOpenTelemetry();
builder.AddOllamaApiClient("embeddings").AddEmbeddingGenerator()
    .UseLogging()
    .UseOpenTelemetry();
#else // IsAzureOpenAI
var azureOpenAI = builder.AddAzureOpenAIClient("openai");
azureOpenAI.AddChatClient("gpt-4o-mini")
    .UseFunctionInvocation()
    .UseLogging()
    .UseOpenTelemetry();
azureOpenAI.AddEmbeddingGenerator("text-embedding-3-small")
    .UseLogging()
    .UseOpenTelemetry();
#endif

#if (UseAzureAISearch) // TODO
#else // UseQdrant
builder.AddQdrantClient("vectordb");
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
#endif
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();

builder.Services.AddDbContext<IngestionCacheDbContext>(options =>
    options.UseSqlite("Data Source=ingestioncache.db"));

var app = builder.Build();
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
