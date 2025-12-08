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
    instructions: "You edit short stories to improve grammar and style, ensuring the stories are less than 300 words. Once finished editing, you select a title and format the story for publishing.",
    tools: [AIFunctionFactory.Create(FormatStory)]
));

builder.AddWorkflow("publisher", (sp, key) => AgentWorkflowBuilder.BuildSequential(
    workflowName: key,
    sp.GetRequiredKeyedService<AIAgent>("writer"),
    sp.GetRequiredKeyedService<AIAgent>("editor")
)).AddAsAIAgent();

// Register services for OpenAI responses and conversations (also required for DevUI)
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();
app.UseHttpsRedirection();

// Map endpoints for OpenAI responses and conversations (also required for DevUI)
app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (builder.Environment.IsDevelopment())
{
    // Map DevUI endpoint to /devui
    app.MapDevUI();
}

app.Run();

[Description("Formats the story for publication, revealing its title.")]
string FormatStory(string title, string story) => $"""
    **Title**: {title}

    {story}
    """;
