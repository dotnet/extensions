# Microsoft.Extensions.DocumentExtraction.Abstractions

.NET developers need to turn documents such as scanned images and PDFs into structured, AI-ready content, recovering text along with layout, tables, figures, and coordinates in a provider-neutral way. The `Microsoft.Extensions.DocumentExtraction` libraries provide a unified approach for representing document-extraction components, complementing `Microsoft.Extensions.DataIngestion` (extraction pulls content out of documents; ingestion feeds that content into a retrieval pipeline).

## The packages

The [Microsoft.Extensions.DocumentExtraction.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DocumentExtraction.Abstractions) package provides the core exchange types, including [`IDocumentExtractionClient`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.documentextraction.idocumentextractionclient), [`DocumentExtractionResult`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.documentextraction.documentextractionresult), [`DocumentPage`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.documentextraction.documentpage), and the reading-order [`DocumentElement`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.documentextraction.documentelement) model (blocks, tables, and images with bounding regions and confidence). Any .NET library that provides a document-extraction engine can implement these abstractions to enable seamless integration with consuming code.

The [Microsoft.Extensions.DocumentExtraction](https://www.nuget.org/packages/Microsoft.Extensions.DocumentExtraction) package has an implicit dependency on the `Microsoft.Extensions.DocumentExtraction.Abstractions` package. This package enables you to easily integrate components such as logging, telemetry, and options configuration into your applications using familiar dependency injection and builder patterns.

## Which package to reference

Libraries that provide implementations of the abstractions typically reference only `Microsoft.Extensions.DocumentExtraction.Abstractions`.

To also have access to higher-level utilities for working with document-extraction clients, reference the `Microsoft.Extensions.DocumentExtraction` package instead (which itself references `Microsoft.Extensions.DocumentExtraction.Abstractions`). Most consuming applications and services should reference the `Microsoft.Extensions.DocumentExtraction` package along with a library that provides a concrete implementation of the abstractions.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.DocumentExtraction.Abstractions --prerelease
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DocumentExtraction.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Documentation

Refer to the [Microsoft.Extensions.DocumentExtraction libraries documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.documentextraction) for more information and API usage examples.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
