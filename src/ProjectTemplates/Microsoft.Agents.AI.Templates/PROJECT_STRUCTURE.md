# Microsoft.Agents.AI.Templates Project Structure

This document describes the Microsoft.Agents.AI.Templates project structure.

## Project Overview

The `Microsoft.Agents.AI.Templates` project is a .NET project template package that provides project templates for Microsoft.Agents.AI applications. It follows the same structure and infrastructure as `Microsoft.Extensions.AI.Templates`.

## Project Structure

### Source Project
**Location**: `src/ProjectTemplates/Microsoft.Agents.AI.Templates/`

**Key Files**:
- `Microsoft.Agents.AI.Templates.csproj` - The template package project file
- `THIRD-PARTY-NOTICES.TXT` - Third-party license notices

**Configuration**:
- Package type: `Template`
- Target frameworks: `$(NetCoreTargetFrameworks)` (net8.0, net9.0, net10.0)
- Description: "Project templates for Microsoft.Agents.AI."
- Package tags: `dotnet-new;templates;agents;ai`
- Workstream: AI
- Stage: preview
- PreReleaseVersionIteration: 1

**Project References**:
- References `GenerateTemplateContent` project to ensure template content is generated before building

**Template Content**:

#### WebApiAgents Template (`aiagents-webapi`)
**Location**: `src/ProjectTemplates/Microsoft.Agents.AI.Templates/src/WebApiAgents/WebApiAgents-CSharp/`

A simple ASP.NET Core Web API template that demonstrates the AI Agents framework with:
- **Writer Agent**: Writes short stories (300 words or less) about specified topics
- **Editor Agent**: Edits stories to improve grammar and style
- **Publisher Workflow**: A sequential workflow combining writer and editor agents
- OpenAI-compatible API endpoints via `MapOpenAIResponses()`

**Template Files**:
- `.template.config/template.json` - Template definition
  - Short name: `aiagents-webapi`
  - Default name: `WebApiAgents`
  - Source name: `WebApiAgents-CSharp`
  - Target frameworks: net10.0 (default), net9.0, net8.0
  - Configurable HTTP/HTTPS ports via template parameters
  - **AI Service Provider Parameter** (`--provider`):
    - `githubmodels` (default) - GitHub Models
    - `azureopenai` - Azure OpenAI
    - `openai` - OpenAI Platform
    - `ollama` - Ollama (local development)
  - **Chat Model Parameter** (`--ChatModel`):
    - Default for OpenAI/Azure OpenAI/GitHub Models: `gpt-4o-mini`
    - Default for Ollama: `llama3.2`
  - **Use Managed Identity Parameter** (`--managed-identity`):
    - Default: `true` (when using Azure OpenAI)
    - Enables keyless authentication for Azure services
- `WebApiAgents-CSharp.csproj.in` - Project file template with conditional package references
- `WebApiAgents-CSharp.csproj` - Static project file (for development/testing)
- `Program.cs` - Application entry point with conditional AI service configuration
- `Properties/launchSettings.json` - Launch profiles with port configurations
- `appsettings.json` - Application configuration
- `README.md` - Comprehensive getting started guide with:
  - Provider-specific configuration instructions
  - Multiple configuration methods (user secrets, environment variables)
  - Template parameter documentation
  - Example API usage with curl
  - Troubleshooting guidance
- `.gitignore` - Git ignore file to prevent committing sensitive data

**Conditional Package References** (based on AI Service Provider):
- **All providers**:
  - `Microsoft.Agents.AI` (1.0.0-preview.251104.1)
  - `Microsoft.Agents.AI.Hosting` (1.0.0-preview.251104.1)
  - `Microsoft.Agents.AI.Workflows` (1.0.0-preview.251104.1)
- **OpenAI/Azure OpenAI/GitHub Models**:
  - `Microsoft.Agents.AI.Hosting.OpenAI` (1.0.0-alpha.251104.1)
  - `Microsoft.Agents.AI.OpenAI` (1.0.0-preview.251104.1)
- **Ollama**:
  - `OllamaSharp` (4.1.2)
- **Azure OpenAI with Managed Identity**:
  - `Azure.Identity` (1.13.1)

**Template Features**:
- Multi-provider support (GitHub Models, OpenAI, Azure OpenAI, Ollama)
- Configurable chat models with provider-specific defaults
- Managed identity support for Azure OpenAI
- Sequential workflow pattern demonstration
- OpenAI-compatible REST API endpoints
- Comprehensive documentation with provider-specific guidance
- HTTPS configured by default
- Minimal API design pattern

### Test Project
**Location**: `test/ProjectTemplates/Microsoft.Agents.AI.Templates.IntegrationTests/`

**Key Files**:
- `Microsoft.Agents.AI.Templates.Tests.csproj` - The integration test project
- `README.md` - Documentation for running and updating template tests
- `WebApiAgentsTemplateSnapshotTests.cs` - Snapshot tests for aiagents-webapi template
- `ProjectRootHelper.cs` - Helper to locate the test project root
- `VerifyScrubbers.cs` - Utilities for scrubbing variable content from test snapshots

**Test Support Directories**:

