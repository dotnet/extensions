// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Quality.Utilities;

internal static class JsonOutputFixer
{
    internal static ReadOnlySpan<char> TrimMarkdownDelimiters(string json)
    {
#if NET
        ReadOnlySpan<char> trimmed = json;
#else
        ReadOnlySpan<char> trimmed = json.ToCharArray();
#endif

        // Trim whitespace and markdown characters from beginning and end.
        trimmed = trimmed.Trim().Trim(['`']);

        // Trim 'json' marker from markdown if it exists.
        const string JsonMarker = "json";
        int markerLength = JsonMarker.Length;
        if (trimmed.Length > markerLength && trimmed[0..markerLength].SequenceEqual(JsonMarker.AsSpan()))
        {
            trimmed = trimmed.Slice(markerLength);
        }

        return trimmed;
    }

    internal static async ValueTask<string?> RepairJsonAsync(
        ChatConfiguration chatConfig,
        string json,
        CancellationToken cancellationToken)
    {
        const string SystemPrompt =
            """
            You are an AI assistant. Your job is to fix any syntax errors in a supplied JSON object so that it conforms
            strictly to the JSON standard. Your response should include just the fixed JSON object and nothing else.
            """;

        string fixPrompt =
            $"""
            Fix the following JSON object. Return exactly the same JSON object with the same data content but with any
            syntax errors corrected.

            If the supplied text includes any markdown delimiters around the JSON object, strip out the markdown
            delimiters and return just the fixed JSON object. Your response should start with an open curly brace and
            end with a closing curly brace.
            ---
            {json}
            """;

        ChatOptions chatOptions =
            new ChatOptions
            {
                Temperature = 0.0f,
                ResponseFormat = ChatResponseFormat.Json
            };

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, SystemPrompt),
            new ChatMessage(ChatRole.User, fixPrompt)
        };

        // TASK: Explore supplying the target json type as a type parameter to the IChatClient.GetResponseAsync<T>()
        // extension method. Tracked by https://github.com/dotnet/extensions/issues/5888.

        ChatResponse response =
            await chatConfig.ChatClient.GetResponseAsync(
                messages,
                chatOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);

        return response.Message.Text;
    }
}
