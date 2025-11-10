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
    /// <summary>Creates an OpenAI <see cref="ResponseTool"/> from an <see cref="AIFunctionDeclaration"/>.</summary>
    /// <param name="function">The function to convert.</param>
    /// <returns>An OpenAI <see cref="ResponseTool"/> representing <paramref name="function"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static FunctionTool AsOpenAIResponseTool(this AIFunctionDeclaration function) =>
        OpenAIResponsesChatClient.ToResponseTool(Throw.IfNull(function));

    /// <summary>Creates an OpenAI <see cref="ResponseTool"/> from an <see cref="AITool"/>.</summary>
    /// <param name="tool">The tool to convert.</param>
    /// <returns>An OpenAI <see cref="ResponseTool"/> representing <paramref name="tool"/> or <see langword="null"/> if there is no mapping.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tool"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method is only able to create <see cref="ResponseTool"/>s for <see cref="AITool"/> types
    /// it's aware of, namely all of those available from the Microsoft.Extensions.AI.Abstractions library.
    /// </remarks>
    public static ResponseTool? AsOpenAIResponseTool(this AITool tool) =>
        OpenAIResponsesChatClient.ToResponseTool(Throw.IfNull(tool));

    /// <summary>
    /// Creates an OpenAI <see cref="ResponseTextFormat"/> from a <see cref="ChatResponseFormat"/>.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="options">The options to use when interpreting the format.</param>
    /// <returns>The converted OpenAI <see cref="ResponseTextFormat"/>.</returns>
    public static ResponseTextFormat? AsOpenAIResponseTextFormat(this ChatResponseFormat? format, ChatOptions? options = null) =>
        OpenAIResponsesChatClient.ToOpenAIResponseTextFormat(format, options);

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
        OpenAIResponsesChatClient.FromOpenAIResponse(Throw.IfNull(response), options, conversationId: null);

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
        OpenAIResponsesChatClient.FromOpenAIStreamingResponseUpdatesAsync(Throw.IfNull(responseUpdates), options, conversationId: null, cancellationToken: cancellationToken);

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

    /// <summary>Adds the <see cref="ResponseTool"/> to the list of <see cref="AITool"/>s.</summary>
    /// <param name="tools">The list of <see cref="AITool"/>s to which the provided tool should be added.</param>
    /// <param name="tool">The <see cref="ResponseTool"/> to add.</param>
    /// <remarks>
    /// <see cref="ResponseTool"/> does not derive from <see cref="AITool"/>, so it cannot be added directly to a list of <see cref="AITool"/>s.
    /// Instead, this method wraps the provided <see cref="ResponseTool"/> in an <see cref="AITool"/> and adds that to the list.
    /// The <see cref="IChatClient"/> returned by <see cref="OpenAIClientExtensions.AsIChatClient(OpenAIResponseClient)"/> will
    /// be able to unwrap the <see cref="ResponseTool"/> when it processes the list of tools and use the provided <paramref name="tool"/> as-is.
    /// </remarks>
    public static void Add(this IList<AITool> tools, ResponseTool tool)
    {
        _ = Throw.IfNull(tools);

        tools.Add(AsAITool(tool));
    }

    /// <summary>Creates an <see cref="AITool"/> to represent a raw <see cref="ResponseTool"/>.</summary>
    /// <param name="tool">The tool to wrap as an <see cref="AITool"/>.</param>
    /// <returns>The <paramref name="tool"/> wrapped as an <see cref="AITool"/>.</returns>
    /// <remarks>
    /// <para>
    /// The returned tool is only suitable for use with the <see cref="IChatClient"/> returned by
    /// <see cref="OpenAIClientExtensions.AsIChatClient(OpenAIResponseClient)"/> (or <see cref="IChatClient"/>s that delegate
    /// to such an instance). It is likely to be ignored by any other <see cref="IChatClient"/> implementation.
    /// </para>
    /// <para>
    /// When a tool has a corresponding <see cref="AITool"/>-derived type already defined in Microsoft.Extensions.AI,
    /// such as <see cref="AIFunction"/>, <see cref="HostedWebSearchTool"/>, <see cref="HostedMcpServerTool"/>, or
    /// <see cref="HostedFileSearchTool"/>, those types should be preferred instead of this method, as they are more portable,
    /// capable of being respected by any <see cref="IChatClient"/> implementation. This method does not attempt to
    /// map the supplied <see cref="ResponseTool"/> to any of those types, it simply wraps it as-is:
    /// the <see cref="IChatClient"/> returned by <see cref="OpenAIClientExtensions.AsIChatClient(OpenAIResponseClient)"/> will
    /// be able to unwrap the <see cref="ResponseTool"/> when it processes the list of tools.
    /// </para>
    /// </remarks>
    public static AITool AsAITool(this ResponseTool tool)
    {
        _ = Throw.IfNull(tool);

        return new OpenAIResponsesChatClient.ResponseToolAITool(tool);
    }
}
