using Microsoft.Extensions.AI;
using ChatWithCustomData_CSharp.Web.Components;
using ChatWithCustomData_CSharp.Web.Services;
using ChatWithCustomData_CSharp.Web.Services.Ingestion;
#if (IsOllama)
#elif (IsOpenAI || IsGHModels)
using OpenAI;
#else // IsAzureOpenAI
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
builder.AddAzureSearchClient("search");
builder.Services.AddAzureAISearchCollection<IngestedChunk>("data-ChatWithCustomData-CSharp.Web-chunks");
builder.Services.AddAzureAISearchCollection<IngestedDocument>("data-ChatWithCustomData-CSharp.Web-documents");
#elif (UseQdrant)
builder.AddQdrantClient("vectordb");
builder.Services.AddQdrantCollection<Guid, IngestedChunk>("data-ChatWithCustomData-CSharp.Web-chunks");
builder.Services.AddQdrantCollection<Guid, IngestedDocument>("data-ChatWithCustomData-CSharp.Web-documents");
#else // UseLocalVectorStore
var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteCollection<string, IngestedChunk>("data-ChatWithCustomData-CSharp.Web-chunks", vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedDocument>("data-ChatWithCustomData-CSharp.Web-documents", vectorStoreConnectionString);
#endif
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
#if (IsOllama)
// Get the IHttpClientBuilder for chat_httpClient
var chatClientBuilder = builder.Services.AddHttpClient("chat_httpClient");
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental
chatClientBuilder.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
// Override resilience for this client
chatClientBuilder.AddStandardResilienceHandler(config =>
{
    // Extend the HTTP Client timeout for Ollama
    config.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
    // Must be at least double the AttemptTimeout to pass options validation
    config.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
    config.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(10);
});
chatClientBuilder.AddServiceDiscovery();
// Get the IHttpClientBuilder for embeddings_httpClient
var embeddingsClientBuilder = builder.Services.AddHttpClient("embeddings_httpClient");
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental
embeddingsClientBuilder.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
// Override resilience for this client
embeddingsClientBuilder.AddStandardResilienceHandler(config =>
{
    // Extend the HTTP Client timeout for Ollama
    config.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
    // Must be at least double the AttemptTimeout to pass options validation
    config.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
    config.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(10);
});
embeddingsClientBuilder.AddServiceDiscovery();
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

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
await DataIngestor.IngestDataAsync(
    app.Services,
    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.Run();
