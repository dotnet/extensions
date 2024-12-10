// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Chat;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Defines a set of helpers used to serialize Microsoft.Extensions.AI content using the OpenAI wire format.
/// </summary>
public static class OpenAISerializationHelpers
{
    /// <summary>
    /// Deserializes a chat completion request in the OpenAI wire format into a pair of <see cref="ChatMessage"/> and <see cref="ChatOptions"/> values.
    /// </summary>
    /// <param name="stream">The stream containing a message using the OpenAI wire format.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The deserialized list of chat messages and chat options.</returns>
    public static async Task<OpenAIChatCompletionRequest> DeserializeChatCompletionRequestAsync(
        Stream stream, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(stream);

        BinaryData binaryData = await BinaryData.FromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        ChatCompletionOptions openAiChatOptions = JsonModelHelpers.Deserialize<ChatCompletionOptions>(binaryData);
        return OpenAIModelMappers.FromOpenAIChatCompletionRequest(openAiChatOptions);
    }

    /// <summary>
    /// Serializes a Microsoft.Extensions.AI completion using the OpenAI wire format.
    /// </summary>
    /// <param name="stream">The stream to write the value.</param>
    /// <param name="chatCompletion">The chat completion to serialize.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> governing function call content serialization.</param>
    /// <param name="cancellationToken">A token used to cancel the serialization operation.</param>
    /// <returns>A task tracking the serialization operation.</returns>
    public static async Task SerializeAsync(
        Stream stream,
        ChatCompletion chatCompletion,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(stream);
        _ = Throw.IfNull(chatCompletion);
        options ??= AIJsonUtilities.DefaultOptions;

        OpenAI.Chat.ChatCompletion openAiChatCompletion = OpenAIModelMappers.ToOpenAIChatCompletion(chatCompletion, options);
        BinaryData binaryData = JsonModelHelpers.Serialize(openAiChatCompletion);
        await stream.WriteAsync(binaryData.ToMemory(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes a Microsoft.Extensions.AI streaming completion using the OpenAI wire format.
    /// </summary>
    /// <param name="stream">The stream to write the value.</param>
    /// <param name="streamingChatCompletionUpdates">The streaming chat completions to serialize.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> governing function call content serialization.</param>
    /// <param name="cancellationToken">A token used to cancel the serialization operation.</param>
    /// <returns>A task tracking the serialization operation.</returns>
    public static Task SerializeStreamingAsync(
        Stream stream,
        IAsyncEnumerable<StreamingChatCompletionUpdate> streamingChatCompletionUpdates,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(stream);
        _ = Throw.IfNull(streamingChatCompletionUpdates);
        options ??= AIJsonUtilities.DefaultOptions;

        var mappedUpdates = OpenAIModelMappers.ToOpenAIStreamingChatCompletionAsync(streamingChatCompletionUpdates, options, cancellationToken);
        return SseFormatter.WriteAsync(ToSseEventsAsync(mappedUpdates), stream, FormatAsSseEvent, cancellationToken);

        static async IAsyncEnumerable<SseItem<BinaryData>> ToSseEventsAsync(IAsyncEnumerable<OpenAI.Chat.StreamingChatCompletionUpdate> updates)
        {
            await foreach (var update in updates.ConfigureAwait(false))
            {
                BinaryData binaryData = JsonModelHelpers.Serialize(update);
                yield return new(binaryData);
            }

            yield return new(_finalSseEvent);
        }

        static void FormatAsSseEvent(SseItem<BinaryData> sseItem, IBufferWriter<byte> writer) =>
            writer.Write(sseItem.Data.ToMemory().Span);
    }

    private static readonly BinaryData _finalSseEvent = new("[DONE]"u8.ToArray());
}
