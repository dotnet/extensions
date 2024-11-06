# JsonSchemaExporter

Provides a polyfill for the [.NET 9 `JsonSchemaExporter` component](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/extract-schema) that is compatible with all supported targets using System.Text.Json version 8.

To use this in your project, add the following to your `.csproj` file:

```xml
<PropertyGroup>
  <InjectJsonSchemaExporterOnLegacy>true</InjectJsonSchemaExporterOnLegacy>
</PropertyGroup>
```
