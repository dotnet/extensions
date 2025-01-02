# Release History

## 9.0.1-preview.1.24570.5

- Changed `IChatClient`/`IEmbeddingGenerator`.`GetService` to be non-generic.
- Added `ToChatCompletion` / `ToChatCompletionUpdate` extension methods for `IEnumerable<StreamingChatCompletionUpdate>` / `IAsyncEnumerable<StreamingChatCompletionUpdate>`, respectively.
- Added `ToStreamingChatCompletionUpdates` instance method to `ChatCompletion`.
- Added `IncludeTypeInEnumSchemas`, `DisallowAdditionalProperties`, `RequireAllProperties`, and `TransformSchemaNode` options to `AIJsonSchemaCreateOptions`.
- Fixed a Native AOT warning in `AIFunctionFactory.Create`.
- Fixed a bug in `AIJsonUtilities` in the handling of Boolean schemas.
- Improved the `ToString` override of `ChatMessage` and `StreamingChatCompletionUpdate` to include all `TextContent`, and of `ChatCompletion` to include all choices.
- Added `DebuggerDisplay` attributes to `DataContent` and `GeneratedEmbeddings`.
- Improved the documentation.
 
## 9.0.0-preview.9.24556.5

- Added a strongly-typed `ChatOptions.Seed` property.
- Improved `AdditionalPropertiesDictionary` with a `TryAdd` method, a strongly-typed `Enumerator`, and debugger-related attributes for improved debuggability.
- Fixed `AIJsonUtilities` schema generation for Boolean schemas.

## 9.0.0-preview.9.24525.1

- Lowered the required version of System.Text.Json to 8.0.5 when targeting net8.0 or older.
- Annotated `FunctionCallContent.Exception` and `FunctionResultContent.Exception` as `[JsonIgnore]`, such that they're ignored when serializing instances with `JsonSerializer`. The corresponding constructors accepting an `Exception` were removed.
- Annotated `ChatCompletion.Message` as `[JsonIgnore]`, such that it's ignored when serializing instances with `JsonSerializer`.
- Added the `FunctionCallContent.CreateFromParsedArguments` method.
- Added the `AdditionalPropertiesDictionary.TryGetValue<T>` method.
- Added the `StreamingChatCompletionUpdate.ModelId` property and removed the `AIContent.ModelId` property.
- Renamed the `GenerateAsync` extension method on `IEmbeddingGenerator<,>` to `GenerateEmbeddingsAsync` and updated it to return `Embedding<T>` rather than `GeneratedEmbeddings`.
- Added `GenerateAndZipAsync` and `GenerateEmbeddingVectorAsync` extension methods for `IEmbeddingGenerator<,>`.
- Added the `EmbeddingGeneratorOptions.Dimensions` property.
- Added the `ChatOptions.TopK` property.
- Normalized `null` inputs in `TextContent` to be empty strings.

## 9.0.0-preview.9.24507.7

Initial Preview
