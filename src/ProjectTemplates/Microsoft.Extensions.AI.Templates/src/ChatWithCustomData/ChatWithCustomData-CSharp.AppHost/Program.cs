var builder = DistributedApplication.CreateBuilder(args);
#if (IsOllama) // ASPIRE PARAMETERS
#elif (IsGHModels)

// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set Parameters:gitHubModelsToken YOUR-GITHUB-TOKEN
var gitHubModelsToken = builder.AddParameter("gitHubModelsToken", secret: true);
#else // IsAzureOpenAI || IsOpenAI

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
#if (IsOpenAI)
//   dotnet user-secrets set ConnectionStrings:openai Key=YOUR-API-KEY
#else // IsAzureOpenAI
//   dotnet user-secrets set ConnectionStrings:openai Endpoint=https://YOUR-DEPLOYMENT-NAME.openai.azure.com;Key=YOUR-API-KEY
#endif
var openai = builder.AddConnectionString("openai");
#endif
#if (UseAzureAISearch)

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:azureAISearch Endpoint=https://YOUR-DEPLOYMENT-NAME.search.windows.net;Key=YOUR-API-KEY
var azureAISearch = builder.AddConnectionString("azureAISearch");
#elif (UseQdrant)

var qdrantApiKey = builder.AddParameter("qdrantApiKey", secret: true);
#else // UseLocalVectorStore
#endif
#if (IsOllama) // AI SERVICE PROVIDER CONFIGURATION

var ollama = builder.AddOllama("ollama")
    .WithDataVolume();
var chat = ollama.AddModel("chat", "llama3.2");
var embeddings = ollama.AddModel("embeddings", "all-minilm");
#endif
#if (UseAzureAISearch) // VECTOR DATABASE CONFIGURATION
#elif (UseQdrant)

var vectorDB = builder.AddQdrant("vectordb", apiKey: qdrantApiKey, grpcPort: 6334, httpPort: 6333)
    .WithDataBindMount("./qdrant_data")
    .WithLifetime(ContainerLifetime.Persistent);
#else // UseLocalVectorStore
#endif

var ingestionCache = builder.AddSqlite("ingestionCache")
    .WithSqliteWeb();

var webApp = builder.AddProject<Projects.ChatWithCustomData_CSharp_Web>("aichatweb-app");
#if (IsOllama) // AI SERVICE PROVIDER REFERENCES
webApp
    .WithReference(chat)
    .WithReference(embeddings)
    .WaitFor(chat)
    .WaitFor(embeddings);
#elif (IsGHModels)
webApp.WithEnvironment("GITHUB_MODELS_TOKEN", gitHubModelsToken);
#else // IsAzureOpenAI || IsOpenAI
webApp.WithReference(openai);
#endif
#if (UseAzureAISearch) // VECTOR DATABASE REFERENCES
webApp.WithReference(azureAISearch);
#elif (UseQdrant)
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB);
#else // UseLocalVectorStore
#endif
webApp
    .WithReference(ingestionCache)
    .WaitFor(ingestionCache);

builder.Build().Run();
