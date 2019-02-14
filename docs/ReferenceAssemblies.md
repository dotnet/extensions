Reference assemblies
========================

Most projects in this solution have `ref` directory next to `src` that contains project and source code for reference assembly.
Reference assemblies contain public API surface of libraries and are used for ASP.NET Core targeting pack generation.

### When changing public API

Run `dotnet msbuild /t:GenerateReferenceSource` in that project's `src` directory

### When adding a new project

Run `.\eng\scripts\GenerateProjectList.ps1` from the repository root and `dotnet msbuild /t:GenerateReferenceSource` in that project's `src` directory

### To set project properties in reference assembly project

`ref.csproj` is automaticaly generated and shouldn't be edited. To set project properties on a reference assembly project place a `Directory.Build.props` next to it and add the properties there.

### My project doesn't need reference assembly

Set `<HasReferenceAssembly>false</HasReferenceAssembly>` in implementation (`src`) project and re-run `.\eng\scripts\GenerateProjectList.ps1`.

### Regenerate reference assemblies for all projects

Run `.\eng\scripts\GenerateReferenceAssemblies.ps1` from repository root.
