using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;
using ChatWithCustomData_CSharp.Web.Components;
using ChatWithCustomData_CSharp.Web.Services;
using ChatWithCustomData_CSharp.Web.Services.Ingestion;
#if(UseAzureAISearch)
using Azure;
#if (UseManagedIdentity)
using Azure.Identity;
#else
using System.ClientModel;
#endif
#endif
#if (IsOllama)
#elif (IsOpenAI || IsGHModels)
using OpenAI;
using System.ClientModel;
#else
using Azure.AI.OpenAI;
using System.ClientModel;
#endif
#if (UseAzureAISearch)
using Azure.Search.Documents.Indexes;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
#elif (UseLocalVectorStore)
#else // UseQdrant
using Microsoft.SemanticKernel.Connectors.Qdrant;
#endif

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

#if (IsGHModels)
// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set GitHubModels:Token YOUR-GITHUB-TOKEN
var credential = new ApiKeyCredential(builder.Configuration["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token. See the README for details."));
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
#elif (IsOpenAI)
// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set OpenAI:Key YOUR-API-KEY
var openAIClient = new OpenAIClient(
    new ApiKeyCredential(builder.Configuration["OpenAI:Key"] ?? throw new InvalidOperationException("Missing configuration: OpenAI:Key. See the README for details.")));
var chatClient = openAIClient.AsChatClient("gpt-4o-mini");
var embeddingGenerator = openAIClient.AsEmbeddingGenerator("text-embedding-3-small");
#elif (IsAzureAiFoundry)

#else
// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set AzureOpenAI:Endpoint https://YOUR-DEPLOYMENT-NAME.openai.azure.com
#if (!UseManagedIdentity)
//   dotnet user-secrets set AzureOpenAI:Key YOUR-API-KEY
#endif
var azureOpenAi = new AzureOpenAIClient(
    new Uri(builder.Configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Missing configuration: AzureOpenAi:Endpoint. See the README for details.")),
#if (UseManagedIdentity)
    new DefaultAzureCredential());
#else
    new ApiKeyCredential(builder.Configuration["AzureOpenAI:Key"] ?? throw new InvalidOperationException("Missing configuration: AzureOpenAi:Key. See the README for details.")));
#endif
var chatClient = azureOpenAi.AsChatClient("gpt-4o-mini");
var embeddingGenerator = azureOpenAi.AsEmbeddingGenerator("text-embedding-3-small");
#endif

#if (UseAzureAISearch)
// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set AzureAISearch:Endpoint https://YOUR-DEPLOYMENT-NAME.search.windows.net
#if (!UseManagedIdentity)
//   dotnet user-secrets set AzureAISearch:Key YOUR-API-KEY
#endif
var vectorStore = new AzureAISearchVectorStore(
    new SearchIndexClient(
        new Uri(builder.Configuration["AzureAISearch:Endpoint"] ?? throw new InvalidOperationException("Missing configuration: AzureAISearch:Endpoint. See the README for details.")),
#if (UseManagedIdentity)
        new DefaultAzureCredential()));
#else
        new AzureKeyCredential(builder.Configuration["AzureAISearch:Key"] ?? throw new InvalidOperationException("Missing configuration: AzureAISearch:Key. See the README for details."))));
#endif
#elif (UseLocalVectorStore)
var vectorStore = new JsonVectorStore(Path.Combine(AppContext.BaseDirectory, "vector-store"));
#else // UseQdrant
builder.AddQdrantClient("vectordb");
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
#endif

#if (UseAzureAISearch || UseLocalVectorStore)
builder.Services.AddSingleton<IVectorStore>(vectorStore);
#endif
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
#if (IsOllama)
#else
builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
builder.Services.AddEmbeddingGenerator(embeddingGenerator);
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
