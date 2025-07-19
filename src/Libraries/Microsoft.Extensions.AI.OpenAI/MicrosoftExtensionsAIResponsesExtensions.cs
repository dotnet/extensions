// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

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
    /// <returns>A sequence of OpenAI response items.</returns>
    public static IEnumerable<ResponseItem> AsOpenAIResponseItems(this IEnumerable<ChatMessage> messages) =>
        OpenAIResponsesChatClient.ToOpenAIResponseItems(Throw.IfNull(messages));

    /// <summary>Creates a Microsoft.Extensions.AI <see cref="ChatResponse"/> from an <see cref="OpenAIResponse"/>.</summary>
    /// <param name="response">The <see cref="OpenAIResponse"/> to convert to a <see cref="ChatResponse"/>.</param>
    /// <param name="options">The options employed in the creation of the response.</param>
    /// <returns>A converted <see cref="ChatResponse"/>.</returns>
    public static ChatResponse AsChatResponse(this OpenAIResponse response, ResponseCreationOptions? options = null) =>
        OpenAIResponsesChatClient.FromOpenAIResponse(Throw.IfNull(response), options);
}
