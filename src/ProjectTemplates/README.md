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

# Testing AI templates locally

Regardless of how you run AI templates locally, you must first generate NuGet packages for `Microsoft.Extensions.AI` projects defined within this repo:
```sh
.\build.cmd -vs AI -noLaunch # Generate an SDK.sln for `Microsoft.Extensions.AI*` projects
.\build.cmd -build -pack     # Build a NuGet package for each project
```

## Running directly within the repo

Navigate to the `Microsoft.Extensions.AI.Templates` folder and run:
```sh
dotnet build
```

This will generate the necessary template content to build and run AI templates from within this repo.

Now, you can navigate to a folder containg a template's `.csproj` file and run:
```sh
dotnet run
```

## Installing the templates locally

First, create the template NuGet package by project by navigating to the `Microsoft.Extensions.AI.Templates` folder and running:
```sh
dotnet pack
```

Then, navigate to the directory where you'd like to create the test project and run the following commands:
```sh
dotnet new uninstall Microsoft.Extensions.AI.Templates       # Uninstall any existing version of the templates
dontet new install "<PATH_TO_TEMPLATE_NUPKG>" --debug:reinit # Install the template from the generated .nupkg file (in the artifacts/packages folder)
```

Then, create a project from the template and run it:
```sh
dotnet new chat # (specify options as necessary)
dotnet run
```

**Note:** If the `Microsoft.Extensions.AI*` packages aren't already in your local NuGet cache, you might need to add the `artifacts/packages/Debug/Shipping` folder as a nuget package source in order to build the templates successfully. To do this, run:
```sh
dotnet new nugetconfig
dotnet nuget add source "<SHIPPING_PACKAGES_FOLDER>" -n local
```
