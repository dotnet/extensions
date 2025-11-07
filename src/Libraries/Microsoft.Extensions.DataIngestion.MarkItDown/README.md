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

### Creating a MarkItDownReader for Data Ingestion (Local Process)

Use `MarkItDownReader` to convert documents using the MarkItDown executable installed locally:

```csharp
using Microsoft.Extensions.DataIngestion;

IngestionDocumentReader reader =
    new MarkItDownReader(new FileInfo(@"pathToMarkItDown.exe"), extractImages: true);

using IngestionPipeline<string> pipeline = new(reader, CreateChunker(), CreateWriter());
```

### Creating a MarkItDownMcpReader for Data Ingestion (MCP Server)

Use `MarkItDownMcpReader` to convert documents using a MarkItDown MCP server:

```csharp
using Microsoft.Extensions.DataIngestion;

// Connect to a MarkItDown MCP server (e.g., running in Docker)
IngestionDocumentReader reader =
    new MarkItDownMcpReader(new Uri("http://localhost:3001/sse"));

using IngestionPipeline<string> pipeline = new(reader, CreateChunker(), CreateWriter());
```

The MarkItDown MCP server can be run using Docker:

```bash
docker run -p 3001:3001 mcp/markitdown
```

Or installed via pip:

```bash
pip install markitdown-mcp-server
markitdown-mcp --http --host 0.0.0.0 --port 3001
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
