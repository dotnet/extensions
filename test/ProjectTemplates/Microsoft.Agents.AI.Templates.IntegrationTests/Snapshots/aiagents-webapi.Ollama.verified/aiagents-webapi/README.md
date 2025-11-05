# AI Agents Web API

This is an AI Agents Web API application created from the `aiagents-webapi` template.

## Prerequisites

- Ollama installed locally with the llama3.2 model

## Getting Started

### 1. Configure Your AI Service

#### Ollama Configuration

This application uses Ollama running locally (model: llama3.2). You'll need to have Ollama installed and the llama3.2 model downloaded:

1. Visit [Ollama](https://ollama.com) and follow the installation instructions for your platform
2. Once installed, download the llama3.2 model:
   ```bash
   ollama pull llama3.2
   ```
3. Ensure Ollama is running (it starts automatically after installation)

The application is configured to connect to Ollama at `http://localhost:9999`.


### 2. Run the Application

```bash
dotnet run -lp https
```

The application will start and listen on:
- HTTP: `http://localhost:9999`
- HTTPS: `https://localhost:9999`

### 3. Test the Application

The application exposes OpenAI-compatible API endpoints. You can interact with the AI agents using any OpenAI-compatible client or tools.

## How It Works

This application demonstrates the AI Agents framework with:

1. **Writer Agent**: Writes short stories (300 words or less) about specified topics
2. **Editor Agent**: Edits stories to improve grammar and style, ensuring they stay under 300 words
3. **Publisher Workflow Agent**: A sequential workflow agent that combines the writer and editor agents

The agents are exposed through OpenAI-compatible API endpoints, making them easy to integrate with existing tools and applications.

## Template Parameters

When creating a new project, you can customize it using template parameters:

```bash
# Specify AI service provider
dotnet new aiagents-webapi --provider azureopenai

# Specify a custom chat model
dotnet new aiagents-webapi --chat-model gpt-4o

# Use API key authentication for Azure OpenAI
dotnet new aiagents-webapi --provider azureopenai --managed-identity false

# Use Ollama with a different model
dotnet new aiagents-webapi --provider ollama --chat-model llama3.1
```

### Available Parameters

- **`--provider`**: Choose the AI service provider
  - `githubmodels` (default) - GitHub Models
  - `azureopenai` - Azure OpenAI
  - `openai` - OpenAI Platform
  - `ollama` - Ollama (local development)

- **`--chat-model`**: Specify the chat model/deployment name
  - Default for OpenAI/Azure OpenAI/GitHub Models: `gpt-4o-mini`
  - Default for Ollama: `llama3.2`

- **`--managed-identity`**: Use managed identity for Azure services (default: `true`)
  - Only applicable when `--provider azureopenai`

- **`--framework`**: Target framework (default: `net10.0`)
  - Options: `net10.0`, `net9.0`, `net8.0`

## Project Structure

- `Program.cs` - Application entry point and configuration
- `appsettings.json` - Application configuration
- `Properties/launchSettings.json` - Launch profiles for development

## Learn More

- [Microsoft.Agents.AI Documentation](https://learn.microsoft.com/dotnet/ai/agents)
- [Ollama](https://ollama.com)
- [.NET AI Libraries](https://learn.microsoft.com/dotnet/ai/)

## Troubleshooting

**Problem**: Application fails to connect to Ollama

**Solution**: 
- Ensure Ollama is running. On macOS/Linux, check with `pgrep ollama`. On Windows, check Task Manager.
- Verify Ollama is accessible at `http://localhost:9999`
- Make sure you've downloaded the llama3.2 model: `ollama pull llama3.2`

**Problem**: Model responses are slow or time out

**Solution**: Ollama runs locally and performance depends on your hardware. Consider using a smaller model or ensuring your system has adequate resources (RAM, GPU if available).

