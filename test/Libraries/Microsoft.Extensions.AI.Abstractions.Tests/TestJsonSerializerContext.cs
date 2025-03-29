// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ChatResponse))]
[JsonSerializable(typeof(ChatResponseUpdate))]
[JsonSerializable(typeof(SpeechToTextResponse))]
[JsonSerializable(typeof(SpeechToTextResponseUpdate))]
[JsonSerializable(typeof(SpeechToTextResponseUpdateKind))]
[JsonSerializable(typeof(SpeechToTextOptions))]
[JsonSerializable(typeof(ChatOptions))]
[JsonSerializable(typeof(EmbeddingGenerationOptions))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(AIFunctionArguments))]
[JsonSerializable(typeof(int[]))] // Used in ChatMessageContentTests
[JsonSerializable(typeof(Embedding))] // Used in EmbeddingTests
[JsonSerializable(typeof(Dictionary<string, JsonDocument>))] // Used in Content tests
[JsonSerializable(typeof(Dictionary<string, JsonNode>))] // Used in Content tests
[JsonSerializable(typeof(Dictionary<string, string>))] // Used in Content tests
[JsonSerializable(typeof(ReadOnlyDictionary<string, string>))] // Used in Content tests
[JsonSerializable(typeof(DayOfWeek[]))] // Used in Content tests
[JsonSerializable(typeof(Guid))] // Used in Content tests
[JsonSerializable(typeof(decimal))] // Used in Content tests
internal sealed partial class TestJsonSerializerContext : JsonSerializerContext;
