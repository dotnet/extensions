#if (IsGHModels || IsOpenAI || (IsAzureOpenAI && !IsManagedIdentity))
using System.ClientModel;
#elif (IsAzureOpenAI && IsManagedIdentity)
using System.ClientModel.Primitives;
using Azure.Identity;
#endif
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
#if (IsOllama)
using OllamaSharp;
#elif (IsGHModels || IsOpenAI || IsAzureOpenAI)
using OpenAI;
#endif

var builder = WebApplication.CreateBuilder(args);

#if (IsGHModels)
// You will need to set the token to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set GitHubModels:Token YOUR-GITHUB-TOKEN
var credential = new ApiKeyCredential(builder.Configuration["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token. See README for details."));
var openAIOptions = new OpenAIClientOptions { Endpoint = new Uri("https://models.inference.ai.azure.com") };

var ghModelsClient = new OpenAIClient(credential, openAIOptions)
    .GetChatClient("gpt-4o-mini").AsIChatClient();

builder.Services.AddChatClient(ghModelsClient);
#elif (IsOllama)
// You will need to have Ollama running locally with the llama3.2 model installed
// Visit https://ollama.com for installation instructions
IChatClient chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2");

builder.Services.AddChatClient(chatClient);
#elif (IsOpenAI)
// You will need to set the API key to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set OpenAI:Key YOUR-API-KEY
var openAIClient = new OpenAIClient(
    new ApiKeyCredential(builder.Configuration["OpenAI:Key"] ?? throw new InvalidOperationException("Missing configuration: OpenAI:Key. See README for details.")));

#pragma warning disable OPENAI001 // GetOpenAIResponseClient(string) is experimental and subject to change or removal in future updates.
var chatClient = openAIClient.GetOpenAIResponseClient("gpt-4o-mini").AsIChatClient();
#pragma warning restore OPENAI001

builder.Services.AddChatClient(chatClient);
#elif (IsAzureOpenAI)
// You will need to set the endpoint to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set AzureOpenAI:Endpoint https://YOUR-DEPLOYMENT-NAME.openai.azure.com
#if (!IsManagedIdentity)
//   dotnet user-secrets set AzureOpenAI:Key YOUR-API-KEY
#endif
var azureOpenAIEndpoint = new Uri(new Uri(builder.Configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Missing configuration: AzureOpenAI:Endpoint. See README for details.")), "/openai/v1");
#if (IsManagedIdentity)
#pragma warning disable OPENAI001 // OpenAIClient(AuthenticationPolicy, OpenAIClientOptions) and GetOpenAIResponseClient(string) are experimental and subject to change or removal in future updates.
var azureOpenAI = new OpenAIClient(
    new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
    new OpenAIClientOptions { Endpoint = azureOpenAIEndpoint });

#elif (!IsManagedIdentity)
var openAIOptions = new OpenAIClientOptions { Endpoint = azureOpenAIEndpoint };
var azureOpenAI = new OpenAIClient(new ApiKeyCredential(builder.Configuration["AzureOpenAI:Key"] ?? throw new InvalidOperationException("Missing configuration: AzureOpenAI:Key. See README for details.")), openAIOptions);

#pragma warning disable OPENAI001 // GetOpenAIResponseClient(string) is experimental and subject to change or removal in future updates.
#endif
var chatClient = azureOpenAI.GetOpenAIResponseClient("gpt-4o-mini").AsIChatClient();
#pragma warning restore OPENAI001

builder.Services.AddChatClient(chatClient);
#endif

var writer = builder.AddAIAgent("writer", "You write short stories (300 words or less) about the specified topic.");
var editor = builder.AddAIAgent("editor", "You edit short stories to improve grammar and style. You ensure the stories are less than 300 words.");

builder.AddSequentialWorkflow("publisher", [writer, editor]).AddAsAIAgent();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapOpenAIResponses();

app.Run();
