// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S3254 // Default parameter values should not be passed as arguments

namespace OpenAI.Responses;

/// <summary>Provides extension methods for working with content associated with OpenAI.Responses.</summary>
public static class MicrosoftExtensionsAIResponsesExtensions
{
    /// <summary>Creates an OpenAI <see cref="ResponseTool"/> from an <see cref="AIFunction"/>.</summary>
    /// <param name="function">The function to convert.</param>
    /// <returns>An OpenAI <see cref="ResponseTool"/> representing <paramref name="function"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static ResponseTool AsOpenAIResponseTool(this AIFunction function) =>
        OpenAIResponsesChatClient.ToResponseTool(Throw.IfNull(function));

    /// <summary>Creates a sequence of OpenAI <see cref="ResponseItem"/> instances from the specified input messages.</summary>
    /// <param name="messages">The input messages to convert.</param>
    /// <param name="options">The options employed while processing <paramref name="messages"/>.</param>
    /// <returns>A sequence of OpenAI response items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    public static IEnumerable<ResponseItem> AsOpenAIResponseItems(this IEnumerable<ChatMessage> messages, ChatOptions? options = null) =>
        OpenAIResponsesChatClient.ToOpenAIResponseItems(Throw.IfNull(messages), options);

    /// <summary>Creates a sequence of <see cref="ChatMessage"/> instances from the specified input items.</summary>
    /// <param name="items">The input messages to convert.</param>
    /// <returns>A sequence of <see cref="ChatMessage"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    public static IEnumerable<ChatMessage> AsChatMessages(this IEnumerable<ResponseItem> items) =>
        OpenAIResponsesChatClient.ToChatMessages(Throw.IfNull(items));

    /// <summary>Creates a Microsoft.Extensions.AI <see cref="ChatResponse"/> from an <see cref="OpenAIResponse"/>.</summary>
    /// <param name="response">The <see cref="OpenAIResponse"/> to convert to a <see cref="ChatResponse"/>.</param>
    /// <param name="options">The options employed in the creation of the response.</param>
    /// <returns>A converted <see cref="ChatResponse"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    public static ChatResponse AsChatResponse(this OpenAIResponse response, ResponseCreationOptions? options = null) =>
        OpenAIResponsesChatClient.FromOpenAIResponse(Throw.IfNull(response), options);

    /// <summary>
    /// Creates a sequence of Microsoft.Extensions.AI <see cref="ChatResponseUpdate"/> instances from the specified
    /// sequence of OpenAI <see cref="StreamingResponseUpdate"/> instances.
    /// </summary>
    /// <param name="responseUpdates">The update instances.</param>
    /// <param name="options">The options employed in the creation of the response.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A sequence of converted <see cref="ChatResponseUpdate"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="responseUpdates"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<ChatResponseUpdate> AsChatResponseUpdatesAsync(
        this IAsyncEnumerable<StreamingResponseUpdate> responseUpdates, ResponseCreationOptions? options = null, CancellationToken cancellationToken = default) =>
        OpenAIResponsesChatClient.FromOpenAIStreamingResponseUpdatesAsync(Throw.IfNull(responseUpdates), options, cancellationToken);

    /// <summary>Creates an OpenAI <see cref="OpenAIResponse"/> from a <see cref="ChatResponse"/>.</summary>
    /// <param name="response">The response to convert.</param>
    /// <param name="options">The options employed in the creation of the response.</param>
    /// <returns>The created <see cref="OpenAIResponse"/>.</returns>
    public static OpenAIResponse AsOpenAIResponse(this ChatResponse response, ChatOptions? options = null)
    {
        _ = Throw.IfNull(response);

        if (response.RawRepresentation is OpenAIResponse openAIResponse)
        {
            return openAIResponse;
        }

        return OpenAIResponsesModelFactory.OpenAIResponse(
            response.ResponseId,
            response.CreatedAt ?? default,
            ResponseStatus.Completed,
            usage: null, // No way to construct a ResponseTokenUsage right now from external to the OpenAI library
            maxOutputTokenCount: options?.MaxOutputTokens,
            outputItems: OpenAIResponsesChatClient.ToOpenAIResponseItems(response.Messages, options),
            parallelToolCallsEnabled: options?.AllowMultipleToolCalls ?? false,
            model: response.ModelId ?? options?.ModelId,
            temperature: options?.Temperature,
            topP: options?.TopP,
            previousResponseId: options?.ConversationId,
            instructions: options?.Instructions);
    }
}
