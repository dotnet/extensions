# AI Agents Web API

This is an AI Agents Web API application created from the `aiagents-webapi` template.

## Prerequisites

- An Azure OpenAI service deployment

## Getting Started

### 1. Configure Your AI Service

#### Azure OpenAI Configuration


**Using User Secrets (Recommended for Development)**

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-DEPLOYMENT-NAME.openai.azure.com"
dotnet user-secrets set "AzureOpenAI:Key" "your-azure-openai-key-here"
```

**Using Environment Variables**

- **Windows (PowerShell)**:
  ```powershell
  $env:AzureOpenAI__Endpoint = "https://YOUR-DEPLOYMENT-NAME.openai.azure.com"
  $env:AzureOpenAI__Key = "your-azure-openai-key-here"
  ```

- **Linux/macOS**:
  ```bash
  export AzureOpenAI__Endpoint="https://YOUR-DEPLOYMENT-NAME.openai.azure.com"
  export AzureOpenAI__Key="your-azure-openai-key-here"
  ```

#### Set Up Azure OpenAI

1. Visit [Azure Portal](https://portal.azure.com)
2. Create an Azure OpenAI resource
3. Deploy a model (e.g., gpt-4o-mini)


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
dotnet new aiagents-webapi --ChatModel gpt-4o

# Use API key authentication for Azure OpenAI
dotnet new aiagents-webapi --provider azureopenai --managed-identity false

# Use Ollama with a different model
dotnet new aiagents-webapi --provider ollama --ChatModel llama3.1
```

### Available Parameters

- **`--provider`**: Choose the AI service provider
  - `githubmodels` (default) - GitHub Models
  - `azureopenai` - Azure OpenAI
  - `openai` - OpenAI Platform
  - `ollama` - Ollama (local development)

- **`--ChatModel`**: Specify the chat model/deployment name
  - Default for OpenAI/Azure OpenAI/GitHub Models: `gpt-4o-mini`
  - Default for Ollama: `llama3.2`

- **`--managed-identity`**: Use managed identity for Azure services (default: `true`)
  - Only applicable when `--provider azureopenai`

- **`--Framework`**: Target framework (default: `net10.0`)
  - Options: `net10.0`, `net9.0`, `net8.0`

## Project Structure

- `Program.cs` - Application entry point and configuration
- `appsettings.json` - Application configuration
- `Properties/launchSettings.json` - Launch profiles for development

## Learn More

- [Microsoft.Agents.AI Documentation](https://learn.microsoft.com/dotnet/ai/agents)
- [Azure OpenAI Service](https://azure.microsoft.com/products/ai-services/openai-service)
- [.NET AI Libraries](https://learn.microsoft.com/dotnet/ai/)

## Troubleshooting



**Problem**: API requests fail with authentication errors


