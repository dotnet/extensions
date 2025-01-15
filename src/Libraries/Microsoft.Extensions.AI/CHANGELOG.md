# Release History

## 9.1.0-preview.1.25064.3

- Added `FunctionInvokingChatClient.CurrentContext` to give functions access to detailed function invocation information.
- Updated `OpenTelemetryChatClient`/`OpenTelemetryEmbeddingGenerator` to conform to the latest 1.29.0 draft specification of the Semantic Conventions for Generative AI systems.
- Updated `FunctionInvokingChatClient` to emit an `Activity`/span around all interactions related to a single chat operation.

## 9.0.1-preview.1.24570.5

- Moved the `AddChatClient`, `AddKeyedChatClient`, `AddEmbeddingGenerator`, and `AddKeyedEmbeddingGenerator` extension methods to the `Microsoft.Extensions.DependencyInjection` namespace, changed them to register singleton instances instead of scoped instances, and changed them to support lambda-less chaining.
- Renamed `UseChatOptions`/`UseEmbeddingOptions` to `ConfigureOptions`, and changed the behavior to always invoke the delegate with a safely-mutable instance, either a new instance if the caller provided null, or a clone of the provided instance.
- Renamed the final `Use` method for building a builder to be named `Build`. The inner client instance is passed to the constructor and the `IServiceProvider` is optionally passed to the `Build` method.
- Added `AsBuilder` extension methods to `IChatClient`/`IEmbeddingGenerator` to create builders from the instances.
- Changed the `CachingChatClient`/`CachingEmbeddingGenerator`.`GetCacheKey` method to accept a `params ReadOnlySpan<object?>`, included the `ChatOptions`/`EmbeddingGeneratorOptions` as part of the caching key, and reduced memory allocation.
- Added support for anonymous delegating `IChatClient`/`IEmbeddingGenerator` implementations, with `Use` methods on `ChatClientBuilder`/`EmbeddingGeneratorBuilder` that enable the implementations of the core methods to be supplied as lambdas.
- Changed `UseLogging` to accept an `ILoggerFactory` rather than `ILogger`.
- Reversed the order of the `IChatClient`/`IEmbeddingGenerator` and `IServiceProvider` arguments to used by one of the `Use` overloads.
- Added logging capabilities to `FunctionInvokingChatClient`. `UseFunctionInvocation` now accepts an optional `ILoggerFactory`.
- Fixed the `FunctionInvokingChatClient` to include usage data for non-streaming completions in the augmented history.
- Fixed the `FunctionInvokingChatClient` streaming support to appropriately fail for multi-choice completions.
- Fixed the `FunctionInvokingChatClient` to stop yielding function calling content that was already being handled.
- Improved the documentation.

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
