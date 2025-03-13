var builder = DistributedApplication.CreateBuilder(args);

#if (IsOllama)
#else // IsAzureOpenAI
var azureOpenAIName = builder.AddParameter("azureOpenAIName");
var azureOpenAIResourceGroup = builder.AddParameter("azureOpenAIResourceGroup");
#endif

#if (UseAzureAISearch) // TODO
#else // UseQdrant
var qdrantApiKey = builder.AddParameter("qdrantapikey", secret: true);
#endif

#if (IsOllama)
var ollama = builder.AddOllama("ollama")
    .WithOpenWebUI()
    .WithDataVolume();
var chat = ollama.AddModel("chat", "llama3.2");
var embeddings = ollama.AddModel("embeddings", "all-minilm");
#else // IsAzureOpenAI
var openai = builder.AddAzureOpenAI("openai")
    .AsExisting(azureOpenAIName, azureOpenAIResourceGroup);

openai.AddDeployment(new AzureOpenAIDeployment("gpt-4o-mini", "gpt-4o-mini", "2024-07-18"));
openai.AddDeployment(new AzureOpenAIDeployment("text-embedding-3-small", "text-embedding-3-small", "1"));
#endif

#if (UseAzureAISearch) // TODO
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
#else // IsAzureOpenAI
webApp
    .WithReference(openai)
    .WaitFor(openai);
#endif
#if (UseAzureAISearch) // TODO
#else // UseQdrant
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB);
#endif

builder.Build().Run();
