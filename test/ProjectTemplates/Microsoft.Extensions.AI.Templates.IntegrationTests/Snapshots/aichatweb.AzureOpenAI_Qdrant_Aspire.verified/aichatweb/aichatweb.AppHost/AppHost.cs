var builder = DistributedApplication.CreateBuilder(args);

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://YOUR-DEPLOYMENT-NAME.openai.azure.com/openai/v1;Key=YOUR-API-KEY"
var openai = builder.AddConnectionString("openai");

// See https://learn.microsoft.com/dotnet/aspire/azure/local-provisioning#configuration
// for instructions providing configuration values
var search = builder.AddAzureSearch("search");

var webApp = builder.AddProject<Projects.aichatweb_Web>("aichatweb-app");
webApp.WithReference(openai);
webApp
    .WithReference(search)
    .WaitFor(search);

builder.Build().Run();
