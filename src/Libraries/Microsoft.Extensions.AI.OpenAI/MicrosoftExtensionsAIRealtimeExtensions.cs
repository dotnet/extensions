// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace OpenAI.Realtime;

/// <summary>Provides extension methods for working with content associated with OpenAI.Realtime.</summary>
public static class MicrosoftExtensionsAIRealtimeExtensions
{
    /// <summary>Creates an OpenAI <see cref="ConversationFunctionTool"/> from an <see cref="AIFunction"/>.</summary>
    /// <param name="function">The function to convert.</param>
    /// <returns>An OpenAI <see cref="ConversationFunctionTool"/> representing <paramref name="function"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static ConversationFunctionTool AsOpenAIConversationFunctionTool(this AIFunction function) =>
        OpenAIRealtimeConversationClient.ToOpenAIConversationFunctionTool(Throw.IfNull(function));
}
