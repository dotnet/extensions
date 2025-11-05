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
**Location**: `src/ProjectTemplates/Microsoft.Agents.AI.Templates/src/WebApiAgents/`

A simple ASP.NET Core Web API template that demonstrates the AI Agents framework with:
- **Writer Agent**: Writes short stories (300 words or less) about specified topics
- **Editor Agent**: Edits stories to improve grammar and style
- **Publisher Workflow**: A sequential workflow combining writer and editor agents
- OpenAI-compatible API endpoints via `MapOpenAIResponses()`

**Template Files**:
- `.template.config/template.json` - Template definition
  - Short name: `aiagents-webapi`
  - Default name: `AgentsApp`
  - Source name: `WebApiAgents-CSharp`
  - Target framework: net10.0
  - Configurable HTTP/HTTPS ports via template parameters
- `WebApiAgents-CSharp.csproj` - Project file with Microsoft.Agents.AI package references
- `Program.cs` - Application entry point with AI agent configuration
- `Properties/launchSettings.json` - Launch profiles with port configurations
- `appsettings.json` - Application configuration
- `README.md` - Comprehensive getting started guide with:
  - GitHub Models token configuration instructions
  - Multiple configuration methods (user secrets, environment variables, appsettings)
  - Example API usage with curl
- Troubleshooting guidance
- `.gitignore` - Git ignore file to prevent committing sensitive data

**Package References**:
- `Microsoft.Agents.AI` (1.0.0-preview.251104.1)
- `Microsoft.Agents.AI.Hosting` (1.0.0-preview.251104.1)
- `Microsoft.Agents.AI.Hosting.OpenAI` (1.0.0-alpha.251104.1)
- `Microsoft.Agents.AI.OpenAI` (1.0.0-preview.251104.1)
- `Microsoft.Agents.AI.Workflows` (1.0.0-preview.251104.1)

**Template Features**:
- Uses GitHub Models (gpt-4o-mini) via Azure AI Inference endpoint
- Demonstrates sequential workflow pattern
- Exposes agents via OpenAI-compatible REST API
- Includes comprehensive README with setup instructions
- Configured for HTTPS by default
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
# Basic usage
dotnet new aiagents-webapi -n MyAgentsApp

# With custom ports
dotnet new aiagents-webapi -n MyAgentsApp --httpPort 5100 --httpsPort 7100
```

### Configuring the Project

After creating a project, configure the GitHub Models token:

```bash
cd MyAgentsApp
dotnet user-secrets set "GitHubModels:Token" "your-token-here"
```

### Running the Project

```bash
dotnet run
```

The application exposes OpenAI-compatible endpoints at:
- `POST /v1/chat/completions` - Chat with AI agents
- Model options: `writer`, `editor`, `publisher` (workflow)

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

# Create test project
mkdir output/test1
cd output/test1
dotnet new aiagents-webapi

# Configure and test
dotnet user-secrets set "GitHubModels:Token" "your-token"
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
- Multiple AI provider options (Azure OpenAI, Ollama)
- Aspire orchestration support
- Additional template variations (minimal, with authentication, with Swagger)
