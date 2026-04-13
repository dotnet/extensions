# Microsoft.Extensions.DataRetrieval.Abstractions

Abstractions for building composable retrieval pipelines in .NET RAG (Retrieval-Augmented Generation) applications. The retrieval abstractions are the symmetric counterpart to [`Microsoft.Extensions.DataIngestion`](../Microsoft.Extensions.DataIngestion.Abstractions/) — ingestion writes data in, retrieval reads relevant data out.

## Core Types

| Type | Description |
|------|-------------|
| `RetrievalQuery` | Query text with support for variants (multi-query expansion) and metadata for inter-processor communication. |
| `RetrievalChunk` | A single retrieved chunk with content, relevance score, and record metadata. |
| `RetrievalResults` | Collection of retrieved chunks with pipeline-level metadata (e.g., CRAG scores, reranking info). |
| `RetrievalQueryProcessor` | Abstract base class for pre-search processors (query expansion, HyDE, adaptive routing). |
| `RetrievalResultProcessor` | Abstract base class for post-search processors (re-ranking, CRAG quality gating). |
| `IReranker` | Interface for re-ranking strategies (LLM-based, cross-encoder, ONNX models). |

## Which package to reference

Libraries that provide implementations of the abstractions (e.g., custom query processors, re-rankers) should reference only `Microsoft.Extensions.DataRetrieval.Abstractions`.

Applications that need the full pipeline orchestrator (`RetrievalPipeline`) should reference `Microsoft.Extensions.DataRetrieval` instead (which itself references the abstractions).

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.DataRetrieval.Abstractions --prerelease
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DataRetrieval.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
