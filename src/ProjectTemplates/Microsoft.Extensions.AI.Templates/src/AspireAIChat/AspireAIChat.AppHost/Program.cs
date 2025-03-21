var builder = DistributedApplication.CreateBuilder(args);

#if (IsOllama)
var ollama = builder.AddOllama("ollama")
    .WithOpenWebUI()
    .WithDataVolume();
var chat = ollama.AddModel("chat", "llama3.2");
var embeddings = ollama.AddModel("embeddings", "all-minilm");
#endif

var ingestionCache = builder.AddSqlite("ingestionCache")
    .WithSqliteWeb();

builder.AddProject<Projects.AspireAIChat_Web>("AspireAIChat")
    .WithReference(chat)
    .WithReference(embeddings)
    .WithReference(ingestionCache)
    .WaitFor(chat)
    .WaitFor(embeddings)
    .WaitFor(ingestionCache);

builder.Build().Run();
