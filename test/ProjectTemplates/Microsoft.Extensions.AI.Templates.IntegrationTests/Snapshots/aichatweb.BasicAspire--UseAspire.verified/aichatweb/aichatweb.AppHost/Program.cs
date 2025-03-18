var builder = DistributedApplication.CreateBuilder(args);

// You will need to set the endpoint and key to your own values
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set Parameters:gitHubModelsToken YOUR-GITHUB-TOKEN
var gitHubModelsToken = builder.AddParameter("gitHubModelsToken", secret: true);

var ingestionCache = builder.AddSqlite("ingestionCache")
    .WithSqliteWeb();

var webApp = builder.AddProject<Projects.aichatweb_Web>("aichatweb-app");
webApp.WithEnvironment("GITHUB_MODELS_TOKEN", gitHubModelsToken);
webApp
    .WithReference(ingestionCache)
    .WaitFor(ingestionCache);

builder.Build().Run();
