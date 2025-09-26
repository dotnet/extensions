var builder = DistributedApplication.CreateBuilder(args);
#if (IsOllama) // ASPIRE PARAMETERS
#elif (IsOpenAI || IsGHModels || IsAzureOpenAI)

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
#if (IsOpenAI)
//   dotnet user-secrets set ConnectionStrings:openai "Key=YOUR-API-KEY"
#elif (IsGHModels)
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key=YOUR-API-KEY"
#else // IsAzureOpenAI
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://YOUR-DEPLOYMENT-NAME.openai.azure.com/openai/v1;Key=YOUR-API-KEY"
#endif
var openai = builder.AddConnectionString("openai");
#endif
#if (UseAzureAISearch)

// See https://learn.microsoft.com/dotnet/aspire/azure/local-provisioning#configuration
// for instructions providing configuration values
var search = builder.AddAzureSearch("search");
#endif
#if (IsOllama) // AI SERVICE PROVIDER CONFIGURATION

var ollama = builder.AddOllama("ollama")
    .WithDataVolume();
var chat = ollama.AddModel("chat", "llama3.2");
var embeddings = ollama.AddModel("embeddings", "all-minilm");
#endif
#if (UseAzureAISearch) // VECTOR DATABASE CONFIGURATION
#elif (UseQdrant)

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
#else // UseLocalVectorStore
#endif

var webApp = builder.AddProject<Projects.ChatWithCustomData_CSharp_Web_AspireClassName_Web>("aichatweb-app");
#if (IsOllama) // AI SERVICE PROVIDER REFERENCES
webApp
    .WithReference(chat)
    .WithReference(embeddings)
    .WaitFor(chat)
    .WaitFor(embeddings);
#elif (IsOpenAI || IsGHModels || IsAzureOpenAI)
webApp.WithReference(openai);
#endif
#if (UseAzureAISearch) // VECTOR DATABASE REFERENCES
webApp
    .WithReference(search)
    .WaitFor(search);
#elif (UseQdrant)
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB);
#else // UseLocalVectorStore
#endif

builder.Build().Run();
