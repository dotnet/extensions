# Release History

## 10.1.0-preview.1

- Introduced `SectionChunker` class for treating each document section as a separate entity (https://github.com/dotnet/extensions/pull/7015)
- Extended `IngestionPipeline<T>` with a new `ProcessAsync(IAsyncEnumerable<IngestionDocument>)` overload that enables processing documents without a file system reader. The `IngestionDocumentReader` has been moved from the constructor to a parameter on the file-system-oriented `ProcessAsync` overloads.

## 10.0.0-preview.1

- Initial preview release
