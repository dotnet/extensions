// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Chat;

#pragma warning disable S1135 // Track uses of "TODO" tags

namespace Microsoft.Extensions.AI;

/// <summary>
/// Defines a set of helpers used to serialize Microsoft.Extensions.AI content using the OpenAI wire format.
/// </summary>
public static class OpenAISerializationHelpers
{
    /// <summary>
    /// Deserializes a stream using the OpenAI wire format into a pair of <see cref="ChatMessage"/> and <see cref="ChatOptions"/> values.
    /// </summary>
    /// <param name="stream">The stream containing a message using the OpenAI wire format.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> governing deserialization of function call content.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The deserialized list of chat messages and chat options.</returns>
    public static async Task<(IList<ChatMessage> Messages, ChatOptions? Options)> DeserializeFromOpenAIAsync(
        Stream stream,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(stream);
        options ??= AIJsonUtilities.DefaultOptions;

        BinaryData binaryData = await BinaryData.FromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        ChatCompletionOptions openAiChatOptions = JsonModelHelpers.Deserialize<ChatCompletionOptions>(binaryData);
        var openAiMessages = (IList<OpenAI.Chat.ChatMessage>)typeof(ChatCompletionOptions).GetProperty("Messages")!.GetValue(openAiChatOptions)!;

        IList<ChatMessage> messages = OpenAIModelMappers.FromOpenAIChatMessages(openAiMessages, options).ToList();
        ChatOptions chatOptions = OpenAIModelMappers.FromOpenAIOptions(openAiChatOptions);
        return (messages, chatOptions);
    }

    /// <summary>
    /// Serializes a Microsoft.Extensions.AI completion using the OpenAI wire format.
    /// </summary>
    /// <param name="chatCompletion">The chat completion to serialize.</param>
    /// <param name="stream">The stream to write the value.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> governing function call content serialization.</param>
    /// <param name="cancellationToken">A token used to cancel the serialization operation.</param>
    /// <returns>A task tracking the serialization operation.</returns>
    public static async Task SerializeAsOpenAIAsync(
        this ChatCompletion chatCompletion,
        Stream stream,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(stream);
        _ = Throw.IfNull(chatCompletion);
        options ??= AIJsonUtilities.DefaultOptions;

        OpenAI.Chat.ChatCompletion openAiChatCompletion = OpenAIModelMappers.ToOpenAIChatCompletion(chatCompletion, options);
        BinaryData binaryData = JsonModelHelpers.Serialize(openAiChatCompletion);
#if NET
        await stream.WriteAsync(binaryData.ToMemory(), cancellationToken).ConfigureAwait(false);
#else
        await stream.WriteAsync(binaryData.ToArray(), 0, binaryData.Length, cancellationToken).ConfigureAwait(false);
#endif
    }

    /// <summary>
    /// Serializes a Microsoft.Extensions.AI streaming completion using the OpenAI wire format.
    /// </summary>
    /// <param name="streamingChatCompletionUpdates">The streaming chat completions to serialize.</param>
    /// <param name="stream">The stream to write the value.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> governing function call content serialization.</param>
    /// <param name="cancellationToken">A token used to cancel the serialization operation.</param>
    /// <returns>A task tracking the serialization operation.</returns>
    public static Task SerializeAsOpenAIAsync(
        this IAsyncEnumerable<StreamingChatCompletionUpdate> streamingChatCompletionUpdates,
        Stream stream,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(stream);
        _ = Throw.IfNull(streamingChatCompletionUpdates);
        options ??= AIJsonUtilities.DefaultOptions;

        var mappedUpdates = OpenAIModelMappers.ToOpenAIStreamingChatCompletionAsync(streamingChatCompletionUpdates, options, cancellationToken);
        return SseFormatter.WriteAsync(WrapEventsAsync(mappedUpdates), stream, FormatAsSseEvent, cancellationToken);

        static async IAsyncEnumerable<SseItem<T>> WrapEventsAsync<T>(IAsyncEnumerable<T> elements)
        {
            await foreach (T element in elements.ConfigureAwait(false))
            {
                yield return new SseItem<T>(element); // TODO specify eventId or reconnection interval?
            }
        }

        static void FormatAsSseEvent(SseItem<OpenAI.Chat.StreamingChatCompletionUpdate> sseItem, IBufferWriter<byte> writer)
        {
            BinaryData binaryData = JsonModelHelpers.Serialize(sseItem.Data);
            writer.Write(binaryData.ToMemory().Span);
        }
    }

    private static class JsonModelHelpers
    {
        public static BinaryData Serialize<TModel>(TModel value)
            where TModel : IJsonModel<TModel>
        {
            return value.Write(ModelReaderWriterOptions.Json);
        }

        public static TModel Deserialize<TModel>(BinaryData data)
            where TModel : IJsonModel<TModel>, new()
        {
            return JsonModelDeserializationWitness<TModel>.Value.Create(data, ModelReaderWriterOptions.Json);
        }

        private sealed class JsonModelDeserializationWitness<TModel>
            where TModel : IJsonModel<TModel>, new()
        {
            public static readonly IJsonModel<TModel> Value = new TModel();
        }
    }

}
