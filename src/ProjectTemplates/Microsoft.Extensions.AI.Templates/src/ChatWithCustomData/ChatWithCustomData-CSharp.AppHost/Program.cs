var builder = DistributedApplication.CreateBuilder(args);

#if (UseAzureAISearch || UseLocalVectorStore)
#else // UseQdrant
var qdrantApiKey = builder.AddParameter("qdrantapikey", secret: true);
#endif

#if (IsOllama)
var ollama = builder.AddOllama("ollama")
    .WithOpenWebUI()
    .WithDataVolume();
var chat = ollama.AddModel("chat", "llama3.2");
var embeddings = ollama.AddModel("embeddings", "all-minilm");
#endif

#if (UseAzureAISearch || UseLocalVectorStore)
#else // UseQdrant
var vectorDB = builder.AddQdrant("vectordb", apiKey: qdrantApiKey, grpcPort: 6334, httpPort: 6333)
    .WithDataBindMount("./qdrant_data")
    .WithLifetime(ContainerLifetime.Persistent);
#endif

var webApp = builder.AddProject<Projects.ChatWithCustomData_CSharp_Web>("aichatweb-app");
#if (IsOllama)
webApp
    .WithReference(chat)
    .WithReference(embeddings)
    .WaitFor(chat)
    .WaitFor(embeddings);
#endif
#if (UseAzureAISearch || UseLocalVectorStore)
#else // UseQdrant
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB);
#endif

builder.Build().Run();
