// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Extensions.AI;

/// <summary>Provides cached options around JSON serialization to be used by the project.</summary>
internal static partial class JsonDefaults
{
    /// <summary>Gets the <see cref="JsonSerializerOptions"/> singleton to use for serialization-related operations.</summary>
    public static JsonSerializerOptions Options { get; } = CreateDefaultOptions();

    /// <summary>Creates the default <see cref="JsonSerializerOptions"/> to use for serialization-related operations.</summary>
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        // If reflection-based serialization is enabled by default, use it, as it's the most permissive in terms of what it can serialize,
        // and we want to be flexible in terms of what can be put into the various collections in the object model.
        // Otherwise, use the source-generated options to enable Native AOT.

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            // Keep in sync with the JsonSourceGenerationOptions on JsonContext below.
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#pragma warning disable IL3050
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
#pragma warning restore IL3050
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
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(IList<ChatMessage>))]
    [JsonSerializable(typeof(ChatOptions))]
    [JsonSerializable(typeof(EmbeddingGenerationOptions))]
    [JsonSerializable(typeof(ChatClientMetadata))]
    [JsonSerializable(typeof(EmbeddingGeneratorMetadata))]
    [JsonSerializable(typeof(ChatCompletion))]
    [JsonSerializable(typeof(StreamingChatCompletionUpdate))]
    [JsonSerializable(typeof(IReadOnlyList<StreamingChatCompletionUpdate>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(IDictionary<int, int>))]
    [JsonSerializable(typeof(IDictionary<string, object?>))]
    [JsonSerializable(typeof(JsonElement))]
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
