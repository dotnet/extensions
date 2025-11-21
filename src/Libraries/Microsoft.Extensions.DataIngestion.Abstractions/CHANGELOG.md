# Release History

## 10.0.0-preview.1

- Initial preview release of Microsoft.Extensions.DataIngestion.Abstractions
- Introduced `IngestionDocument` class for representing format-agnostic document containers
- Introduced `IngestionDocumentElement` abstract base class for document elements
- Introduced document element types:
  - `IngestionDocumentSection` - Represents a section or page in a document
  - `IngestionDocumentParagraph` - Represents a paragraph
  - `IngestionDocumentHeader` - Represents a header with optional level
  - `IngestionDocumentFooter` - Represents a footer
  - `IngestionDocumentTable` - Represents a table with 2D cell array
  - `IngestionDocumentImage` - Represents an image with optional binary content and alternative text
- Introduced `IngestionChunk<T>` class for representing content chunks
- Introduced `IngestionChunker<T>` abstract base class for splitting documents into chunks
- Introduced `IngestionDocumentReader` abstract base class for reading source content and converting to documents
- Introduced `IngestionDocumentProcessor` abstract base class for processing documents
- Introduced `IngestionChunkProcessor<T>` abstract base class for processing chunks
- Introduced `IngestionChunkWriter<T>` abstract base class for writing chunks to storage
