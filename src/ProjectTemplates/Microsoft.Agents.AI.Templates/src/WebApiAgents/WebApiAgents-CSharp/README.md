# AI Agents Web API

This is an AI Agents Web API application created from the `webapi-agents` template.

## Prerequisites

- .NET 10.0 SDK or later
- A GitHub Models API token (free to get started)

## Getting Started

### 1. Configure Your API Token

This application uses GitHub Models for AI functionality. You'll need to configure your GitHub Models API token:

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

**Option C: Using appsettings.Development.json (Not Recommended - Do Not Commit)**

Create an `appsettings.Development.json` file:

```json
{
  "GitHubModels": {
    "Token": "your-github-models-token-here"
  }
}
```

?? **Important**: Never commit API tokens to source control. Add `appsettings.Development.json` to your `.gitignore` file.

### 2. Get a GitHub Models Token

1. Visit [GitHub Models](https://github.com/marketplace/models)
2. Sign in with your GitHub account
3. Select a model (e.g., gpt-4o-mini)
4. Click "Get API Key" or follow the authentication instructions
5. Copy your personal access token

### 3. Run the Application

```bash
dotnet run
```

The application will start and listen on:
- HTTP: `http://localhost:5056`
- HTTPS: `https://localhost:7041`

### 4. Test the Application

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
