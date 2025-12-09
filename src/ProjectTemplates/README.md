# Project Template Local Development and Maintenance

## Updating project template JavaScript dependencies

To update project template JavaScript dependencies:
1. Install a recent build of Node.js
2. Update the `package.json` file with added or updated dependencies
3. Run the following commands from this directory:
    ```sh
    npm install
    npm run copy-dependencies
    ```

You'll need to authenticate to the dotnet-public-npm feed (this cannot be done by a community contributor), otherwise you'll get errors like "code E401 Unable to authenticate".
Install and run `artifacts-npm-credprovider` as described in the [Azure Artifacts docs](https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-public-npm/connect).

To add a new dependency, run `npm install <package-name>` and update the `scripts` section in `package.json` to specify how the new dependency should be copied into its template.

## Component governance

There are two types of template dependencies that need to get scanned for component governance (CG):
* .NET dependencies (specified via `<PackageReference />` in each `.csproj` file)
* JS dependencies (everything in the `wwwroot/lib` folder of the `.Web` project)

There are template execution tests in the `test/ProjectTemplates` folders that create, restore, and build each possible variation of the project templates. These tests execute before the CG step of the internal CI pipeline, which scans the build artifacts from each generated project (namely the `project.assets.json` file and the local NuGet package cache) to detect which .NET dependencies got pulled in.

However, CG can't detect JS dependencies by scanning execution test output, because the generated projects don't contain manifests describing JS dependencies. Instead, we have a `package.json` and `package-lock.json` in the same folder as this README that define which JS dependencies get included in the template and how they get copied into template content (see previous section in this document). CG then automatically tracks packages listed in this `package-lock.json`.

## Build the templates using just-built library package versions

By default the templates use just-built versions of library packages from this repository, so NuGet packages must be produced before the templates can be run:

```pwsh
.\build.cmd -vs AI -noLaunch # Generate an SDK.sln for projects matching the AI filter
.\build.cmd -build -pack     # Build a NuGet package for each project in the generated SDK.sln
```

Once the library packages are built, the template packages can be built with references to the local package versions using the following commands:

```pwsh
.\build.cmd -pack -projects .\src\ProjectTemplates\Microsoft.Agents.AI.ProjectTemplates\Microsoft.Extensions.AI.Templates.csproj
.\build.cmd -pack -projects .\src\ProjectTemplates\Microsoft.Extensions.AI.Templates\Microsoft.Extensions.AI.Templates.csproj
```

## Build the templates using pinned library package versions

The templates can also be built to reference pinned versions of the library packages. This approach is used when a templates package is updated off-cycle from the library packages. The pinned versions are hard-coded in the `GeneratedContent.targets` file in this directory. To build the templates package using the pinned versions, run:

```pwsh
.\build.cmd -pack -projects .\src\ProjectTemplates\Microsoft.Agents.AI.ProjectTemplates\Microsoft.Agents.AI.ProjectTemplates.csproj /p:TemplateUsePinnedPackageVersions=true
.\build.cmd -pack -projects .\src\ProjectTemplates\Microsoft.Extensions.AI.Templates\Microsoft.Extensions.AI.Templates.csproj /p:TemplateUsePinnedPackageVersions=true
```

Setting `/p:TemplateUsePinnedPackageVersions=true` will apply three different categories of pinned package versions:

1. Packages from this repository that are _not_ part of `Microsoft.Extensions.AI*`, namely `Microsoft.Extensions.Http.Resilience`
2. Packages from this repository that _are_ part of `Microsoft.Extensions.AI*`
3. The `Microsoft.EntityFrameworkCoreSqlite` package

## Installing the templates locally

After building the templates package using one of the approaches above, it can be installed locally. **Note:** Since package versions don't change between local builds, the recommended steps include clearing the `Microsoft.Extensions.AI*` and `Microsoft.Agents.AI*` packages from your local nuget cache.

**Note:** For the following commands to succeed, you'll need to either install a compatible .NET SDK globally or prepend the repo's generated `.dotnet` folder to the PATH environment variable.

```pwsh
# Uninstall any existing version of the templates
dotnet new uninstall Microsoft.Agents.AI.ProjectTemplates
dotnet new uninstall Microsoft.Extensions.AI.Templates

# Clear the packages from the NuGet cache since the local package version does not change
Remove-Item ~\.nuget\packages\Microsoft.Agents.AI* -Recurse -Force
Remove-Item ~\.nuget\packages\Microsoft.Extensions.AI* -Recurse -Force

# Install the templates from the generated .nupkg file (in the artifacts/packages folder)
dotnet new install .\artifacts\packages\Debug\Shipping\Microsoft.Agents.AI.ProjectTemplates*.nupkg
dotnet new install .\artifacts\packages\Debug\Shipping\Microsoft.Extensions.AI.Templates*.nupkg
```

Finally, create a project from the template and run it:

```pwsh
dotnet new aiagent-webapi `
    [--provider <azureopenai | githubmodels | ollama | openai>] `
    [--managed-identity]

# or

dotnet new aichatweb `
    [--provider <azureopenai | githubmodels | ollama | openai>] `
    [--vector-store <azureaisearch | local | qdrant>] `
    [--aspire] `
    [--managed-identity]

# If using `--aspire`, cd into the *AppHost directory
# Follow the instructions in the generated README for setting the necessary user-secrets

dotnet run
```
