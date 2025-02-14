using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using ChatWithCustomData.Web.Components;
using ChatWithCustomData.Web.Services;
using ChatWithCustomData.Web.Services.Ingestion;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
#if(IsAzureOpenAI || UseAzureAISearch)
using Azure;
#if (UseManagedIdentity)
using Azure.Identity;
#else
using System.ClientModel;
#endif
#endif
#if (IsOllama)
using OllamaSharp;
#elif (IsOpenAI || IsGHModels)
using OpenAI;
using System.ClientModel;
#else
using Azure.AI.OpenAI;
#endif
#if (UseAzureAISearch)
using Azure.Search.Documents.Indexes;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
#endif

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

#if (IsOllama)
await ValidatePrerequisitesAsync(builder.Configuration);
#else
ValidatePrerequisites(builder.Configuration);
#endif

#if (IsGHModels)
// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set GitHubModels:Token YOUR-GITHUB-TOKEN
var ghToken = builder.Configuration["GitHubModels:Token"];

var credential = new ApiKeyCredential(ghToken);
var openAIOptions = new OpenAIClientOptions()
{
    Endpoint = new Uri("https://models.inference.ai.azure.com")
};

var ghModelsClient = new OpenAIClient(credential, openAIOptions);
var chatClient = ghModelsClient.AsChatClient("gpt-4o-mini");
var embeddingGenerator = ghModelsClient.AsEmbeddingGenerator("text-embedding-3-small");
#elif (IsOllama)
IChatClient chatClient = new OllamaApiClient(new Uri("http://localhost:11434"),
    "llama3.2");
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(new Uri("http://localhost:11434"),
    "all-minilm");
#elif (IsOpenAI)
// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set OpenAI:Key YOUR-API-KEY
var openAIClient = new OpenAIClient(
    new ApiKeyCredential(builder.Configuration["OpenAI:Key"] ?? throw new InvalidOperationException("Missing configuration: OpenAI:Key")));
var chatClient = openAIClient.AsChatClient("gpt-4o-mini");
var embeddingGenerator = openAIClient.AsEmbeddingGenerator("text-embedding-3-small");
#elif (IsAzureAiFoundry)

#else
// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set AzureOpenAi:Endpoint https://YOUR-DEPLOYMENT-NAME.openai.azure.com
#if (!UseManagedIdentity)
//   dotnet user-secrets set AzureOpenAi:Key YOUR-API-KEY
#endif
var azureOpenAi = new AzureOpenAIClient(
    new Uri(builder.Configuration["AzureOpenAi:Endpoint"] ?? throw new InvalidOperationException("Missing configuration: AzureOpenAi:Endpoint")),
#if (UseManagedIdentity)
    new DefaultAzureCredential());
#else
    new ApiKeyCredential(builder.Configuration["AzureOpenAi:Key"] ?? throw new InvalidOperationException("Missing configuration: AzureOpenAi:Key")));
#endif
var chatClient = azureOpenAi.AsChatClient("gpt-4o-mini");
var embeddingGenerator = azureOpenAi.AsEmbeddingGenerator("text-embedding-3-small");
#endif

#if (UseAzureAISearch)
// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set AzureAISearch:Endpoint https://YOUR-DEPLOYMENT-NAME.search.windows.net
//   dotnet user-secrets set AzureAISearch:Key YOUR-API-KEY
var vectorStore = new AzureAISearchVectorStore(
    new SearchIndexClient(
        new Uri(builder.Configuration["AzureAISearch:Endpoint"] ?? throw new InvalidOperationException("Missing configuration: AzureAISearch:Endpoint")),
#if (UseManagedIdentity)
        new DefaultAzureCredential()));
#else
        new AzureKeyCredential(builder.Configuration["AzureAISearch:Key"] ?? throw new InvalidOperationException("Missing configuration: AzureAISearch:Key"))));
#endif
#else
var vectorStore = new JsonVectorStore(Path.Combine(AppContext.BaseDirectory, "vector-store"));
#endif

builder.Services.AddSingleton<IVectorStore>(vectorStore);
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddChatClient(chatClient).UseFunctionInvocation();
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

// Serve any file in the /Data directory for the purpose of showing citations
// Caution: only place files in this directory that you want to be publicly accessible
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Data")),
    RequestPath = "/citation"
});

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await DataIngestor.IngestDataAsync(
    app.Services,
    new PDFDirectorySource(Path.Combine(builder.Environment.ContentRootPath, "Data")));

app.Run();

#if (IsOllama)
async Task ValidatePrerequisitesAsync(IConfiguration configuration)
#else
void ValidatePrerequisites(IConfiguration configuration)
#endif
{
#if (IsOllama)
    var client = new OllamaApiClient(new Uri("http://localhost:11434"));
    
    try
    {
        await client.IsRunningAsync();
     }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Failed to check if the Ollama API is running. Please make sure the Ollama service is started and accessible. See the README for details.", ex);
    }

    var installedModels = await client.ListRunningModelsAsync();
    var requiredModels = new[] { "llama2", "all-minilm" };
    var missingModels = requiredModels.Except(installedModels.Select(m => m.Name));

    if (missingModels.Any())
    {
        throw new InvalidOperationException(
            $"Required Ollama models are not installed: {string.Join(", ", missingModels)}. " +
            "Please install the missing models, by using your terminal and running: 'ollama pull <model>'. See the README for details.");
    }
#elif (IsOpenAI)
    if (string.IsNullOrEmpty(configuration["OpenAI:ApiKey"]))
    {
        throw new InvalidOperationException("Missing configuration: OpenAI:ApiKey. See the README for details.");
    }
#else
#if (UseManagedIdentity)
    if (string.IsNullOrEmpty(configuration["AzureOpenAI:Endpoint"]))
    {
        throw new InvalidOperationException("Missing configuration: AzureOpenAI:Endpoint. See the README for details.");
    }
#else
    if (string.IsNullOrEmpty(configuration["AzureOpenAI:Endpoint"]))
    {
        throw new InvalidOperationException("Missing configuration: AzureOpenAI:Endpoint. See the README for details.");
    }
    if (string.IsNullOrEmpty(configuration["AzureOpenAI:Key"]))
    {
        throw new InvalidOperationException("Missing configuration: AzureOpenAI:Key. See the README for details.");
    }
#endif
#endif
#if (UseAzureAISearch)

#if (UseManagedIdentity)
    if (string.IsNullOrEmpty(configuration["AzureAISearch:Endpoint"]))
    {
        throw new InvalidOperationException("Missing configuration: AzureAISearch:Endpoint. See the README for details.");
    }
#else
    if (string.IsNullOrEmpty(configuration["AzureAISearch:Endpoint"]))
    {
        throw new InvalidOperationException("Missing configuration: AzureAISearch:Endpoint. See the README for details.");
    }
    if (string.IsNullOrEmpty(configuration["AzureAISearch:Key"]))
    {
        throw new InvalidOperationException("Missing configuration: AzureAISearch:Key. See the README for details.");
    }
#endif
#endif
}