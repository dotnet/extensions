# AI Agent Web API

This is an AI Agent Web API application created from the `aiagent-webapi` template. This template is currently in a preview stage. If you have feedback, please take a [brief survey](https://aka.ms/dotnet/aiagent-webapi/preview1/survey).

## Prerequisites

- A GitHub Models API token (free to get started)

## Getting Started

### 1. Configure Your AI Service

#### GitHub Models Configuration

This application uses GitHub Models (model: gpt-4o-mini) for AI functionality. You'll need to configure your GitHub Models API token:

**Option A: Using User Secrets (Recommended for Development)**

```bash
dotnet user-secrets set "GITHUB_TOKEN" "your-github-models-token-here"
```

**Option B: Using Environment Variables**

Set the `GITHUB_TOKEN` environment variable:

- **Windows (PowerShell)**:
  ```powershell
  $env:GITHUB_TOKEN = "your-github-models-token-here"
  ```

- **Linux/macOS**:
  ```bash
  export GITHUB_TOKEN="your-github-models-token-here"
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

In development environments, a `/devui/` route is mapped to the Agent Framework development UI (DevUI), and when the app is launched through an IDE a browser will open to this URL. DevUI provides a web-based UI for interacting with agents and workflows. DevUI operates as an OpenAI-compatible client using the Responses and Conversations endpoints.

## How It Works

This application demonstrates Agent Framework with:

1. **Writer Agent**: Writes short stories (300 words or less) about specified topics
2. **Editor Agent**: Edits stories to improve grammar and style, ensuring they stay under 300 words
3. **Publisher Workflow Agent**: A sequential workflow agent that combines the writer and editor agents

The agents are exposed through OpenAI-compatible API endpoints, making them easy to integrate with existing tools and applications.

## Template Parameters

When creating a new project, you can customize it using template parameters:

```bash
# Specify AI service provider
dotnet new aiagent-webapi --provider azureopenai

# Specify a custom chat model
dotnet new aiagent-webapi --chat-model gpt-4o

# Use API key authentication for Azure OpenAI
dotnet new aiagent-webapi --provider azureopenai --managed-identity false

# Use Ollama with a different model
dotnet new aiagent-webapi --provider ollama --chat-model llama3.1
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

- [AI apps for .NET developers](https://learn.microsoft.com/dotnet/ai)
- [Microsoft Agent Framework Documentation](https://aka.ms/dotnet/agent-framework/docs)
- [GitHub Models](https://github.com/marketplace/models)

## Troubleshooting

**Problem**: Application fails with "Missing configuration: GITHUB_TOKEN"

**Solution**: Make sure you've configured your GitHub Models API token using one of the methods described above.

**Problem**: API requests fail with authentication errors

**Solution**: Verify your GitHub Models token is valid and hasn't expired. You may need to regenerate it from the GitHub Models website.

