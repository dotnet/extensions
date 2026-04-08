# Microsoft.Extensions.DataIngestion.Abstractions

.NET developers need to efficiently process, chunk, and retrieve information from diverse document formats while preserving semantic meaning and structural context. The `Microsoft.Extensions.DataIngestion` libraries provide a unified approach for representing document ingestion components.

## The packages

The [Microsoft.Extensions.DataIngestion.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DataIngestion.Abstractions) package provides the core exchange types for both ingestion and retrieval.

### Ingestion types

[`IngestionDocument`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.ingestiondocument), [`IngestionChunker<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.ingestionchunker-1), [`IngestionChunkProcessor<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.ingestionchunkprocessor-1), and [`IngestionChunkWriter<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dataingestion.ingestionchunkwriter-1). Any .NET library that provides document processing capabilities can implement these abstractions to enable seamless integration with consuming code.

### Retrieval types

The retrieval abstractions are the symmetric counterpart to ingestion — they define how applications query, process, and rank results from vector stores:

| Type | Description |
|------|-------------|
| `RetrievalQuery` | Query text with support for variants (multi-query expansion) and metadata for inter-processor communication. |
| `RetrievalChunk` | A single retrieved chunk with content, relevance score, and record metadata. |
| `RetrievalResults` | Collection of retrieved chunks with pipeline-level metadata (e.g., CRAG scores, reranking info). |
| `RetrievalQueryProcessor` | Abstract base class for pre-search processors (query expansion, HyDE, adaptive routing). |
| `RetrievalResultProcessor` | Abstract base class for post-search processors (re-ranking, CRAG quality gating). |
| `ISearchReranker` | Interface for re-ranking strategies (LLM-based, cross-encoder, ONNX models). |

### Implementation package

The [Microsoft.Extensions.DataIngestion](https://www.nuget.org/packages/Microsoft.Extensions.DataIngestion) package has an implicit dependency on the `Microsoft.Extensions.DataIngestion.Abstractions` package. This package provides pipeline orchestrators for both ingestion (`IngestionPipeline<T>`) and retrieval (`RetrievalPipeline`), along with dependency injection, telemetry, and processor chaining. The `RetrievalPipeline` supports query variant deduplication via Reciprocal Rank Fusion (RRF) and tree-aware hierarchical search.

## Which package to reference

Libraries that provide implementations of the abstractions typically reference only `Microsoft.Extensions.DataIngestion.Abstractions`.

To also have access to higher-level utilities for working with document ingestion components, reference the `Microsoft.Extensions.DataIngestion` package instead (which itself references `Microsoft.Extensions.DataIngestion.Abstractions`). Most consuming applications and services should reference the `Microsoft.Extensions.DataIngestion` package along with one or more libraries that provide concrete implementations of the abstractions, such as `Microsoft.Extensions.DataIngestion.MarkItDown` or `Microsoft.Extensions.DataIngestion.Markdig`.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.DataIngestion.Abstractions --prerelease
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DataIngestion.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Documentation

Refer to the [Microsoft.Extensions.DataIngestion libraries documentation](https://learn.microsoft.com/dotnet/dataingestion/microsoft-extensions-dataingestion) for more information and API usage examples.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
