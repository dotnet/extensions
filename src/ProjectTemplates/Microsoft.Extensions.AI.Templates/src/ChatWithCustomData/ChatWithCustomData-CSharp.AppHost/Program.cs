var builder = DistributedApplication.CreateBuilder(args);

#if (UseAzureAISearch) // TODO
#else // UseQdrant
var qdrantApiKey = builder.AddParameter("qdrantapikey", secret: true);
#endif

#if (UseAzureAISearch) // TODO
#else // UseQdrant
var vectorDB = builder.AddQdrant("vectordb", apiKey: qdrantApiKey, grpcPort: 6334, httpPort: 6333)
    .WithDataBindMount("./qdrant_data")
    .WithLifetime(ContainerLifetime.Persistent);
#endif

var webApp = builder.AddProject<Projects.ChatWithCustomData_CSharp_Web>("aichatweb-app");
#if (UseAzureAISearch) // TODO
#else // UseQdrant
webApp
    .WaitFor(vectorDB)
    .WithReference(vectorDB);
#endif

builder.Build().Run();
