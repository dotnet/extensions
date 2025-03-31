# Updating project template JavaScript dependencies

To update project template JavaScript dependencies:
1. Install a recent build of Node.js
2. Update the `package.json` file with added or updated dependencies
3. Run the following commands from this directory:
    ```sh
    npm install
    npm run copy-dependencies
    ```

To add a new dependency, run `npm install <package-name>` and update the `scripts` section in `package.json` to specify how the new dependency should be copied into its template.

# Running AI templates

By default the templates use just-built versions of `Microsoft.Extensions.AI*` packages, so NuGet packages must be produced before the templates can be run:
```sh
.\build.cmd -vs AI -noLaunch # Generate an SDK.sln for Microsoft.Extensions.AI* projects
.\build.cmd -build -pack     # Build a NuGet package for each project
```

Alternatively, you can override the `TemplateMicrosoftExtensionsAIVersion` property (defined in the `GeneratedContent.targets` file in this directory) with a publicly-available version. This will disable the template generation logic that utilizes locally-built `Microsoft.Extensions.AI*` packages.

## Installing the templates locally

First, create the template NuGet package by running the following from the repo root:
```pwsh
.\build.cmd # If not done already
.\build.cmd -pack -projects .\src\ProjectTemplates\Microsoft.Extensions.AI.Templates\Microsoft.Extensions.AI.Templates.csproj
```

**Note:** Since package versions don't change between local builds, it may be necessary to occasionally delete `Microsoft.Extensions.AI*` packages from your local nuget cache, especially if you're making changes to these packages. An example of how to do this in PowerShell is:
```pwsh
Remove-Item ~\.nuget\packages\microsoft.extensions.ai* -Recurse -Force
```

**Note:** For the following commands to succeed, you'll need to either install a compatible .NET SDK globally or prepend the repo's generated `.dotnet` folder to the PATH environment variable.

Then, navigate to the directory where you'd like to create the test project and run the following commands:
```sh
dotnet new uninstall Microsoft.Extensions.AI.Templates       # Uninstall any existing version of the templates
dotnet new install "<PATH_TO_TEMPLATE_NUPKG>" --debug:reinit # Install the template from the generated .nupkg file (in the artifacts/packages folder)
```

Finally, create a project from the template and run it:
```sh
dotnet new aichatweb [-A <azureopenai | githubmodels | ollama | openai>] [-V <azureaisearch | local>]
dotnet run
```

## Running the templates directly within the repo

The project templates are structured in a way that allows them to be run directly within the repo.

**Note:** For the following commands to succeed, you'll need to either install a compatible .NET SDK globally or prepend the repo's generated `.dotnet` folder to the PATH environment variable.

Navigate to the `Microsoft.Extensions.AI.Templates` folder and run:
```sh
dotnet build
```

This will generate the necessary template content to build and run AI templates from within this repo.

Now, you can navigate to a folder containing a template's `.csproj` file and run:
```sh
dotnet run
```
