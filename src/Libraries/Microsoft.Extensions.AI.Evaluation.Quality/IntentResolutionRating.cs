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
    public static IntentResolutionRating Inconclusive { get; } =
        new IntentResolutionRating(
            resolutionScore: 0,
            explanation: string.Empty,
            agentPerceivedIntent: string.Empty,
            actualUserIntent: string.Empty,
            conversationHasIntent: false,
            correctIntentDetected: false,
            intentResolved: false);

    [JsonRequired]
    [JsonPropertyName("resolution_score")]
    public int ResolutionScore { get; set; }

    [JsonRequired]
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; }

    [JsonRequired]
    [JsonPropertyName("agent_perceived_intent")]
    public string AgentPerceivedIntent { get; set; }

    [JsonRequired]
    [JsonPropertyName("actual_user_intent")]
    public string ActualUserIntent { get; set; }

    [JsonRequired]
    [JsonPropertyName("conversation_has_intent")]
    public bool ConversationHasIntent { get; set; }

    [JsonRequired]
    [JsonPropertyName("correct_intent_detected")]
    public bool CorrectIntentDetected { get; set; }

    [JsonRequired]
    [JsonPropertyName("intent_resolved")]
    public bool IntentResolved { get; set; }

    private const int MinValue = 1;
    private const int MaxValue = 5;

    public bool IsInconclusive => ResolutionScore < MinValue || ResolutionScore > MaxValue;

    [JsonConstructor]
#pragma warning disable S107 // Methods should not have too many parameters
    public IntentResolutionRating(
        int resolutionScore,
        string explanation,
        string agentPerceivedIntent,
        string actualUserIntent,
        bool conversationHasIntent,
        bool correctIntentDetected,
        bool intentResolved)
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
