// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

// These types are directly serialized by DistributedCachingChatClient
[JsonSerializable(typeof(ChatResponse))]
[JsonSerializable(typeof(IList<ChatMessage>))]
[JsonSerializable(typeof(IReadOnlyList<ChatResponseUpdate>))]

// These types are specific to the tests in this project
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Embedding<float>))]
[JsonSerializable(typeof(Dictionary<string, JsonDocument>))]
[JsonSerializable(typeof(Dictionary<string, JsonNode>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(DayOfWeek[]))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(ChatOptions))]
[JsonSerializable(typeof(EmbeddingGenerationOptions))]
internal sealed partial class TestJsonSerializerContext : JsonSerializerContext;
