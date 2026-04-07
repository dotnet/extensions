# Microsoft.Extensions.DataIngestion

.NET developers need to efficiently process, chunk, and retrieve information from diverse document formats while preserving semantic meaning and structural context. The `Microsoft.Extensions.DataIngestion` libraries provide a unified approach for representing document ingestion components.

## The packages

The [Microsoft.Extensions.DataIngestion.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DataIngestion.Abstractions) package provides the core exchange types, including [`IngestionDocument`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.ingestiondocument), [`IngestionChunker<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.ingestionchunker-1), [`IngestionChunkProcessor<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.ingestionchunkprocessor-1), and [`IngestionChunkWriter<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.ingestionchunkwriter-1). Any .NET library that provides document processing capabilities can implement these abstractions to enable seamless integration with consuming code.

The [Microsoft.Extensions.DataIngestion](https://www.nuget.org/packages/Microsoft.Extensions.DataIngestion) package has an implicit dependency on the `Microsoft.Extensions.DataIngestion.Abstractions` package. This package enables you to easily integrate components such as enrichment processors, vector storage writers, and telemetry into your applications using familiar dependency injection and pipeline patterns. For example, it provides the [`SentimentEnricher`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.sentimentenricher), [`KeywordEnricher`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.keywordenricher), and [`SummaryEnricher`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.summaryenricher) processors that can be chained together in ingestion pipelines.

## Which package to reference

Libraries that provide implementations of the abstractions typically reference only `Microsoft.Extensions.DataIngestion.Abstractions`.

To also have access to higher-level utilities for working with document ingestion components, reference the `Microsoft.Extensions.DataIngestion` package instead (which itself references `Microsoft.Extensions.DataIngestion.Abstractions`). Most consuming applications and services should reference the `Microsoft.Extensions.DataIngestion` package along with one or more libraries that provide concrete implementations of the abstractions, such as `Microsoft.Extensions.DataIngestion.MarkItDown` or `Microsoft.Extensions.DataIngestion.Markdig`.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.DataIngestion --prerelease
```
Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DataIngestion" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Creating and using an ingestion pipeline

### Basic usage

Use `IngestionDocumentReader.ReadAsync` to read documents from files or a directory, and pass the result to `IngestionPipeline.ProcessAsync`:

```csharp
VectorStoreCollection<Guid, IngestionChunkVectorRecord<string>> collection =
    vectorStore.GetIngestionRecordCollection("chunks", dimensionCount: 1536);

using VectorStoreWriter<string, IngestionChunkVectorRecord<string>> writer = new(collection);
using IngestionPipeline<string> pipeline = new(chunker, writer);

// Read from a directory and ingest all matching files
MarkdownReader reader = new();
await foreach (var result in pipeline.ProcessAsync(reader.ReadAsync(directory, "*.md")))
{
    Console.WriteLine($"Processed '{result.DocumentId}': {(result.Succeeded ? "success" : "failed")}");
}
```

### Reading from a list of files

```csharp
IEnumerable<FileInfo> files = [ new FileInfo("doc1.md"), new FileInfo("doc2.md") ];
await foreach (var result in pipeline.ProcessAsync(reader.ReadAsync(files)))
{
    Console.WriteLine($"Processed '{result.DocumentId}': {(result.Succeeded ? "success" : "failed")}");
}
```

### Using the pipeline without a reader

You can also create documents directly and pass them to the pipeline without using a reader:

```csharp
async IAsyncEnumerable<IngestionDocument> GetDocumentsAsync()
{
    var document = new IngestionDocument("my-document-id");
    document.Sections.Add(new IngestionDocumentSection
    {
        Elements = { new IngestionDocumentParagraph("Document content goes here.") }
    });
    yield return document;
}

await foreach (var result in pipeline.ProcessAsync(GetDocumentsAsync()))
{
    Console.WriteLine($"Processed '{result.DocumentId}': {(result.Succeeded ? "success" : "failed")}");
}
```

### Basic usage

The simplest way to store ingestion chunks in a vector store is to use the `GetIngestionRecordCollection` extension method to create a collection, and then pass it to a `VectorStoreWriter`:

```csharp
VectorStoreCollection<Guid, IngestionChunkVectorRecord<string>> collection =
    vectorStore.GetIngestionRecordCollection("chunks", dimensionCount: 1536);

using VectorStoreWriter<string, IngestionChunkVectorRecord<string>> writer = new(collection);

await writer.WriteAsync(chunks);
```

### Custom metadata

To store custom metadata alongside each chunk, create a type derived from `IngestionChunkVectorRecord<TChunk>` with additional properties, and a `VectorStoreWriter` subclass that overrides `SetMetadata`:

```csharp
public class ChunkWithMetadata : IngestionChunkVectorRecord<string>
{
    [VectorStoreVector(1536)]
    public override string? Embedding => Content;

    [VectorStoreData(StorageName = "classification")]
    public string? Classification { get; set; }
}

public class MetadataWriter : VectorStoreWriter<string, ChunkWithMetadata>
{
    public MetadataWriter(VectorStoreCollection<Guid, ChunkWithMetadata> collection)
        : base(collection) { }

    protected override void SetMetadata(ChunkWithMetadata record, string key, object? value)
    {
        switch (key)
        {
            case nameof(ChunkWithMetadata.Classification):
                record.Classification = value as string;
                break;
            default:
                throw new UnreachableException($"Unknown metadata key: {key}");
        }
    }
}
```

### Custom collection schema

To map to a pre-existing collection that uses different storage names, create a `VectorStoreCollectionDefinition` manually:

```csharp
VectorStoreCollectionDefinition definition = new()
{
    Properties =
    {
        new VectorStoreKeyProperty(nameof(IngestionChunkVectorRecord<string>.Key), typeof(Guid))
            { StorageName = "my_key" },
        new VectorStoreVectorProperty(nameof(IngestionChunkVectorRecord<string>.Embedding), typeof(string), 1536)
            { StorageName = "my_embedding" },
        new VectorStoreDataProperty(nameof(IngestionChunkVectorRecord<string>.Content), typeof(string))
            { StorageName = "my_content" },
        new VectorStoreDataProperty(nameof(IngestionChunkVectorRecord<string>.Context), typeof(string))
            { StorageName = "my_context" },
        new VectorStoreDataProperty(nameof(IngestionChunkVectorRecord<string>.DocumentId), typeof(string))
            { StorageName = "my_documentid", IsIndexed = true },
    },
};

VectorStoreCollection<Guid, IngestionChunkVectorRecord<string>> collection =
    vectorStore.GetCollection<Guid, IngestionChunkVectorRecord<string>>("chunks", definition);
using VectorStoreWriter<string, IngestionChunkVectorRecord<string>> writer = new(collection);
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
