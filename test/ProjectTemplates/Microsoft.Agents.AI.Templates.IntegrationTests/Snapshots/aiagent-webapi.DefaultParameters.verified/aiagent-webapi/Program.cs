using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// You will need to set the token to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set GitHubModels:Token YOUR-GITHUB-TOKEN
var credential = new ApiKeyCredential(builder.Configuration["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token. See README for details."));
var openAIOptions = new OpenAIClientOptions { Endpoint = new Uri("https://models.inference.ai.azure.com") };

var chatClient = new OpenAIClient(credential, openAIOptions)
    .GetChatClient("gpt-4o-mini").AsIChatClient();

builder.Services.AddChatClient(chatClient);

builder.AddAIAgent("writer", "You write short stories (300 words or less) about the specified topic.");

builder.AddAIAgent("editor", (sp, key) => new ChatClientAgent(
    chatClient,
    name: key,
    instructions: "You edit short stories to improve grammar and style. You ensure the stories are less than 300 words.",
    tools: [ AIFunctionFactory.Create(FormatStory) ]
));

builder.AddWorkflow("publisher", (sp, key) => AgentWorkflowBuilder.BuildSequential(
    workflowName: key,
    sp.GetRequiredKeyedService<AIAgent>("writer"),
    sp.GetRequiredKeyedService<AIAgent>("editor")
)).AddAsAIAgent();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapOpenAIResponses();

app.Run();

[Description("Formats the story for display.")]
string FormatStory(string title, string story) => $"""
    **Title**: {title}
    **Date**: {DateTime.Today.ToShortDateString()}

    {story}
    """;
