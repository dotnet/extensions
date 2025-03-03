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

# Installing the templates locally

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
dotnet new chat # (specify options as necessary)
dotnet run
```
