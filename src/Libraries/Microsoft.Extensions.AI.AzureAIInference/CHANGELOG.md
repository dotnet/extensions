# Release History

## 9.0.1-preview.1.24570.5

  - Made the `ToolCallJsonSerializerOptions` property non-nullable.

## 9.0.0-preview.9.24556.5

- Fixed `AzureAIInferenceEmbeddingGenerator` to respect `EmbeddingGenerationOptions.Dimensions`.

## 9.0.0-preview.9.24525.1

- Lowered the required version of System.Text.Json to 8.0.5 when targeting net8.0 or older.
- Updated to use Azure.AI.Inference 1.0.0-beta.2.
- Added `AzureAIInferenceEmbeddingGenerator` and corresponding `AsEmbeddingGenerator` extension method.
- Improved handling of assistant messages that include both text and function call content.

## 9.0.0-preview.9.24507.7

Initial Preview
