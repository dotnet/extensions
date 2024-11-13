# Release History

## 9.0.0-preview.9.24556.5

- Added `UseEmbeddingGenerationOptions` and corresponding `ConfigureOptionsEmbeddingGenerator`.

## 9.0.0-preview.9.24525.1

- Added new `AIJsonUtilities` and `AIJsonSchemaCreateOptions` classes.
- Made `AIFunctionFactory.Create` safe for use with Native AOT.
- Simplified the set of `AIFunctionFactory.Create` overloads.
- Changed the default for `FunctionInvokingChatClient.ConcurrentInvocation` from `true` to `false`.
- Improved the readability of JSON generated as part of logging.
- Fixed handling of generated JSON schema names when using arrays or generic types.
- Improved `CachingChatClient`'s coalescing of streaming updates, including reduced memory allocation and enhanced metadata propagation.
- Updated `OpenTelemetryChatClient` and `OpenTelemetryEmbeddingGenerator` to conform to the latest 1.28.0 draft specification of the Semantic Conventions for Generative AI systems.
- Improved `CompleteAsync<T>`'s structured output support to handle primitive types, enums, and arrays.

## 9.0.0-preview.9.24507.7

Initial Preview
