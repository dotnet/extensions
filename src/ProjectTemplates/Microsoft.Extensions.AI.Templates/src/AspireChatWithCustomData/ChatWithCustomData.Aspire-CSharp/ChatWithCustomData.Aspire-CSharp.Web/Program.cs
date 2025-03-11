using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using ChatWithCustomData.Aspire_CSharp.Web.Components;
using ChatWithCustomData.Aspire_CSharp.Web.Services;
using ChatWithCustomData.Aspire_CSharp.Web.Services.Ingestion;
#if (IsOllama)
using OllamaSharp;
#else // AzureOpenAI
using Azure.AI.OpenAI;
using System.ClientModel;
#endif
#if (UseAzureAISearch)
using Azure.Search.Documents.Indexes;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
#else // UseQdrant
using Microsoft.SemanticKernel.Connectors.Qdrant;
#endif

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();


#if (IsOllama)
#else // AzureOpenAI
var azureOpenAi = new AzureOpenAIClient(
    new Uri(builder.Configuration["AZURE_OPENAI_ENDPOINT"] ?? throw new InvalidOperationException("Missing configuration: AZURE_OPENAI_ENDPOINT. See the README for details.")),
    new ApiKeyCredential(builder.Configuration["AZURE_OPENAI_KEY"] ?? throw new InvalidOperationException("Missing configuration: AZURE_OPENAI_KEY. See the README for details.")));
var chatClient = azureOpenAi.AsChatClient("gpt-4o-mini");
var embeddingGenerator = azureOpenAi.AsEmbeddingGenerator("text-embedding-3-small");
#endif

#if (UseAzureAISearch)
#else // UseQdrant
builder.AddQdrantClient("vectordb");
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
#endif

builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

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
