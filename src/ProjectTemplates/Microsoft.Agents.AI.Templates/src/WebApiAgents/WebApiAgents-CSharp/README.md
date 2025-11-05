# AI Agents Web API

This is an AI Agents Web API application created from the `aiagents-webapi` template.

## Prerequisites

- .NET 10.0 SDK or later
<!--#if (IsGHModels) -->
- A GitHub Models API token (free to get started)
<!--#elif (IsOpenAI) -->
- An OpenAI API key
<!--#elif (IsAzureOpenAI) -->
- An Azure OpenAI service deployment
<!--#elif (IsOllama) -->
- Ollama installed locally with the llama3.2 model
<!--#endif -->

## Getting Started

### 1. Configure Your AI Service

<!--#if (IsGHModels) -->
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

<!--#elif (IsOpenAI) -->
#### OpenAI Configuration

This application uses the OpenAI Platform (model: gpt-4o-mini). You'll need to configure your OpenAI API key:

**Using User Secrets (Recommended for Development)**

```bash
dotnet user-secrets set "OpenAI:Key" "your-openai-api-key-here"
```

**Using Environment Variables**

Set the `OpenAI__Key` environment variable:

- **Windows (PowerShell)**:
  ```powershell
  $env:OpenAI__Key = "your-openai-api-key-here"
  ```

- **Linux/macOS**:
  ```bash
  export OpenAI__Key="your-openai-api-key-here"
  ```

#### Get an OpenAI API Key

