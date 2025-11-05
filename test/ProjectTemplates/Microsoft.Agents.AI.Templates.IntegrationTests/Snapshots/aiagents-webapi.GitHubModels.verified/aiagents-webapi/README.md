# AI Agents Web API

This is an AI Agents Web API application created from the `aiagents-webapi` template.

## Prerequisites

- A GitHub Models API token (free to get started)

## Getting Started

### 1. Configure Your AI Service

#### GitHub Models Configuration

This application uses GitHub Models (model: gpt-4o-mini) for AI functionality. You'll need to configure your GitHub Models API token:

**Option A: Using User Secrets (Recommended for Development)**

```bash
dotnet user-secrets set "GitHubModels:Token" "your-github-models-token-here"
```

**Option B: Using Environment Variables**

Set the `GitHubModels__Token` environment variable:

- **Windows (PowerShell)**:
  ```powershell
  $env:GitHubModels__Token = "your-github-models-token-here"
  ```

- **Linux/macOS**:
  ```bash
  export GitHubModels__Token="your-github-models-token-here"
  ```

#### Get a GitHub Models Token

1. Visit [GitHub Models](https://github.com/marketplace/models)
2. Sign in with your GitHub account
3. Select a model (e.g., gpt-4o-mini)
4. Click "Get API Key" or follow the authentication instructions
5. Copy your personal access token


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
- [GitHub Models](https://github.com/marketplace/models)
- [.NET AI Libraries](https://learn.microsoft.com/dotnet/ai/)

## Troubleshooting

**Problem**: Application fails with "Missing configuration: GitHubModels:Token"

**Solution**: Make sure you've configured your GitHub Models API token using one of the methods described above.

**Problem**: API requests fail with authentication errors

**Solution**: Verify your GitHub Models token is valid and hasn't expired. You may need to regenerate it from the GitHub Models website.

