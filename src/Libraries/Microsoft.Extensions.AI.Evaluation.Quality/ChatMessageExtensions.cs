// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

internal static class ChatMessageExtensions
{
    internal static string RenderAsJson(this IEnumerable<ChatMessage> messages, JsonSerializerOptions? options = null)
    {
        _ = Throw.IfNull(messages);

        var messagesJsonArray = new JsonArray();

        foreach (ChatMessage message in messages)
        {
            JsonNode? messageJsonNode =
                JsonSerializer.SerializeToNode(
                    message,
                    AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(ChatMessage)));

            if (messageJsonNode is not null)
            {
                messagesJsonArray.Add(messageJsonNode);
            }
        }

        string renderedMessages = messagesJsonArray.ToJsonString(options);
        return renderedMessages;
    }
}