1. Visit [OpenAI Platform](https://platform.openai.com)
2. Sign in or create an account
3. Navigate to API Keys
4. Create a new API key
5. Copy your API key

<!--#elif (IsAzureOpenAI) -->
#### Azure OpenAI Configuration

This application uses Azure OpenAI service (model: gpt-4o-mini). You'll need to configure your Azure OpenAI endpoint<!--#if (!IsManagedIdentity) --> and API key<!--#endif -->:

**Using User Secrets (Recommended for Development)**

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-DEPLOYMENT-NAME.openai.azure.com"
<!--#if (!IsManagedIdentity) -->
dotnet user-secrets set "AzureOpenAI:Key" "your-azure-openai-key-here"
<!--#endif -->
```

**Using Environment Variables**

- **Windows (PowerShell)**:
  ```powershell
  $env:AzureOpenAI__Endpoint = "https://YOUR-DEPLOYMENT-NAME.openai.azure.com"
<!--#if (!IsManagedIdentity) -->
  $env:AzureOpenAI__Key = "your-azure-openai-key-here"
<!--#endif -->
  ```

- **Linux/macOS**:
  ```bash
  export AzureOpenAI__Endpoint="https://YOUR-DEPLOYMENT-NAME.openai.azure.com"
<!--#if (!IsManagedIdentity) -->
  export AzureOpenAI__Key="your-azure-openai-key-here"
<!--#endif -->
  ```

<!--#if (IsManagedIdentity) -->
#### Managed Identity Authentication

This application is configured to use Azure Managed Identity for authentication. When deploying to Azure:

1. Ensure your Azure resource (App Service, Container Apps, etc.) has a managed identity enabled
2. Grant the managed identity access to your Azure OpenAI resource with the "Cognitive Services OpenAI User" role
3. No API key configuration is needed

For local development, ensure you're signed in to Azure CLI or have configured DefaultAzureCredential appropriately.

<!--#endif -->
#### Set Up Azure OpenAI

1. Visit [Azure Portal](https://portal.azure.com)
2. Create an Azure OpenAI resource
3. Deploy a model (e.g., gpt-4o-mini)
4. Copy your endpoint<!--#if (!IsManagedIdentity) --> and API key<!--#endif -->

<!--#elif (IsOllama) -->
#### Ollama Configuration

This application uses Ollama running locally (model: llama3.2). You'll need to have Ollama installed and the llama3.2 model downloaded:

1. Visit [Ollama](https://ollama.com) and follow the installation instructions for your platform
2. Once installed, download the llama3.2 model:
   ```bash
   ollama pull llama3.2
   ```
3. Ensure Ollama is running (it starts automatically after installation)

The application is configured to connect to Ollama at `http://localhost:11434`.

<!--#endif -->

### 2. Run the Application

```bash
dotnet run
```

The application will start and listen on:
- HTTP: `http://localhost:5056`
- HTTPS: `https://localhost:7041`

### 3. Test the Application

The application exposes OpenAI-compatible API endpoints. You can interact with the AI agents using any OpenAI-compatible client or tools.

Example using `curl`:

```bash
curl -X POST https://localhost:7041/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "publisher",
    "messages": [
      {
        "role": "user",
        "content": "Write a story about a robot learning to paint."
      }
    ]
  }'
```

## How It Works

This application demonstrates the AI Agents framework with:

1. **Writer Agent**: Writes short stories (300 words or less) about specified topics
2. **Editor Agent**: Edits stories to improve grammar and style, ensuring they stay under 300 words
3. **Publisher Workflow**: A sequential workflow that combines the writer and editor agents

The agents are exposed through OpenAI-compatible API endpoints, making them easy to integrate with existing tools and applications.

## Template Parameters

When creating a new project, you can customize it using template parameters:

```bash
# Specify AI service provider
dotnet new aiagents-webapi --provider azureopenai

# Specify a custom chat model
dotnet new aiagents-webapi --ChatModel gpt-4o

# Use API key authentication for Azure OpenAI
dotnet new aiagents-webapi --provider azureopenai --UseManagedIdentity false

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

- **`--UseManagedIdentity`**: Use managed identity for Azure services (default: `true`)
  - Only applicable when `--AiServiceProvider azureopenai`

- **`--Framework`**: Target framework (default: `net10.0`)
  - Options: `net10.0`, `net9.0`, `net8.0`

## Project Structure

- `Program.cs` - Application entry point and configuration
- `appsettings.json` - Application configuration
- `Properties/launchSettings.json` - Launch profiles for development

## Learn More

- [Microsoft.Agents.AI Documentation](https://learn.microsoft.com/dotnet/ai/agents)
<!--#if (IsGHModels) -->
- [GitHub Models](https://github.com/marketplace/models)
<!--#elif (IsOpenAI) -->
- [OpenAI Platform](https://platform.openai.com)
<!--#elif (IsAzureOpenAI) -->
- [Azure OpenAI Service](https://azure.microsoft.com/products/ai-services/openai-service)
<!--#elif (IsOllama) -->
- [Ollama](https://ollama.com)
<!--#endif -->
- [.NET AI Libraries](https://learn.microsoft.com/dotnet/ai/)

## Troubleshooting

<!--#if (IsGHModels) -->
**Problem**: Application fails with "Missing configuration: GitHubModels:Token"

**Solution**: Make sure you've configured your GitHub Models API token using one of the methods described above.

**Problem**: API requests fail with authentication errors

**Solution**: Verify your GitHub Models token is valid and hasn't expired. You may need to regenerate it from the GitHub Models website.

<!--#elif (IsOpenAI) -->
**Problem**: Application fails with "Missing configuration: OpenAI:Key"

**Solution**: Make sure you've configured your OpenAI API key using one of the methods described above.

**Problem**: API requests fail with authentication errors

**Solution**: Verify your OpenAI API key is valid. Check your usage limits and billing status on the OpenAI Platform.

<!--#elif (IsAzureOpenAI) -->
**Problem**: Application fails with "Missing configuration: AzureOpenAI:Endpoint"<!--#if (!IsManagedIdentity) --> or "Missing configuration: AzureOpenAI:Key"<!--#endif -->

**Solution**: Make sure you've configured your Azure OpenAI endpoint<!--#if (!IsManagedIdentity) --> and API key<!--#endif --> using one of the methods described above.

<!--#if (IsManagedIdentity) -->
**Problem**: Managed identity authentication fails

**Solution**: 
- Ensure your Azure resource has a system-assigned or user-assigned managed identity enabled
- Verify the managed identity has been granted the "Cognitive Services OpenAI User" role on your Azure OpenAI resource
- For local development, ensure you're signed in to Azure CLI: `az login`

<!--#endif -->
**Problem**: API requests fail with authentication errors

**Solution**: Verify your Azure OpenAI endpoint is correct and<!--#if (!IsManagedIdentity) --> your API key is valid<!--#endif --><!--#if (IsManagedIdentity) --> your managed identity has the correct permissions<!--#endif -->.

<!--#elif (IsOllama) -->
**Problem**: Application fails to connect to Ollama

**Solution**: 
- Ensure Ollama is running. On macOS/Linux, check with `pgrep ollama`. On Windows, check Task Manager.
- Verify Ollama is accessible at `http://localhost:11434`
- Make sure you've downloaded the llama3.2 model: `ollama pull llama3.2`

**Problem**: Model responses are slow or time out

**Solution**: Ollama runs locally and performance depends on your hardware. Consider using a smaller model or ensuring your system has adequate resources (RAM, GPU if available).

<!--#endif -->
