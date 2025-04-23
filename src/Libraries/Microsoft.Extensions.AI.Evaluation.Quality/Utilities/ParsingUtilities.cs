// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Quality.Utilities;

internal static class ParsingUtilities
{
    internal static ReadOnlySpan<char> TrimJsonMarkdownDelimiters(string json)
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
        if (trimmed.Length > markerLength && trimmed.Slice(0, markerLength).SequenceEqual(JsonMarker.AsSpan()))
        {
            trimmed = trimmed.Slice(markerLength);
        }

        return trimmed;
    }

    internal static async ValueTask<string> RepairJsonAsync(
        string json,
        ChatConfiguration chatConfig,
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

        ChatResponse response =
            await chatConfig.ChatClient.GetResponseAsync(
                messages,
                chatOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);

        return response.Text.Trim();
    }

    internal static bool TryParseNumericScore(
        string evaluationResponse,
        [NotNullWhen(true)] out int? score,
        out string? reason,
        out string? chainOfThought)
    {
        if (string.IsNullOrWhiteSpace(evaluationResponse))
        {
            score = null;
            reason = null;
            chainOfThought = null;
            return false;
        }

        evaluationResponse = evaluationResponse.Trim();

        // Format: <S0>chain of thought</S0>, <S1>explanation</S1>, <S2>score</S2>

        if (!TryParseScore(evaluationResponse, tag: "S2", out score))
        {
            score = null;
            reason = null;
            chainOfThought = null;
            return false;
        }

        _ = TryParseText(evaluationResponse, tag: "S1", out reason);
        _ = TryParseText(evaluationResponse, tag: "S0", out chainOfThought);

        return true;

        static bool TryParseScore(string evaluationResponse, string tag, [NotNullWhen(true)] out int? score)
        {
            if (!TryParseText(evaluationResponse, tag, out string? text))
            {
                score = null;
                return false;
            }

            if (!int.TryParse(text, out int scoreValue))
            {
                score = null;
                return false;
            }

            score = scoreValue;
            return true;
        }

        static bool TryParseText(string evaluationResponse, string tag, [NotNullWhen(true)] out string? text)
        {
            Match match = Regex.Match(evaluationResponse, $@"<{tag}>(?<value>.*?)</{tag}>", RegexOptions.Multiline);

            if (!match.Success || match.Groups["value"] is not Group valueGroup || !valueGroup.Success)
            {
                text = null;
                return false;
            }

            if (valueGroup.Value is not string matchText ||
                matchText.Trim() is not string trimmedMatchText ||
                string.IsNullOrEmpty(trimmedMatchText))
            {
                text = null;
                return false;
            }

            text = trimmedMatchText;
            return true;
        }
    }
}
