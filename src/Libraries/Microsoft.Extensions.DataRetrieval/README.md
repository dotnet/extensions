# Microsoft.Extensions.DataRetrieval

Retrieval pipeline orchestration for .NET RAG (Retrieval-Augmented Generation) applications. Built on the abstractions defined in [`Microsoft.Extensions.DataRetrieval.Abstractions`](../Microsoft.Extensions.DataRetrieval.Abstractions/).

## Key Components

| Type | Description |
|------|-------------|
| `RetrievalPipeline` | Orchestrates query processing → vector search → result processing with RRF dedup and tree-aware hierarchical search. |
| `RetrievalPipelineOptions` | Configuration for the pipeline (collection name, top-K, processors). |

## Features

- **Multi-query deduplication** — Reciprocal Rank Fusion (RRF) merges results from expanded query variants
- **Tree-aware retrieval** — hierarchical traversal from root summaries to leaf chunks
- **Extensible processors** — plug in custom `RetrievalQueryProcessor` and `RetrievalResultProcessor` implementations
- **OpenTelemetry** — built-in distributed tracing and structured logging

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.DataRetrieval --prerelease
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DataRetrieval" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).
