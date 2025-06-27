// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using OpenAI.RealtimeConversation;

namespace Microsoft.Extensions.AI;

/// <summary>Provides helpers for interacting with OpenAI Realtime.</summary>
internal sealed class OpenAIRealtimeConversationClient
{
    public static ConversationFunctionTool ToOpenAIConversationFunctionTool(AIFunction aiFunction)
    {
        (BinaryData parameters, _) = OpenAIClientExtensions.ToOpenAIFunctionParameters(aiFunction);

        return new ConversationFunctionTool(aiFunction.Name)
        {
            Description = aiFunction.Description,
            Parameters = parameters,
        };
    }
}
