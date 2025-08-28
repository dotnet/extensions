# Release History

## 9.8.0-preview.1.25412.6

- Updated to depend on OpenAI 2.3.0.
- Added more conversion helpers for converting bidirectionally between Microsoft.Extensions.AI messages and OpenAI messages.
- Fixed handling of multiple response messages in the Responses `IChatClient`.
- Updated to accommodate the additions in `Microsoft.Extensions.AI.Abstractions`.

## 9.7.1-preview.1.25365.4

- Added some conversion helpers for converting Microsoft.Extensions.AI messages to OpenAI messages.
- Enabled specifying "strict" via ChatOptions for OpenAI clients.

## 9.7.0-preview.1.25356.2

- Updated to depend on OpenAI 2.2.0.
- Added conversion helpers from `AIFunction` to various OpenAI tool representations.
- Added `AsIChatClient` extension method for OpenAI's `AssistantClient`, enabling `IChatClient` to be used with OpenAI Assistants.
- Tweaked how JSON schemas for functions are transformed for better compatibility with OpenAI `strict` constraints.
- Improved handling of `RawRepresentation` in `IChatClients` for Responses and Chat Completion APIs.
- Improved `ISpeechToTextClient` implementation to support streaming transcriptions.
- Updated to accommodate the additions in `Microsoft.Extensions.AI.Abstractions`.

## 9.6.0-preview.1.25310.2

- Updated to accommodate the additions in `Microsoft.Extensions.AI.Abstractions`.

## 9.5.0-preview.1.25265.7

- Added PDF support to `IChatClient` implementations.
- Disabled use of `strict` schema handling by default.
- Added support for creating `ErrorContent` in `IChatClient` implementations, such as for refusals.
- Updated to accommodate the changes in `Microsoft.Extensions.AI.Abstractions`.

## 9.4.4-preview.1.25259.16

- Made `IChatClient` implementation more resilient with non-OpenAI services.
- Added `ErrorContent` to represent refusals.
- Updated to accommodate the changes in `Microsoft.Extensions.AI.Abstractions`.

## 9.4.3-preview.1.25230.7

- Reverted previous change that enabled `strict` schemas by default.
- Updated `IChatClient` implementations to support `DataContent`s for PDFs.
- Updated to accommodate the changes in `Microsoft.Extensions.AI.Abstractions`.

## 9.4.0-preview.1.25207.5

- Updated to OpenAI 2.2.0-beta-4.
- Added `AsISpeechToTextClient` extension method for `AudioClient`.
- Removed `AsChatClient(OpenAIClient)`/`AsEmbeddingGenerator(OpenAIClient)` extension methods, renamed the remaining `AsChatClient` methods to `AsIChatClient`, renamed the remaining `AsEmbeddingGenerator` methods to `AsIEmbeddingGenerator`, and added an `AsIChatClient` for `OpenAIResponseClient`.
- Removed the public `OpenAIChatClient`/`OpenAIEmbeddingGenerator` types. These are only created now via the extension methods.
- Removed serialization/deserialization helpers.
- Updated to support pulling propagating image detail from `AdditionalProperties`.
- Updated to accommodate the changes in `Microsoft.Extensions.AI.Abstractions`.

## 9.3.0-preview.1.25161.3

- Updated to accommodate the changes in `Microsoft.Extensions.AI.Abstractions`.

## 9.3.0-preview.1.25114.11

- Updated to depend on OpenAI 2.2.0-beta.1, updating with support for the Developer role, audio input and output, and additional options like output prediction. It is now also compatible with NativeAOT.
- Added an `AsChatClient` extension method for OpenAI's `AssistantClient`, enabling `IChatClient` to be used with OpenAI Assistants.
- Improved the OpenAI serialization helpers, including a custom converter for the `OpenAIChatCompletionRequest` envelope type.

## 9.1.0-preview.1.25064.3

- Updated to depend on OpenAI 2.1.0.
- Updated to propagate `Metadata` and `StoredOutputEnabled` from `ChatOptions.AdditionalProperties`.
- Added serialization helpers methods for deserializing OpenAI compatible JSON into the Microsoft.Extensions.AI object model, and vice versa serializing the Microsoft.Extensions.AI object model into OpenAI compatible JSON.

## 9.0.1-preview.1.24570.5

  - Upgraded to depend on the 2.1.0-beta.2 version of the OpenAI NuGet package.
  - Added the `OpenAIRealtimeExtensions` class, with `ToConversationFunctionTool` and `HandleToolCallsAsync` extension methods for using `AIFunction` with the OpenAI Realtime API.
  - Made the `ToolCallJsonSerializerOptions` property non-nullable.

## 9.0.0-preview.9.24525.1

- Lowered the required version of System.Text.Json to 8.0.5 when targeting net8.0 or older.
- Improved handling of system messages that include multiple content items.
- Improved handling of assistant messages that include both text and function call content.
- Fixed handling of streaming updates containing empty payloads.

## 9.0.0-preview.9.24507.7

- Initial Preview
