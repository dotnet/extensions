# Microsoft.Agents.AI.Templates Project Structure

This document describes the newly created `Microsoft.Agents.AI.Templates` project structure and infrastructure.

## Project Overview

The `Microsoft.Agents.AI.Templates` project is a .NET project template package designed to provide project templates for Microsoft.Agents.AI applications. It follows the same structure and infrastructure as `Microsoft.Extensions.AI.Templates`.

## Project Structure

### Source Project
**Location**: `src/ProjectTemplates/Microsoft.Agents.AI.Templates/`

**Key Files**:
- `Microsoft.Agents.AI.Templates.csproj` - The template package project file
- `THIRD-PARTY-NOTICES.TXT` - Third-party license notices (currently a placeholder)

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

**Content Items**:
- Template content will be added to the `<Content>` ItemGroup once actual templates are implemented
- Currently includes only `THIRD-PARTY-NOTICES.TXT`

### Test Project
**Location**: `test/ProjectTemplates/Microsoft.Agents.AI.Templates.IntegrationTests/`

**Key Files**:
- `Microsoft.Agents.AI.Templates.Tests.csproj` - The integration test project
- `README.md` - Documentation for running and updating template tests
- `AgentTemplateSnapshotTests.cs` - Placeholder snapshot test class (test is skipped until template content is added)
- `ProjectRootHelper.cs` - Helper to locate the test project root
- `VerifyScrubbers.cs` - Utilities for scrubbing variable content from test snapshots

**Infrastructure Files** (in `Infrastructure/` subdirectory):
- `WellKnownPaths.cs` - Constants for important paths (repo root, template feed location, sandbox paths, etc.)
- `TestCommand.cs` - Base class for executing commands during tests
- `DotNetCommand.cs` - Specialized command for executing dotnet CLI commands
- `TestCommandResult.cs` - Result data from command execution
- `ProcessExtensions.cs` - Extension methods for working with processes

**Test Support Directories**:

1. **TemplateSandbox/** - For manual template testing and debugging
   - `.editorconfig` - Prevents repo settings from applying to generated templates
   - `.gitignore` - Ignores the `output/` directory
   - `activate.ps1` - PowerShell script to set up environment for manual template testing
   - `README.md` - Instructions for debugging templates manually

2. **Snapshots/** - For verified template output snapshots
   - `README.md` - Documentation about snapshot testing
   - Subdirectories for each test scenario will be added when templates are implemented

**Configuration**:
- Project type: Integration Test (`IsIntegrationTestProject = true`)
- Suppressed warnings: CA1063, CA1861, CA2201, VSTHRD003, S104, S125, S2699

**Package References**:
- `Microsoft.TemplateEngine.Authoring.TemplateVerifier` - For template verification
- `Microsoft.TemplateEngine.TestHelper` - For template testing utilities

**Project References**:
- `TestUtilities` - Common test utilities

## Next Steps

When actual template content is ready to be added:

1. **Create Template Directory Structure**:
   - Create template directories under `src/ProjectTemplates/Microsoft.Agents.AI.Templates/src/`
   - Each template should have a `.template.config/` directory with a `template.json` file

2. **Update Source Project**:
   - Add `<Content>` items to the `.csproj` file for the new template directories
   - Define exclusion patterns to match the template structure
   - Update `THIRD-PARTY-NOTICES.TXT` if the templates use third-party libraries

3. **Add Template Tests**:
   - Remove the `Skip` attribute from tests in `AgentTemplateSnapshotTests.cs`
   - Add new test methods for different template configurations
   - Update the `_verificationExcludePatterns` array to match template output patterns
   - Update the template location and short name in test methods

4. **Generate Snapshots**:
   - Run the tests to generate initial snapshots
 - Use `DiffEngineTray` to review and accept the generated snapshots

5. **Add Execution Tests** (optional):
   - Create an `AgentTemplateExecutionTests.cs` file similar to `AIChatWebExecutionTests.cs` if you want to test that generated templates compile and run

## Integration with Build System

The project uses the `GenerateTemplateContent` approach for generating template content, which is consistent with other template projects in the repository.

The test infrastructure is designed to:
- Use the locally built .NET SDK from the repository
- Reference locally built NuGet packages
- Support debugging through the TemplateSandbox directory
- Verify template output matches expected snapshots

## Validation

All created files have been validated:
- Projects compile successfully
- No build errors or warnings
- Infrastructure follows repository conventions
- Namespace consistency (`Microsoft.Agents.AI.Templates.Tests`)
