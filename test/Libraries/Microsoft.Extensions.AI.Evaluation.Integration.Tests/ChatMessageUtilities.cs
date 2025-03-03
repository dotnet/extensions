// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Integration.Tests;

internal static class ChatMessageUtilities
{
    internal static ChatMessage ToUserMessage(this string message)
        => new ChatMessage(ChatRole.User, message);

    internal static ChatMessage ToAssistantMessage(this string message)
        => new ChatMessage(ChatRole.Assistant, message);
}
