# Release History

## 9.3.0-preview.1.25114.11

- Ensures that all yielded `ChatResponseUpdates` include a `ResponseId`.
- Ensures that error HTTP status codes are correctly propagated as exceptions.

## 9.1.0-preview.1.25064.3

- Added support for function calling when doing streaming operations.
- Added support for native structured output.

## 9.0.1-preview.1.24570.5

- Made the `ToolCallJsonSerializerOptions` property non-nullable.

## 9.0.0-preview.9.24525.1

- Lowered the required version of System.Text.Json to 8.0.5 when targeting net8.0 or older.
- Added additional constructors to `OllamaChatClient` and `OllamaEmbeddingGenerator` that accept `string` endpoints, in addition to the existing ones accepting `Uri` endpoints.

## 9.0.0-preview.9.24507.7

Initial Preview
