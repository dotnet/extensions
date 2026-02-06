// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;
using OpenAI.Responses;

#pragma warning disable SA1515 // Single-line comment should be preceded by blank line

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="OpenAIClient"/>s.</summary>
public static class OpenAIClientExtensions
{
    /// <summary>Key into AdditionalProperties used to store a strict option.</summary>
    private const string StrictKey = "strict";

    /// <summary>Gets the default OpenAI endpoint.</summary>
    internal static Uri DefaultOpenAIEndpoint { get; } = new("https://api.openai.com/v1");

    /// <summary>Gets a <see cref="ChatRole"/> for "developer".</summary>
    internal static ChatRole ChatRoleDeveloper { get; } = new ChatRole("developer");

    /// <summary>
    /// Gets the JSON schema transformer cache conforming to OpenAI <b>strict</b> / structured output restrictions per
    /// https://platform.openai.com/docs/guides/structured-outputs?api-mode=responses#supported-schemas.
    /// </summary>
    internal static AIJsonSchemaTransformCache StrictSchemaTransformCache { get; } = new(new()
    {
        DisallowAdditionalProperties = true,
        ConvertBooleanSchemas = true,
        MoveDefaultKeywordToDescription = true,
        RequireAllProperties = true,
        TransformSchemaNode = (ctx, node) =>
        {
            // Move content from common but unsupported properties to description. In particular, we focus on properties that
            // the AIJsonUtilities schema generator might produce and/or that are explicitly mentioned in the OpenAI documentation.

            if (node is JsonObject schemaObj)
            {
                StringBuilder? additionalDescription = null;

                ReadOnlySpan<string> unsupportedProperties =
                [
                    // Produced by AIJsonUtilities but not in allow list at https://platform.openai.com/docs/guides/structured-outputs#supported-properties:
                    "contentEncoding", "contentMediaType", "not",

                    // Explicitly mentioned at https://platform.openai.com/docs/guides/structured-outputs?api-mode=responses#key-ordering as being unsupported with some models:
                    "minLength", "maxLength", "pattern", "format",
                    "minimum", "maximum", "multipleOf",
                    "patternProperties",
                    "minItems", "maxItems",

                    // Explicitly mentioned at https://learn.microsoft.com/azure/ai-services/openai/how-to/structured-outputs?pivots=programming-language-csharp&tabs=python-secure%2Cdotnet-entra-id#unsupported-type-specific-keywords
                    // as being unsupported with Azure OpenAI:
                    "unevaluatedProperties", "propertyNames", "minProperties", "maxProperties",
                    "unevaluatedItems", "contains", "minContains", "maxContains", "uniqueItems",
                ];

                foreach (string propName in unsupportedProperties)
                {
                    if (schemaObj[propName] is { } propNode)
                    {
                        _ = schemaObj.Remove(propName);
                        AppendLine(ref additionalDescription, propName, propNode);
                    }
                }

                if (additionalDescription is not null)
                {
                    schemaObj["description"] = schemaObj["description"] is { } descriptionNode && descriptionNode.GetValueKind() == JsonValueKind.String ?
                        $"{descriptionNode.GetValue<string>()}{Environment.NewLine}{additionalDescription}" :
                        additionalDescription.ToString();
                }

                return node;

                static void AppendLine(ref StringBuilder? sb, string propName, JsonNode propNode)
                {
                    sb ??= new();

                    if (sb.Length > 0)
                    {
                        _ = sb.AppendLine();
                    }

                    _ = sb.Append(propName).Append(": ").Append(propNode);
                }
            }

            return node;
        },
    });

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="ChatClient"/>.</summary>
    /// <param name="chatClient">The client.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="ChatClient"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="chatClient"/> is <see langword="null"/>.</exception>
    public static IChatClient AsIChatClient(this ChatClient chatClient) =>
        new OpenAIChatClient(chatClient);

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="ResponsesClient"/>.</summary>
    /// <param name="responseClient">The client.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="ResponsesClient"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="responseClient"/> is <see langword="null"/>.</exception>
    [Experimental(DiagnosticIds.Experiments.AIOpenAIResponses)]
    public static IChatClient AsIChatClient(this ResponsesClient responseClient) =>
        new OpenAIResponsesChatClient(responseClient);

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="AssistantClient"/>.</summary>
    /// <param name="assistantClient">The <see cref="AssistantClient"/> instance to be accessed as an <see cref="IChatClient"/>.</param>
    /// <param name="assistantId">The unique identifier of the assistant with which to interact.</param>
    /// <param name="threadId">
    /// An optional existing thread identifier for the chat session. This serves as a default, and may be overridden per call to
    /// <see cref="IChatClient.GetResponseAsync"/> or <see cref="IChatClient.GetStreamingResponseAsync"/> via the <see cref="ChatOptions.ConversationId"/>
    /// property. If no thread ID is provided via either mechanism, a new thread will be created for the request.
    /// </param>
    /// <returns>An <see cref="IChatClient"/> instance configured to interact with the specified agent and thread.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assistantClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="assistantId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="assistantId"/> is empty or composed entirely of whitespace.</exception>
    [Experimental(DiagnosticIds.Experiments.AIOpenAIAssistants)]
    public static IChatClient AsIChatClient(this AssistantClient assistantClient, string assistantId, string? threadId = null) =>
        new OpenAIAssistantsChatClient(assistantClient, assistantId, threadId);

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="AssistantClient"/>.</summary>
    /// <param name="assistantClient">The <see cref="AssistantClient"/> instance to be accessed as an <see cref="IChatClient"/>.</param>
    /// <param name="assistant">The <see cref="Assistant"/> with which to interact.</param>
    /// <param name="threadId">
    /// An optional existing thread identifier for the chat session. This serves as a default, and may be overridden per call to
    /// <see cref="IChatClient.GetResponseAsync"/> or <see cref="IChatClient.GetStreamingResponseAsync"/> via the <see cref="ChatOptions.ConversationId"/>
    /// property. If no thread ID is provided via either mechanism, a new thread will be created for the request.
    /// </param>
    /// <returns>An <see cref="IChatClient"/> instance configured to interact with the specified agent and thread.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assistantClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="assistant"/> is <see langword="null"/>.</exception>
    [Experimental(DiagnosticIds.Experiments.AIOpenAIAssistants)]
    public static IChatClient AsIChatClient(this AssistantClient assistantClient, Assistant assistant, string? threadId = null) =>
        new OpenAIAssistantsChatClient(assistantClient, assistant, threadId);

