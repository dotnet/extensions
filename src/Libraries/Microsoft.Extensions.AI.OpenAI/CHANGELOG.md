# Release History

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

Initial Preview
