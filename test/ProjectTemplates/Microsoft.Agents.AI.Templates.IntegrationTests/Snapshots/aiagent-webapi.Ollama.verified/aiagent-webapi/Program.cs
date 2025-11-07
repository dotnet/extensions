using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

// You will need to have Ollama running locally with the llama3.2 model installed
// Visit https://ollama.com for installation instructions
var chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2");

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

if (builder.Environment.IsDevelopment())
{
    // Add the Agent Framework developer UI (DevUI) services in development environments
    builder.AddDevUI();
}

var app = builder.Build();
app.UseHttpsRedirection();

// Expose the agents using the OpenAI Responses API
// This is also needed for DevUI to function
app.MapOpenAIResponses();

// Expose the conversations using the OpenAI Conversations API
// This is also needed for DevUI to manage stateful conversations
app.MapOpenAIConversations();

if (app.Environment.IsDevelopment())
{
    // Map the Agent Framework developer UI to /devui/ in development environments
    app.MapDevUI();
}

app.Run();

[Description("Formats the story for display.")]
string FormatStory(string title, string story) => $"""
    **Title**: {title}
    **Date**: {DateTime.Today.ToShortDateString()}

    {story}
    """;
