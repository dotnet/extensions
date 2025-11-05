using Microsoft.Extensions.AI;
#if (IsOpenAI || IsGHModels)
using OpenAI;
#endif
using ChatWithCustomData_CSharp.Web.Components;
using ChatWithCustomData_CSharp.Web.Services;
using ChatWithCustomData_CSharp.Web.Services.Ingestion;

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
#elif (IsAzureAIFoundry)
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

#if (IsAzureAISearch)
builder.AddAzureSearchClient("search");
builder.Services.AddAzureAISearchCollection<IngestedChunk>("data-ChatWithCustomData-CSharp.Web-chunks");
builder.Services.AddAzureAISearchCollection<IngestedDocument>("data-ChatWithCustomData-CSharp.Web-documents");
#elif (IsQdrant)
builder.AddQdrantClient("vectordb");
builder.Services.AddQdrantCollection<Guid, IngestedChunk>("data-ChatWithCustomData-CSharp.Web-chunks");
builder.Services.AddQdrantCollection<Guid, IngestedDocument>("data-ChatWithCustomData-CSharp.Web-documents");
#else // IsLocalVectorStore
var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteCollection<string, IngestedChunk>("data-ChatWithCustomData-CSharp.Web-chunks", vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedDocument>("data-ChatWithCustomData-CSharp.Web-documents", vectorStoreConnectionString);
#endif
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
#if (IsOllama)
// Applies robust HTTP resilience settings for all HttpClients in the Web project,
// not across the entire solution. It's aimed at supporting Ollama scenarios due
// to its self-hosted nature and potentially slow responses.
// Remove this if you want to use the global or a different HTTP resilience policy instead.
builder.Services.AddOllamaResilienceHandler();
#endif

var app = builder.Build();

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

// By default, we ingest Markdown files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
await DataIngestor.IngestDataAsync(
    app.Services,
    new MarkdownDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.Run();
