// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats;
using static Microsoft.Extensions.AI.Evaluation.Reporting.Storage.DiskBasedResponseCache;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

[JsonSerializable(typeof(EvaluationResult))]
[JsonSerializable(typeof(Dataset))]
[JsonSerializable(typeof(CacheEntry))]
[JsonSerializable(typeof(CacheOptions))]
// Sync with types in AIJsonUtilities.JsonContext
[JsonSerializable(typeof(IList<ChatMessage>))]
[JsonSerializable(typeof(ChatOptions))]
[JsonSerializable(typeof(EmbeddingGenerationOptions))]
[JsonSerializable(typeof(ChatClientMetadata))]
[JsonSerializable(typeof(EmbeddingGeneratorMetadata))]
[JsonSerializable(typeof(ChatResponse))]
[JsonSerializable(typeof(ChatResponseUpdate))]
[JsonSerializable(typeof(IReadOnlyList<ChatResponseUpdate>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(IDictionary<string, object?>))]
[JsonSerializable(typeof(JsonDocument))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(Embedding))]
[JsonSerializable(typeof(Embedding<byte>))]
[JsonSerializable(typeof(Embedding<int>))]
#if NET
[JsonSerializable(typeof(Embedding<Half>))]
#endif
[JsonSerializable(typeof(Embedding<float>))]
[JsonSerializable(typeof(Embedding<double>))]
[JsonSerializable(typeof(AIContent))]
[JsonSourceGenerationOptions(
    Converters = [
        typeof(CamelCaseEnumConverter<EvaluationDiagnosticSeverity>),
        typeof(CamelCaseEnumConverter<EvaluationRating>),
        typeof(CamelCaseEnumConverter<CacheMode>),
        typeof(TimeSpanConverter)],
    WriteIndented = true,
    IgnoreReadOnlyProperties = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class SerializerContext : JsonSerializerContext
{
    private static SerializerContext? _compact;

    internal static SerializerContext Compact =>
        _compact ??=
            new(new JsonSerializerOptions(Default.Options)
            {
                WriteIndented = false,
            });
}
