var builder = DistributedApplication.CreateBuilder(args);

// See https://learn.microsoft.com/dotnet/aspire/azure/local-provisioning#configuration
// for instructions providing configuration values
var openai = builder.AddAzureOpenAI("openai");

openai.AddDeployment(
    name: "gpt-4o-mini",
    modelName: "gpt-4o-mini",
    modelVersion: "2024-07-18");

openai.AddDeployment(
    name: "text-embedding-3-small",
    modelName: "text-embedding-3-small",
    modelVersion: "1");

// See https://learn.microsoft.com/dotnet/aspire/azure/local-provisioning#configuration
// for instructions providing configuration values
var search = builder.AddAzureSearch("search");

var markitdown = builder.AddContainer("markitdown", "mcp/markitdown")
    .WithArgs("--http", "--host", "0.0.0.0", "--port", "3001")
    .WithHttpEndpoint(targetPort: 3001, name: "http");

var webApp = builder.AddProject<Projects.aichatweb_Web>("aichatweb-app");
webApp
    .WithReference(openai)
    .WaitFor(openai);
webApp
    .WithReference(search)
    .WaitFor(search);
webApp
    .WithEnvironment("MARKITDOWN_MCP_URL", markitdown.GetEndpoint("http"));

builder.Build().Run();
