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

## Build the templates using just-built library package versions

By default the templates use just-built versions of library packages from this repository, so NuGet packages must be produced before the templates can be run:

```pwsh
.\build.cmd -vs AI -noLaunch # Generate an SDK.sln for Microsoft.Extensions.AI* projects
.\build.cmd -build -pack     # Build a NuGet package for each project
```

Once the library packages are built, the `Microsoft.Extensions.AI.Templates` package is built with references to the local package versions using the following commands:

```pwsh
.\build.cmd -pack -projects .\src\ProjectTemplates\Microsoft.Extensions.AI.Templates\Microsoft.Extensions.AI.Templates.csproj
```

## Build the templates using pinned library package versions

The templates can also be built to reference pinned versions of the library packages. This approach is used when the `Microsoft.Extensions.AI.Templates` package is updated off-cycle from the library packages. The pinned versions are hard-coded in the `GeneratedContent.targets` file in this directory. To build the templates package using the pinned versions, run:

```pwsh
.\build.cmd -pack -projects .\src\ProjectTemplates\Microsoft.Extensions.AI.Templates\Microsoft.Extensions.AI.Templates.csproj /p:TemplateUsePinnedPackageVersions=true
```

Setting `/p:TemplateUsePinnedPackageVersions=true` will apply three different categories of pinned package versions:

1. Packages from this repository that are _not_ part of `Microsoft.Extensions.AI*`, namely `Microsoft.Extensions.Http.Resilience`
2. Packages from this repository that _are_ part of `Microsoft.Extensions.AI*`
3. The `Microsoft.EntityFrameworkCoreSqlite` package

## Installing the templates locally

After building the templates package using one of the approaches above, it can be installed locally. **Note:** Since package versions don't change between local builds, the recommended steps include clearing the `Microsoft.Extensions.AI*` packages from your local nuget cache.

**Note:** For the following commands to succeed, you'll need to either install a compatible .NET SDK globally or prepend the repo's generated `.dotnet` folder to the PATH environment variable.

```pwsh
# Uninstall any existing version of the templates
dotnet new uninstall Microsoft.Extensions.AI.Templates

# Clear the Microsoft.Extensions.AI packages from the NuGet cache since the local package version does not change
Remove-Item ~\.nuget\packages\Microsoft.Extensions.AI* -Recurse -Force

# Install the template from the generated .nupkg file (in the artifacts/packages folder)
dotnet new install .\artifacts\packages\Debug\Shipping\Microsoft.Extensions.AI.Templates*.nupkg
```

Finally, create a project from the template and run it:

```pwsh
dotnet new aichatweb `
    [--provider <azureopenai | githubmodels | ollama | openai>] `
    [--vector-store <azureaisearch | local | qdrant>] `
    [--aspire] `
    [--managed-identity]

# If using `--aspire`, cd into the *AppHost directory
# Follow the instructions in the generated README for setting the necessary user-secrets

dotnet run
```
