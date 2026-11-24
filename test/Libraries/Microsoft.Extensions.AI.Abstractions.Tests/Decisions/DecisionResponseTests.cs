// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using static Microsoft.Extensions.AI.DecisionTestData;

namespace Microsoft.Extensions.AI;

public class DecisionResponseTests
{
    [Fact]
    public void GetAnswer_ReturnsMixedAnsweredAndUnansweredAnswers()
    {
        DecisionResponse response = CreateMixedResponse();

        UnansweredDecisionAnswer unanswered = Assert.IsType<UnansweredDecisionAnswer>(response.GetAnswer("score"));
        Assert.Equal(DecisionKind.Score, unanswered.Kind);
        Assert.Equal(DecisionUnansweredReason.Refusal, unanswered.Reason);
        Assert.IsType<ChoiceDecisionAnswer>(response.GetAnswer("choice"));
        Assert.Throws<KeyNotFoundException>(() => response.GetAnswer("missing"));
    }

    [Fact]
    public void Response_DefaultsToEmptyAnswers()
    {
        DecisionResponse response = new(null);
        Assert.Empty(response.Answers);
        Assert.Empty(new DecisionResponse().Answers);
    }

    [Fact]
    public void Json_RoundTripsMixedResponsesPreservingOrderAndKind()
    {
        DecisionResponse response = CreateMixedResponse();
        response.ModelId = "model";
        response.Usage = new() { InputTokenCount = 10 };
        response.Answers[0].AdditionalProperties = new() { ["confidence"] = "high" };

        string json = JsonSerializer.Serialize(response, AIJsonUtilities.DefaultOptions);
        DecisionResponse roundTripped = JsonSerializer.Deserialize<DecisionResponse>(json, AIJsonUtilities.DefaultOptions)!;

        Assert.Equal(3, roundTripped.Answers.Count);
        Assert.Equal("model", roundTripped.ModelId);
        Assert.Equal(10, roundTripped.Usage!.InputTokenCount);
        Assert.Equal(0.75, Assert.IsType<BinaryDecisionAnswer>(roundTripped.Answers[0]).TrueProbability);
        Assert.True(roundTripped.Answers[0].AdditionalProperties!.ContainsKey("confidence"));
        Assert.Equal("b", Assert.IsType<ChoiceDecisionAnswer>(roundTripped.Answers[1]).SelectedCandidateId);
        UnansweredDecisionAnswer unanswered = Assert.IsType<UnansweredDecisionAnswer>(roundTripped.Answers[2]);
        Assert.Equal(DecisionKind.Score, unanswered.Kind);
        Assert.Equal(DecisionUnansweredReason.Refusal, unanswered.Reason);
    }
}