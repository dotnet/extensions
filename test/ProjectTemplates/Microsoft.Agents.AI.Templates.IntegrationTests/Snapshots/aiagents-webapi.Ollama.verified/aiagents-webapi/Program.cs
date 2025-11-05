using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

// You will need to have Ollama running locally with the llama3.2 model installed
// Visit https://ollama.com for installation instructions
IChatClient chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2");

builder.Services.AddChatClient(chatClient);

var writer = builder.AddAIAgent("writer", "You write short stories (300 words or less) about the specified topic.");
var editor = builder.AddAIAgent("editor", "You edit short stories to improve grammar and style. You ensure the stories are less than 300 words.");

builder.AddSequentialWorkflow("publisher", [writer, editor]).AddAsAIAgent();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapOpenAIResponses();

app.Run();
