// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.RealtimeConversation;

#pragma warning disable S907 // "goto" statement should not be used
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S3604 // Member initializer values should not be redundant
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

// this contains only tool conversion routines for now.
internal sealed partial class OpenAIRealtimeConversationClient
{
    public static ConversationFunctionTool ToOpenAIConversationFunctionTool(AIFunction aiFunction)
    {
        bool? strict =
            aiFunction.AdditionalProperties.TryGetValue(OpenAIClientExtensions.StrictKey, out object? strictObj) &&
            strictObj is bool strictValue ?
            strictValue : null;

        string jsonSchema = OpenAIClientExtensions.GetSchema(aiFunction, strict).GetRawText();

        var toolSchema = JsonSerializer.Deserialize(jsonSchema, RealtimeConversationClientJsonContext.Default.ConversationFunctionToolJson)!;
        var functionParameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(toolSchema, RealtimeConversationClientJsonContext.Default.ConversationFunctionToolJson));

        var tool = new ConversationFunctionTool(aiFunction.Name)
        {
            Description = aiFunction.Description,
            Parameters = functionParameters,
        };
        return tool;
    }

    /// <summary>Used to create the JSON payload for an OpenAI chat tool description.</summary>
    private sealed class ConversationFunctionToolJson
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("required")]
        public HashSet<string> Required { get; set; } = [];

        [JsonPropertyName("properties")]
        public Dictionary<string, JsonElement> Properties { get; set; } = [];

        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; }
    }

    /// <summary>Source-generated JSON type information.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        UseStringEnumConverter = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true)]
    [JsonSerializable(typeof(ConversationFunctionToolJson))]
    [JsonSerializable(typeof(IDictionary<string, object?>))]
    [JsonSerializable(typeof(string[]))]
    private sealed partial class RealtimeConversationClientJsonContext : JsonSerializerContext;
}