# Microsoft.Extensions.StaticAnalysis

A curated set of code analyzers and code analyzer settings.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.StaticAnalysis
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.StaticAnalysis" Version="[CURRENTVERSION]" >
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

## Usage Example

On install, a warning will be displayed that `The StaticAnalysisCodeType property is not defined, assuming 'General'`. The General set of diagnostics is enabled by default. To select a different set of diagnostics (or hide the warning) add the `StaticAnalysisCodeType` property to your project as follows.

```XML
  <PropertyGroup>
    <StaticAnalysisCodeType>General</StaticAnalysisCodeType>
  </PropertyGroup>
```

## Available Sets Of Diagnostics

Different pre-defined sets of diagnostics are available depending on the type of project being built. These can be specified in the StaticAnalysisCodeType property:
- Benchmark: Projects used for benchmarking.
- General: Any type of project.
- NonProdExe: Projects that produce an exe for non-production use.
- NonProdLib: Projects that produce a library (dll) for non-production use.
- ProdExe: Projects that produce an exe for production use.
- ProdLib: Projects that produce a library (dll) for production use.
- Test: Projects used for testing.

Each of these also has an optional `-Tier1` and a `-Tier2` variant (e.g. `General-Tier1`).
- `Tier1` enables only the most important diagnostics from this set.
- `Tier2` includes Tier1 diagnostics and others that aren't as critical.
- The names without `Tier` suffixes include all diagnostics from tier's 1 and 2, and any others that are applicable.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
