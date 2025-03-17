using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using aichatweb.Web.Components;
using aichatweb.Web.Services;
using aichatweb.Web.Services.Ingestion;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

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

var vectorStore = new JsonVectorStore(Path.Combine(AppContext.BaseDirectory, "vector-store"));

builder.Services.AddSingleton<IVectorStore>(vectorStore);
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
