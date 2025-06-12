// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI.Evaluation.Quality.JsonSerialization;
using Microsoft.Extensions.AI.Evaluation.Quality.Utilities;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

internal sealed class IntentResolutionRating
{
    public static IntentResolutionRating Inconclusive { get; } = new IntentResolutionRating();

    [JsonPropertyName("resolution_score")]
    public int ResolutionScore { get; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; }

    [JsonPropertyName("agent_perceived_intent")]
    public string? AgentPerceivedIntent { get; }

    [JsonPropertyName("actual_user_intent")]
    public string? ActualUserIntent { get; }

    [JsonPropertyName("conversation_has_intent")]
    public bool ConversationHasIntent { get; }

    [JsonPropertyName("correct_intent_detected")]
    public bool CorrectIntentDetected { get; }

    [JsonPropertyName("intent_resolved")]
    public bool IntentResolved { get; }

    private const int MinValue = 1;
    private const int MaxValue = 5;

    public bool IsInconclusive => ResolutionScore < MinValue || ResolutionScore > MaxValue;

    [JsonConstructor]
#pragma warning disable S107 // Methods should not have too many parameters
    public IntentResolutionRating(
        int resolutionScore = -1,
        string? explanation = null,
        string? agentPerceivedIntent = null,
        string? actualUserIntent = null,
        bool conversationHasIntent = false,
        bool correctIntentDetected = false,
        bool intentResolved = false)
#pragma warning restore S107
    {
        ResolutionScore = resolutionScore;
        Explanation = explanation;
        AgentPerceivedIntent = agentPerceivedIntent;
        ActualUserIntent = actualUserIntent;
        ConversationHasIntent = conversationHasIntent;
        CorrectIntentDetected = correctIntentDetected;
        IntentResolved = intentResolved;
    }

    public static IntentResolutionRating FromJson(string jsonResponse)
    {
        ReadOnlySpan<char> trimmed = JsonOutputFixer.TrimMarkdownDelimiters(jsonResponse);
        return JsonSerializer.Deserialize(trimmed, SerializerContext.Default.IntentResolutionRating)!;
    }
}