1. **TemplateSandbox/** - For manual template testing and debugging
   - `.editorconfig` - Prevents repo settings from applying to generated templates
   - `.gitignore` - Ignores the `output/` directory
   - `activate.ps1` - PowerShell script to set up environment for manual template testing
   - `README.md` - Instructions for debugging templates manually

2. **Snapshots/** - For verified template output snapshots
   - `README.md` - Documentation about snapshot testing
   - Subdirectories for each test scenario (created after first test run)

**Configuration**:
- Project type: Integration Test (`IsIntegrationTestProject = true`)
- Suppressed warnings: CA1063, CA1861, CA2201, VSTHRD003, S104, S125, S2699

**Package References**:
- `Microsoft.TemplateEngine.Authoring.TemplateVerifier` - For template verification
- `Microsoft.TemplateEngine.TestHelper` - For template testing utilities

**Project References**:
- `TestUtilities` - Shared test utilities (uses ProjectTemplates utilities from test/Shared/ProjectTemplates)

**Test Infrastructure**:
The test project uses shared infrastructure from `test/Shared/ProjectTemplates` including:
- `WellKnownPaths` - Path management
- `DotNetCommand` / `DotNetNewCommand` - Command execution
- `VerifyScrubbers` - Content scrubbing for consistent snapshots
- Template verification and execution test bases

## Template Usage

### Installing the Template

```bash
# From the repository root after building
dotnet new install artifacts/packages/Debug/Shipping/Microsoft.Agents.AI.Templates.*.nupkg
```

### Creating a New Project

```bash
# Basic usage (GitHub Models with gpt-4o-mini)
dotnet new aiagents-webapi -n MyAgentsApp

# With custom ports
dotnet new aiagents-webapi -n MyAgentsApp --httpPort 5100 --httpsPort 7100

# Using Azure OpenAI with managed identity
dotnet new aiagents-webapi -n MyAgentsApp --provider azureopenai

# Using Azure OpenAI with API key
dotnet new aiagents-webapi -n MyAgentsApp --provider azureopenai --managed-identity false

# Using OpenAI Platform with custom model
dotnet new aiagents-webapi -n MyAgentsApp --provider openai --ChatModel gpt-4o

# Using Ollama with custom model
dotnet new aiagents-webapi -n MyAgentsApp --provider ollama --ChatModel llama3.1

# Target specific framework
dotnet new aiagents-webapi -n MyAgentsApp --Framework net9.0
```

### Configuring the Project

After creating a project, configure the appropriate credentials:

```bash
cd MyAgentsApp

# For GitHub Models
dotnet user-secrets set "GitHubModels:Token" "your-token-here"

# For OpenAI Platform
dotnet user-secrets set "OpenAI:Key" "your-api-key-here"

# For Azure OpenAI (with API key)
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com"
dotnet user-secrets set "AzureOpenAI:Key" "your-api-key-here"

# For Azure OpenAI (with managed identity)
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com"
# Ensure you're signed in: az login
```

### Running the Project

```bash
dotnet run
```

The application exposes OpenAI-compatible endpoints at:
- `POST /v1/chat/completions` - Chat with AI agents
- Available agents: `writer`, `editor`, `publisher` (workflow)

## Testing the Template

### Running Snapshot Tests

```bash
cd test/ProjectTemplates/Microsoft.Agents.AI.Templates.IntegrationTests
dotnet test
```

### Updating Snapshots

1. Install `DiffEngineTray` following [these instructions](https://github.com/VerifyTests/DiffEngine/blob/main/docs/tray.md)
2. Run the snapshot tests
3. Use `DiffEngineTray` to review and accept changes

### Manual Testing

```bash
cd test/ProjectTemplates/Microsoft.Agents.AI.Templates.IntegrationTests/TemplateSandbox
. ./activate.ps1

# Install template locally
dotnet new install ../../src/ProjectTemplates/Microsoft.Agents.AI.Templates/src/WebApiAgents

# Create test project with different configurations
mkdir output/test-github-models
cd output/test-github-models
dotnet new aiagents-webapi

mkdir ../test-azure-openai
cd ../test-azure-openai
dotnet new aiagents-webapi --provider azureopenai

mkdir ../test-ollama
cd ../test-ollama
dotnet new aiagents-webapi --provider ollama

# Configure and test
dotnet user-secrets set "GitHubModels:Token" "your-token"  # or appropriate config
dotnet run

# Cleanup
dotnet new uninstall ../../src/ProjectTemplates/Microsoft.Agents.AI.Templates/src/WebApiAgents
```

## Integration with Build System

The project uses the `GenerateTemplateContent` approach for template content management, consistent with other template projects in the repository.

The test infrastructure is designed to:
- Use the locally built .NET SDK from the repository
- Reference locally built NuGet packages
- Support debugging through the TemplateSandbox directory
- Verify template output matches expected snapshots

## Package Version Scrubbing

The snapshot tests include scrubbers to normalize package versions:
- Removes pre-release suffixes from Microsoft.Agents.* and Microsoft.Extensions.* package references
- Normalizes localhost ports in launchSettings.json
- Scrubs UserSecretsId values from project files
- Normalizes solution GUIDs

This ensures snapshots remain consistent across different build environments (local, public CI, internal CI).

## Future Enhancements

Potential areas for expansion:
- Additional agent workflow patterns (parallel, conditional)
- Additional template variations (minimal, with authentication, with Swagger)
- Aspire orchestration support
- More AI service provider options
