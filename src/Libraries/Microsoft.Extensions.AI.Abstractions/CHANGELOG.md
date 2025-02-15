# Release History

## 9.3.0-TODO

- Renamed `IChatClient.Complete{Streaming}Async` to `IChatClient.Get{Streaming}ResponseAsync`. This is to avoid confusion with "Complete" being about stopping an operation, as well as to avoid tying the methods to a particular implementation detail of how responses are generated. Along with this, renamed `ChatCompletion` to `ChatResponse`, `StreamingChatCompletionUpdate` to `ChatResponseUpdate`, `CompletionId` to `ResponseId`, `ToStreamingChatCompletionUpdates` to `ToChatResponseUpdates`, and `ToChatCompletion{Async}` to `ToChatResponse{Async}`.
- Removed `IChatClient.Metadata` and `IEmbeddingGenerator.Metadata`. The `GetService` method may be used to retrieve `ChatClientMetadata` and `EmbeddingGeneratorMetadata`, respectively.
- Added overloads of `Get{Streaming}ResponseAsync` that accept a single `ChatMessage` (in addition to the other overloads that accept a `List<ChatMessage>` or a `string`).
- Added `ChatThreadId` properties to `ChatOptions`, `ChatResponse`, and `ChatResponseUpdate`. `IChatClient` can now be used in both stateful and stateless modes of operation, such as with agents that maintain server-side chat history.
- Made `ChatOptions.ToolMode` nullable and added a `None` option.
- Changed `UsageDetails`'s properties from `int?` to `long?`.
- Removed `DataContent.ContainsData`; `DataContent.Data.HasValue` may be used instead.
- Removed `ImageContent` and `AudioContent`; the base `DataContent` should now be used instead, with a new `DataContent.MediaTypeStartsWith` helper for routing based on media type.
- Removed setters on `FunctionCallContent` and `FunctionResultContent` properties where the value is supplied to the constructor.
- Removed `FunctionResultContent.Name`.
- Augmented the base `AITool` with `Name`, `Description`, and `AdditionalProperties` virtual properties.
- Added a `CodeInterpreterTool` for use with services that support server-side code execution.
- Changed `AIFunction`'s schema representation to be for the whole function rather than per parameter, and exposed corresponding methods on `AIJsonUtilities`, e.g. `CreateFunctionJsonSchema`.
- Removed `AIFunctionParameterMetadata` and `AIFunctionReturnParameterMetadata` classes and corresponding properties on `AIFunction` and `AIFunctionFactoryCreateOptions`, replacing them with a `MethodInfo?`. All relevant metadata, such as the JSON schema for the function, are moved to properties directly on `AIFunction`.
- Renamed `AIFunctionFactoryCreateOptions` to `AIFunctionFactoryOptions` and made all its properties nullable.
- Changed `AIJsonUtilities.DefaultOptions` to use relaxed JSON escaping.
- Made `IEmbeddingGenerator<TInput, TEmbedding>` contravariant on `TInput`.

## 9.1.0-preview.1.25064.3

- Added `AdditionalPropertiesDictionary<TValue>` and changed `UsageDetails.AdditionalProperties` to be named `AdditionalCounts` and to be of type `AdditionalPropertiesDictionary<long>`.
- Updated `FunctionCallingChatClient` to sum all `UsageDetails` token counts from all intermediate messages.
- Fixed JSON schema generation for floating-point types.
- Added `AddAIContentType` for enabling custom `AIContent`-derived types to participate in polymorphic serialization.

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
