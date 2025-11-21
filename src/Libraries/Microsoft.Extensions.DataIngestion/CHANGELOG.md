# Release History

## 10.1.0-preview.1

- Introduced `SectionChunker` class for treating each document section as a separate entity (https://github.com/dotnet/extensions/pull/7015)

## 10.0.0-preview.1

- Initial preview release of Microsoft.Extensions.DataIngestion
- Introduced `IngestionPipeline<T>` class for orchestrating document ingestion workflows
- Introduced `IngestionPipelineOptions` class for configuring pipeline behavior
- Introduced `IngestionResult` class for representing ingestion operation results
- Introduced chunker implementations:
  - `HeaderChunker` - Splits documents based on headers and their levels
  - `ElementsChunker` - Splits documents into chunks of individual elements
  - `SemanticSimilarityChunker` - Splits documents based on semantic similarity using embeddings
- Introduced `IngestionChunkerOptions` class for configuring chunker behavior (token limits, overlap, etc.)
- Introduced document processors/enrichers:
  - `ClassificationEnricher` - Enriches document metadata with classifications
  - `KeywordEnricher` - Enriches document metadata with keywords
  - `SentimentEnricher` - Enriches document metadata with sentiment analysis
  - `SummaryEnricher` - Enriches document metadata with summaries
  - `ImageAlternativeTextEnricher` - Enriches images with alternative text descriptions
- Introduced `EnricherOptions` class for configuring enricher behavior
- Introduced `VectorStoreWriter<T>` class for writing chunks to vector stores
- Introduced `VectorStoreWriterOptions<T>` class for configuring vector store writing behavior
