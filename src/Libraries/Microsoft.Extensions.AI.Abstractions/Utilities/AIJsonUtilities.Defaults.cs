// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Extensions.AI;

public static partial class AIJsonUtilities
{
    /// <summary>Gets the <see cref="JsonSerializerOptions"/> singleton used as the default in JSON serialization operations.</summary>
    public static JsonSerializerOptions DefaultOptions { get; } = CreateDefaultOptions();

    /// <summary>Creates the default <see cref="JsonSerializerOptions"/> to use for serialization-related operations.</summary>
    [UnconditionalSuppressMessage("AotAnalysis", "IL3050", Justification = "DefaultJsonTypeInfoResolver is only used when reflection-based serialization is enabled")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "DefaultJsonTypeInfoResolver is only used when reflection-based serialization is enabled")]
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        // If reflection-based serialization is enabled by default, use it, as it's the most permissive in terms of what it can serialize,
        // and we want to be flexible in terms of what can be put into the various collections in the object model.
        // Otherwise, use the source-generated options to enable trimming and Native AOT.

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            // Keep in sync with the JsonSourceGenerationOptions attribute on JsonContext below.
            JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
            };

            options.MakeReadOnly();
            return options;
        }
        else
        {
            return JsonContext.Default.Options;
        }
    }

    // Keep in sync with CreateDefaultOptions above.
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        UseStringEnumConverter = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true)]
    [JsonSerializable(typeof(IList<ChatMessage>))]
    [JsonSerializable(typeof(ChatOptions))]
    [JsonSerializable(typeof(AudioTranscriptionOptions))]
    [JsonSerializable(typeof(EmbeddingGenerationOptions))]
    [JsonSerializable(typeof(ChatClientMetadata))]
    [JsonSerializable(typeof(AudioTranscriptionClientMetadata))]
    [JsonSerializable(typeof(EmbeddingGeneratorMetadata))]
    [JsonSerializable(typeof(ChatCompletion))]
    [JsonSerializable(typeof(StreamingChatCompletionUpdate))]
    [JsonSerializable(typeof(IReadOnlyList<StreamingChatCompletionUpdate>))]
    [JsonSerializable(typeof(AudioTranscriptionCompletion))]
    [JsonSerializable(typeof(StreamingAudioTranscriptionUpdate))]
    [JsonSerializable(typeof(IReadOnlyList<StreamingAudioTranscriptionUpdate>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(IDictionary<int, int>))]
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
    private sealed partial class JsonContext : JsonSerializerContext;
}
