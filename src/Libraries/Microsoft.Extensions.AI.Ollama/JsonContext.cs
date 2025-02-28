// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OllamaChatRequest))]
[JsonSerializable(typeof(OllamaChatRequestMessage))]
[JsonSerializable(typeof(OllamaChatResponse))]
[JsonSerializable(typeof(OllamaChatResponseMessage))]
[JsonSerializable(typeof(OllamaFunctionCallContent))]
[JsonSerializable(typeof(OllamaFunctionResultContent))]
[JsonSerializable(typeof(OllamaFunctionTool))]
[JsonSerializable(typeof(OllamaFunctionToolCall))]
[JsonSerializable(typeof(OllamaFunctionToolParameter))]
[JsonSerializable(typeof(OllamaFunctionToolParameters))]
[JsonSerializable(typeof(OllamaRequestOptions))]
[JsonSerializable(typeof(OllamaTool))]
[JsonSerializable(typeof(OllamaToolCall))]
[JsonSerializable(typeof(OllamaEmbeddingRequest))]
[JsonSerializable(typeof(OllamaEmbeddingResponse))]
internal sealed partial class JsonContext : JsonSerializerContext;
