// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Extensions.AI;

public static partial class AIJsonUtilities
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> singleton used as the default in JSON serialization operations.
    /// </summary>
    /// <remarks>
    /// <para>For Native AOT or applications disabling <see cref="JsonSerializer.IsReflectionEnabledByDefault"/> this instance includes source generated contracts
    /// for all common exchange types contained in the Microsoft.Extensions.AI.Abstractions library.
    /// </para>
    /// <para>
    /// It additionally turns on the following settings:
    /// <list type="number">
    /// <item>Enables the <see cref="JsonSerializerOptions.WriteIndented"/> property.</item>
    /// <item>Enables string based enum serialization as implemented by <see cref="JsonStringEnumConverter"/>.</item>
    /// <item>Enables <see cref="JsonIgnoreCondition.WhenWritingNull"/> as the default ignore condition for properties.</item>
    /// <item>
    /// Enables <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/> when escaping JSON strings.
    /// Consuming applications must ensure that JSON outputs are adequately escaped before embedding in other document formats, such as HTML and XML.
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    public static JsonSerializerOptions DefaultOptions { get; } = CreateDefaultOptions();

    /// <summary>Creates the default <see cref="JsonSerializerOptions"/> to use for serialization-related operations.</summary>
    [UnconditionalSuppressMessage("AotAnalysis", "IL3050", Justification = "DefaultJsonTypeInfoResolver is only used when reflection-based serialization is enabled")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "DefaultJsonTypeInfoResolver is only used when reflection-based serialization is enabled")]
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        // If reflection-based serialization is enabled by default, use it, as it's the most permissive in terms of what it can serialize,
        // and we want to be flexible in terms of what can be put into the various collections in the object model.
        // Otherwise, use the source-generated options to enable trimming and Native AOT.
        JsonSerializerOptions options;

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            // Keep in sync with the JsonSourceGenerationOptions attribute on JsonContext below.
            options = new(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
        }
        else
        {
            options = new(JsonContext.Default.Options)
            {
                // Compile-time encoder setting not yet available
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
        }

        options.MakeReadOnly();
        return options;
    }

    // Keep in sync with CreateDefaultOptions above.
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        UseStringEnumConverter = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true)]
    [JsonSerializable(typeof(SpeechToTextOptions))]
    [JsonSerializable(typeof(SpeechToTextClientMetadata))]
    [JsonSerializable(typeof(SpeechToTextResponse))]
    [JsonSerializable(typeof(SpeechToTextResponseUpdate))]
    [JsonSerializable(typeof(IReadOnlyList<SpeechToTextResponseUpdate>))]
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
    [JsonSerializable(typeof(AIFunctionArguments))]
    [EditorBrowsable(EditorBrowsableState.Never)] // Never use JsonContext directly, use DefaultOptions instead.
    private sealed partial class JsonContext : JsonSerializerContext;
}
