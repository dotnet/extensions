# Microsoft.Extensions.DataIngestion.MarkItDown

Provides an implementation of the `IngestionDocumentReader` class for the [MarkItDown](https://github.com/microsoft/markitdown/) utility.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.DataIngestion.MarkItDown --prerelease
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DataIngestion.MarkItDown" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Examples

### Creating a MarkItDownReader for Data Ingestion

```csharp
using Microsoft.Extensions.DataIngestion;

IngestionDocumentReader reader =
    new MarkItDownReader(new FileInfo(@"pathToMarkItDown.exe"), extractImages: true);

using IngestionPipeline<string> pipeline = new(reader, CreateChunker(), CreateWriter());
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
