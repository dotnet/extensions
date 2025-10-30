// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenAI.Realtime;

namespace Microsoft.Extensions.AI;

/// <summary>Provides helpers for interacting with OpenAI Realtime.</summary>
internal sealed class OpenAIRealtimeConversationClient
{
    public static ConversationFunctionTool ToOpenAIConversationFunctionTool(AIFunctionDeclaration aiFunction, ChatOptions? options = null)
    {
        bool? strict =
            OpenAIClientExtensions.HasStrict(aiFunction.AdditionalProperties) ??
            OpenAIClientExtensions.HasStrict(options?.AdditionalProperties);

        return new ConversationFunctionTool(aiFunction.Name)
        {
            Description = aiFunction.Description,
            Parameters = OpenAIClientExtensions.ToOpenAIFunctionParameters(aiFunction, strict),
        };
    }
}
