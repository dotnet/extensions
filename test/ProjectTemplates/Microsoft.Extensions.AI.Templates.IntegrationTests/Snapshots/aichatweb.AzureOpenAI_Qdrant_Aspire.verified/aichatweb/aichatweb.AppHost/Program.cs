var builder = DistributedApplication.CreateBuilder(args);

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://YOUR-DEPLOYMENT-NAME.openai.azure.com;Key=YOUR-API-KEY"
var openai = builder.AddConnectionString("openai");

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:azureAISearch "Endpoint=https://YOUR-DEPLOYMENT-NAME.search.windows.net;Key=YOUR-API-KEY"
var azureAISearch = builder.AddConnectionString("azureAISearch");

var webApp = builder.AddProject<Projects.aichatweb_Web>("aichatweb-app");
webApp.WithReference(openai);
webApp.WithReference(azureAISearch);

builder.Build().Run();
