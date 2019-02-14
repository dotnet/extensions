Reference assemblies
========================

Most projects in this solution have `ref` directory next to `src` that contains project and source code for reference assembly.
Reference assemblies contain public API surface of libraries and are used for ASP.NET Core targeting pack generation.

### When changing public API

Run `dotnet msbuild /t:GenerateReferenceSource` in projects `src` directory

### When adding a new project

Run `.\eng\scripts\GenerateProjectList.ps1` from repository root and `dotnet msbuild /t:GenerateReferenceSource` in projects `src` directory

### To set project properties in reference assembly project

`ref.csproj` is automaticaly generated and shouldn't be edited. To set project properties on reference assembly project place `Directory.Build.props` next to it and add properties there.

### My project doesn't need reference assembly

Set `<HasReferenceAssembly>false</HasReferenceAssembly>` in implementation (`src`) project and re-run `.\eng\scripts\GenerateProjectList.ps1`.