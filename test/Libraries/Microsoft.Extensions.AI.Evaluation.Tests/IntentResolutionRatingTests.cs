// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Quality.JsonSerialization;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Tests;

public class IntentResolutionRatingTests
{
    [Fact]
    public void JsonIsValid()
    {
        string json =
            """
            {
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true,
                "resolution_score": 5
            }
            """;

        IntentResolutionRating rating = IntentResolutionRating.FromJson(json);

        Assert.Equal(5, rating.ResolutionScore);
        Assert.Equal("The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.", rating.Explanation);
        Assert.Equal("provide a comprehensive chocolate cake recipe", rating.AgentPerceivedIntent);
        Assert.Equal("bake a chocolate cake", rating.ActualUserIntent);
        Assert.True(rating.ConversationHasIntent);
        Assert.True(rating.CorrectIntentDetected);
        Assert.True(rating.IntentResolved);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonIsSurroundedWithMarkdownSyntax()
    {
        string json =
            """

            ```
            {
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true,
                "resolution_score": 5,
            }
            ```

            """;

        IntentResolutionRating rating = IntentResolutionRating.FromJson(json);

        Assert.Equal(5, rating.ResolutionScore);
        Assert.Equal("The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.", rating.Explanation);
        Assert.Equal("provide a comprehensive chocolate cake recipe", rating.AgentPerceivedIntent);
        Assert.Equal("bake a chocolate cake", rating.ActualUserIntent);
        Assert.True(rating.ConversationHasIntent);
        Assert.True(rating.CorrectIntentDetected);
        Assert.True(rating.IntentResolved);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonIsSurroundedWithMarkdownSyntaxWithJsonPrefix()
    {
        string json =
            """

            ```json
            {
                "resolution_score": 5,
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true
            }
            ```

            """;

        IntentResolutionRating rating = IntentResolutionRating.FromJson(json);

        Assert.Equal(5, rating.ResolutionScore);
        Assert.Equal("The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.", rating.Explanation);
        Assert.Equal("provide a comprehensive chocolate cake recipe", rating.AgentPerceivedIntent);
        Assert.Equal("bake a chocolate cake", rating.ActualUserIntent);
        Assert.True(rating.ConversationHasIntent);
        Assert.True(rating.CorrectIntentDetected);
        Assert.True(rating.IntentResolved);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonCanBeRoundTripped()
    {
        IntentResolutionRating rating =
            new IntentResolutionRating(
                resolutionScore: 1,
                explanation: "explanation",
                agentPerceivedIntent: "perceived intent",
                actualUserIntent: "actual intent",
                conversationHasIntent: false,
                correctIntentDetected: true,
                intentResolved: true);

        string json = JsonSerializer.Serialize(rating, SerializerContext.Default.IntentResolutionRating);
        IntentResolutionRating deserialized = IntentResolutionRating.FromJson(json);

        Assert.Equal(rating.ResolutionScore, deserialized.ResolutionScore);
        Assert.Equal(rating.Explanation, deserialized.Explanation);
        Assert.Equal(rating.AgentPerceivedIntent, deserialized.AgentPerceivedIntent);
        Assert.Equal(rating.ActualUserIntent, deserialized.ActualUserIntent);
        Assert.Equal(rating.ConversationHasIntent, deserialized.ConversationHasIntent);
        Assert.Equal(rating.CorrectIntentDetected, deserialized.CorrectIntentDetected);
        Assert.Equal(rating.IntentResolved, deserialized.IntentResolved);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void InconclusiveJsonCanBeRoundTripped()
    {
        IntentResolutionRating rating = IntentResolutionRating.Inconclusive;

        string json = JsonSerializer.Serialize(rating, SerializerContext.Default.IntentResolutionRating);
        IntentResolutionRating deserialized = IntentResolutionRating.FromJson(json);

        Assert.Equal(rating.ResolutionScore, deserialized.ResolutionScore);
        Assert.Equal(rating.Explanation, deserialized.Explanation);
        Assert.Equal(rating.AgentPerceivedIntent, deserialized.AgentPerceivedIntent);
        Assert.Equal(rating.ActualUserIntent, deserialized.ActualUserIntent);
        Assert.Equal(rating.ConversationHasIntent, deserialized.ConversationHasIntent);
        Assert.Equal(rating.CorrectIntentDetected, deserialized.CorrectIntentDetected);
        Assert.Equal(rating.IntentResolved, deserialized.IntentResolved);
        Assert.True(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithNegativeScoreIsInconclusive()
    {
        string json =
            """
            {
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true,
                "resolution_score": -1
            }
            """;

        IntentResolutionRating rating = IntentResolutionRating.FromJson(json);

        Assert.True(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithZeroScoreIsInconclusive()
    {
        string json =
            """
            {
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true,
                "resolution_score": 0
            }
            """;

        IntentResolutionRating rating = IntentResolutionRating.FromJson(json);

        Assert.True(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithExcessivelyHighScoreIsInconclusive()
    {
        string json =
            """
            {
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true,
                "resolution_score": 200
            }
            """;

        IntentResolutionRating rating = IntentResolutionRating.FromJson(json);

        Assert.True(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithAdditionalHallucinatedPropertyIsProcessedCorrectly()
    {
        string json =
            """
            {
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "hallucinated_property": "Some hallucinated text.",
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true,
                "resolution_score": 5,
            }
            """;

        IntentResolutionRating rating = IntentResolutionRating.FromJson(json);

        Assert.Equal(5, rating.ResolutionScore);
        Assert.Equal("The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.", rating.Explanation);
        Assert.Equal("provide a comprehensive chocolate cake recipe", rating.AgentPerceivedIntent);
        Assert.Equal("bake a chocolate cake", rating.ActualUserIntent);
        Assert.True(rating.ConversationHasIntent);
        Assert.True(rating.CorrectIntentDetected);
        Assert.True(rating.IntentResolved);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithDuplicatePropertyUsesLastValue()
    {
        string json =
            """
            {
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "explanation": "Duplicate explanation.",
                "conversation_has_intent": true,
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true,
                "resolution_score": 5,
            }
            """;

        IntentResolutionRating rating = IntentResolutionRating.FromJson(json);

        Assert.Equal(5, rating.ResolutionScore);
        Assert.Equal("Duplicate explanation.", rating.Explanation);
        Assert.Equal("provide a comprehensive chocolate cake recipe", rating.AgentPerceivedIntent);
        Assert.Equal("bake a chocolate cake", rating.ActualUserIntent);
        Assert.True(rating.ConversationHasIntent);
        Assert.True(rating.CorrectIntentDetected);
        Assert.True(rating.IntentResolved);
        Assert.False(rating.IsInconclusive);
    }

    [Fact]
    public void JsonWithSemicolonsInsteadOfCommasThrowsException()
    {
        string json =
            """
            {
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.";
                "conversation_has_intent": true;
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe";
                "actual_user_intent": "bake a chocolate cake";
                "correct_intent_detected": true;
                "intent_resolved": true;
                "resolution_score": 5
            }
            """;

        Assert.Throws<JsonException>(() => IntentResolutionRating.FromJson(json));
    }

    [Fact]
    public void JsonWithMissingPropertiesThrowsException()
    {
        string json =
            """
            {
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "intent_resolved": true,
                "resolution_score": 5
            }
            """;

        Assert.Throws<JsonException>(() => IntentResolutionRating.FromJson(json));
    }

    [Fact]
    public void JsonWithIncorrectPropertyValueTypeThrowsException()
    {
        // Incorrect property value (string instead of boolean for conversation_has_intent).
        string json =
            """
            {
                "explanation": "The response delivers a complete and precise recipe, fully addressing the user's query about baking a chocolate cake.",
                "conversation_has_intent": "A string value",
                "agent_perceived_intent": "provide a comprehensive chocolate cake recipe",
                "actual_user_intent": "bake a chocolate cake",
                "correct_intent_detected": true,
                "intent_resolved": true,
                "resolution_score": 5,
            }
            """;

        Assert.Throws<JsonException>(() => IntentResolutionRating.FromJson(json));
    }
}
