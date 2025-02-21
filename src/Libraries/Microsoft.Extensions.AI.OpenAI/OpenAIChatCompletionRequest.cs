// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;

#pragma warning disable CA1034 // Nested types should not be visible

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an OpenAI chat completion request deserialized as Microsoft.Extension.AI models.
/// </summary>
[JsonConverter(typeof(Converter))]
public sealed class OpenAIChatCompletionRequest
{
    /// <summary>
    /// Gets the chat messages specified in the request.
    /// </summary>
    public required IList<ChatMessage> Messages { get; init; }

    /// <summary>
    /// Gets the chat options governing the request.
    /// </summary>
    public required ChatOptions Options { get; init; }

    /// <summary>
    /// Gets a value indicating whether the response should be streamed.
    /// </summary>
    public bool Stream { get; init; }

    /// <summary>
    /// Gets the model id requested by the chat completion.
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// Converts an OpenAIChatCompletionRequest object to and from JSON.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<OpenAIChatCompletionRequest>
    {
        /// <summary>
        /// Reads and converts the JSON to type OpenAIChatCompletionRequest.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The converted OpenAIChatCompletionRequest object.</returns>
        public override OpenAIChatCompletionRequest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ChatCompletionOptions chatCompletionOptions = JsonModelHelpers.Deserialize<ChatCompletionOptions>(ref reader);
            return OpenAIModelMappers.FromOpenAIChatCompletionRequest(chatCompletionOptions);
        }

        /// <summary>
        /// Writes the specified value as JSON.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, OpenAIChatCompletionRequest value, JsonSerializerOptions options) =>
            throw new NotSupportedException("Request body serialization is not supported.");
    }
}
