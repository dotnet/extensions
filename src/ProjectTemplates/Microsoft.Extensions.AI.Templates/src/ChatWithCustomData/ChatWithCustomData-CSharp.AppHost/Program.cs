var builder = DistributedApplication.CreateBuilder(args);
#if (IsOllama) // ASPIRE PARAMETERS
#elif (IsOpenAI)

// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set Parameters:openAIKey YOUR-API-KEY
var openAIKey = builder.AddParameter("openAIKey", secret: true);
#elif (IsGHModels)

// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set Parameters:gitHubModelsToken YOUR-GITHUB-TOKEN
var gitHubModelsToken = builder.AddParameter("gitHubModelsToken", secret: true);
#else // IsAzureOpenAI

// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set Parameters:azureOpenAIEndpoint https://YOUR-DEPLOYMENT-NAME.openai.azure.com
//   dotnet user-secrets set Parameters:azureOpenAIKey YOUR-API-KEY
var azureOpenAIEndpoint = builder.AddParameter("azureOpenAIEndpoint", secret: true);
var azureOpenAIKey = builder.AddParameter("azureOpenAIKey", secret: true);
#endif
#if (UseAzureAISearch)

// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set Parameters:azureAISearchEndpoint https://YOUR-DEPLOYMENT-NAME.search.windows.net
//   dotnet user-secrets set Parameters:azureAISearchKey YOUR-API-KEY
var azureAISearchEndpoint = builder.AddParameter("azureAISearchEndpoint", secret: true);
var azureAISearchKey = builder.AddParameter("azureAISearchKey", secret: true);
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
#elif (IsOpenAI)
webApp.WithEnvironment("OPENAI_KEY", openAIKey);
#elif (IsGHModels)
webApp.WithEnvironment("GITHUB_MODELS_TOKEN", gitHubModelsToken);
#else // IsAzureOpenAI
webApp
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", azureOpenAIEndpoint)
    .WithEnvironment("AZURE_OPENAI_KEY", azureOpenAIKey);
#endif
#if (UseAzureAISearch) // VECTOR DATABASE REFERENCES
webApp
    .WithEnvironment("AZURE_AI_SEARCH_ENDPOINT", azureAISearchEndpoint)
    .WithEnvironment("AZURE_AI_SEARCH_KEY", azureAISearchKey);
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
