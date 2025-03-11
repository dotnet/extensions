var builder = DistributedApplication.CreateBuilder(args);

var qdrantApiKey = builder.AddParameter("qdrantapikey", secret: true);
var azureOpenAiEndpoint = builder.AddParameter("azureOpenAiEndpoint", secret: true);
var azureOpenAiKey = builder.AddParameter("azureOpenAiKey", secret: true);

var vectorDB = builder.AddQdrant("vectordb", apiKey: qdrantApiKey, grpcPort: 6334, httpPort: 6333)
    .WithDataBindMount("./qdrant_data")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.ChatWithCustomData_Aspire_CSharp_Web>("aichatweb-app")
    .WaitFor(vectorDB)
    .WithReference(vectorDB)
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", azureOpenAiEndpoint)
    .WithEnvironment("AZURE_OPENAI_KEY", azureOpenAiKey);

builder.Build().Run();
