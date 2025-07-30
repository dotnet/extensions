// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace OpenAI.Chat;

/// <summary>Provides extension methods for working with content associated with OpenAI.Chat.</summary>
public static class MicrosoftExtensionsAIChatExtensions
{
    /// <summary>Creates an OpenAI <see cref="ChatTool"/> from an <see cref="AIFunction"/>.</summary>
    /// <param name="function">The function to convert.</param>
    /// <returns>An OpenAI <see cref="ChatTool"/> representing <paramref name="function"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static ChatTool AsOpenAIChatTool(this AIFunction function) =>
        OpenAIChatClient.ToOpenAIChatTool(Throw.IfNull(function));

    /// <summary>Creates a sequence of OpenAI <see cref="ChatMessage"/> instances from the specified input messages.</summary>
    /// <param name="messages">The input messages to convert.</param>
    /// <returns>A sequence of OpenAI chat messages.</returns>
    public static IEnumerable<ChatMessage> AsOpenAIChatMessages(this IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages) =>
        OpenAIChatClient.ToOpenAIChatMessages(Throw.IfNull(messages), chatOptions: null);

    /// <summary>Creates a Microsoft.Extensions.AI <see cref="ChatResponse"/> from a <see cref="ChatCompletion"/>.</summary>
    /// <param name="chatCompletion">The <see cref="ChatCompletion"/> to convert to a <see cref="ChatResponse"/>.</param>
    /// <param name="options">The options employed in the creation of the response.</param>
    /// <returns>A converted <see cref="ChatResponse"/>.</returns>
    public static ChatResponse AsChatResponse(this ChatCompletion chatCompletion, ChatCompletionOptions? options = null) =>
        OpenAIChatClient.FromOpenAIChatCompletion(Throw.IfNull(chatCompletion), options);
}