    /// <summary>Gets an <see cref="ISpeechToTextClient"/> for use with this <see cref="AudioClient"/>.</summary>
    /// <param name="audioClient">The client.</param>
    /// <returns>An <see cref="ISpeechToTextClient"/> that can be used to transcribe audio via the <see cref="AudioClient"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="audioClient"/> is <see langword="null"/>.</exception>
    [Experimental(DiagnosticIds.Experiments.AISpeechToText, UrlFormat = DiagnosticIds.UrlFormat)]
    public static ISpeechToTextClient AsISpeechToTextClient(this AudioClient audioClient) =>
        new OpenAISpeechToTextClient(audioClient);

    /// <summary>Gets an <see cref="IImageGenerator"/> for use with this <see cref="ImageClient"/>.</summary>
    /// <param name="imageClient">The client.</param>
    /// <returns>An <see cref="IImageGenerator"/> that can be used to generate images via the <see cref="ImageClient"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="imageClient"/> is <see langword="null"/>.</exception>
    [Experimental(DiagnosticIds.Experiments.AIImageGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
    public static IImageGenerator AsIImageGenerator(this ImageClient imageClient) =>
        new OpenAIImageGenerator(imageClient);

    /// <summary>Gets an <see cref="IEmbeddingGenerator{String, Single}"/> for use with this <see cref="EmbeddingClient"/>.</summary>
    /// <param name="embeddingClient">The client.</param>
    /// <param name="defaultModelDimensions">The number of dimensions to generate in each embedding.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> that can be used to generate embeddings via the <see cref="EmbeddingClient"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="embeddingClient"/> is <see langword="null"/>.</exception>
    public static IEmbeddingGenerator<string, Embedding<float>> AsIEmbeddingGenerator(this EmbeddingClient embeddingClient, int? defaultModelDimensions = null) =>
        new OpenAIEmbeddingGenerator(embeddingClient, defaultModelDimensions);

    /// <summary>Gets whether the properties specify that strict schema handling is desired.</summary>
    internal static bool? HasStrict(IReadOnlyDictionary<string, object?>? additionalProperties) =>
        additionalProperties?.TryGetValue(StrictKey, out object? strictObj) is true &&
        strictObj is bool strictValue ?
        strictValue : null;

    /// <summary>Extracts from an <see cref="AIFunctionDeclaration"/> the parameters and strictness setting for use with OpenAI's APIs.</summary>
    internal static BinaryData ToOpenAIFunctionParameters(AIFunctionDeclaration aiFunction, bool? strict)
    {
        // Perform any desirable transformations on the function's JSON schema, if it'll be used in a strict setting.
        JsonElement jsonSchema = strict is true ?
            StrictSchemaTransformCache.GetOrCreateTransformedSchema(aiFunction) :
            aiFunction.JsonSchema;

        // Roundtrip the schema through the ToolJson model type to force missing properties
        // into existence, then return the serialized UTF8 bytes as BinaryData.
#pragma warning disable OPENAI001 // OpenAIJsonContext is marked as experimental since it relies on source-generated serializers
        var tool = JsonSerializer.Deserialize(jsonSchema, OpenAIJsonContext.Default.ToolJson)!;
        var functionParameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(tool, OpenAIJsonContext.Default.ToolJson));
#pragma warning restore OPENAI001

        return functionParameters;
    }

    /// <summary>Creates a new instance of <see cref="FunctionCallContent"/> parsing arguments using a specified encoding and parser.</summary>
    /// <param name="json">The input arguments to be parsed.</param>
    /// <param name="callId">The function call ID.</param>
    /// <param name="name">The function name.</param>
    /// <returns>A new instance of <see cref="FunctionCallContent"/> containing the parse result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
#pragma warning disable OPENAI001 // OpenAIJsonContext is marked as experimental since it relies on source-generated serializers
    internal static FunctionCallContent ParseCallContent(string json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(json, callId, name,
            static json => JsonSerializer.Deserialize(json, OpenAIJsonContext.Default.IDictionaryStringObject)!);
#pragma warning restore OPENAI001

    /// <summary>Creates a new instance of <see cref="FunctionCallContent"/> parsing arguments using a specified encoding and parser.</summary>
    /// <param name="utf8json">The input arguments to be parsed.</param>
    /// <param name="callId">The function call ID.</param>
    /// <param name="name">The function name.</param>
    /// <returns>A new instance of <see cref="FunctionCallContent"/> containing the parse result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
#pragma warning disable OPENAI001 // OpenAIJsonContext is marked as experimental since it relies on source-generated serializers
    internal static FunctionCallContent ParseCallContent(BinaryData utf8json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(utf8json, callId, name,
            static utf8json => JsonSerializer.Deserialize(utf8json, OpenAIJsonContext.Default.IDictionaryStringObject)!);
#pragma warning restore OPENAI001

    /// <summary>Gets a media type for an image based on the file extension in the provided URI.</summary>
    internal static string ImageUriToMediaType(Uri uri)
    {
        return MediaTypeMap.GetMediaType(uri.AbsoluteUri) ?? "image/*";
    }

    /// <summary>Sets $.model in <paramref name="patch"/> to <paramref name="modelId"/> if not already set.</summary>
    [Experimental(DiagnosticIds.Experiments.AIOpenAIJsonPatch)]
    internal static void PatchModelIfNotSet(ref JsonPatch patch, string? modelId)
    {
        if (modelId is not null)
        {
            _ = patch.TryGetValue("$.model"u8, out string? existingModel);
            if (existingModel is null)
            {
                patch.Set("$.model"u8, modelId);
            }
        }
    }

    /// <summary>Gets the typed property of the specified name from the tool's <see cref="AITool.AdditionalProperties"/>.</summary>
    internal static T? GetProperty<T>(this AITool tool, string name) =>
        tool.AdditionalProperties?.TryGetValue(name, out object? value) is true && value is T tValue ? tValue : default;

    /// <summary>Gets whether an ID is an OpenAI conversation ID.</summary>
    /// <remarks>
    /// Technically, OpenAI's IDs are opaque. However, by convention conversation IDs start with "conv_" and
    /// we can use that to disambiguate whether we're looking at a conversation ID or something else, like a response ID.
    /// </remarks>
    internal static bool IsConversationId(string? id) =>
        id?.StartsWith("conv_", StringComparison.OrdinalIgnoreCase) is true;

    /// <summary>Used to create the JSON payload for an OpenAI tool description.</summary>
    internal sealed class ToolJson
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("required")]
        public HashSet<string> Required { get; set; } = [];

        [JsonPropertyName("properties")]
        public Dictionary<string, JsonElement> Properties { get; set; } = [];

        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }
}
